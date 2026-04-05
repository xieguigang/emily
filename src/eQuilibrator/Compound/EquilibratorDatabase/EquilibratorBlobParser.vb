Imports System.IO
Imports System.Text

Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' BLOB数据解析器
    ' ========================================================================

    ''' <summary>
    ''' Equilibrator BLOB数据解析器
    ''' 用于解析数据库中存储的二进制数据
    ''' </summary>
    Public Class EquilibratorBlobParser

        ''' <summary>
        ''' 解析group_vector BLOB数据
        ''' 该向量用于基团贡献法计算标准生成能
        ''' </summary>
        Public Shared Function ParseGroupVector(data As Byte()) As Double()
            If data Is Nothing OrElse data.Length = 0 Then
                Return New Double() {}
            End If

            Try
                ' Equilibrator使用numpy数组格式存储
                ' 格式: [协议头][数组信息][数据]
                Using ms As New MemoryStream(data)
                    Using reader As New BinaryReader(ms)

                        ' 检查是否为numpy格式 (以特定的magic number开始)
                        ' 或者是自定义格式
                        If data.Length >= 2 AndAlso data(0) = &H80 Then
                            ' 自定义格式解析
                            Return ParseCustomFormat(reader)
                        Else
                            ' 尝试作为numpy格式解析
                            Return ParseNumpyFormat(data)
                        End If
                    End Using
                End Using
            Catch ex As Exception
                ' 如果解析失败，返回空数组
                Return New Double() {}
            End Try
        End Function

        ''' <summary>
        ''' 解析自定义格式的group_vector
        ''' </summary>
        Private Shared Function ParseCustomFormat(reader As BinaryReader) As Double()
            Dim result As New List(Of Double)()

            Try
                ' 跳过第一个字节 (0x80)
                reader.ReadByte()

                ' 读取数组类型标记
                Dim typeMarker As Byte = reader.ReadByte()

                ' 读取数据长度 (变长整数)
                Dim length As Integer = ReadVarInt(reader)

                ' 根据类型读取数据
                Select Case typeMarker
                    Case &H95 ' float64 类型
                        For i As Integer = 0 To length - 1
                            ' 检查是否有足够的数据
                            If reader.BaseStream.Position + 8 <= reader.BaseStream.Length Then
                                Dim value As Double = reader.ReadDouble()
                                result.Add(value)
                            Else
                                Exit For
                            End If
                        Next
                    Case &H3 ' int32 类型
                        For i As Integer = 0 To length - 1
                            If reader.BaseStream.Position + 4 <= reader.BaseStream.Length Then
                                Dim value As Integer = reader.ReadInt32()
                                result.Add(CDbl(value))
                            Else
                                Exit For
                            End If
                        Next
                End Select
            Catch ex As Exception
                ' 解析错误时返回已解析的数据
            End Try

            Return result.ToArray()
        End Function

        ''' <summary>
        ''' 解析numpy格式的数组
        ''' </summary>
        Private Shared Function ParseNumpyFormat(data As Byte()) As Double()
            ' NumPy .npy格式解析
            ' 简化实现：直接尝试读取float64数组
            Dim result As New List(Of Double)()

            Try
                ' NumPy文件头通常以 \x93NUMPY 开始
                Dim headerStart As Integer = 0
                If data.Length >= 6 AndAlso data(0) = &H93 Then
                    ' 找到文件头结束位置
                    For i As Integer = 10 To Math.Min(data.Length - 1, 128)
                        If data(i) = &HA Then ' 换行符标记头结束
                            headerStart = i + 1
                            Exit For
                        End If
                    Next
                End If

                ' 读取数据部分
                Using ms As New MemoryStream(data, headerStart, data.Length - headerStart)
                    Using reader As New BinaryReader(ms)
                        While reader.BaseStream.Position + 8 <= reader.BaseStream.Length
                            Dim value As Double = reader.ReadDouble()
                            result.Add(value)
                        End While
                    End Using
                End Using
            Catch ex As Exception
                ' 解析失败
            End Try

            Return result.ToArray()
        End Function

        ''' <summary>
        ''' 读取变长整数
        ''' </summary>
        Private Shared Function ReadVarInt(reader As BinaryReader) As Integer
            Dim result As Integer = 0
            Dim shift As Integer = 0
            Dim b As Byte

            Do
                b = reader.ReadByte()
                result = result Or ((b And &H7F) << shift)
                shift += 7
            Loop While (b And &H80) <> 0 AndAlso shift < 35

            Return result
        End Function

        ''' <summary>
        ''' 解析atom_bag BLOB数据
        ''' 返回原子组成字典
        ''' </summary>
        Public Shared Function ParseAtomBag(data As Byte()) As Dictionary(Of String, Integer)
            Dim result As New Dictionary(Of String, Integer)()

            If data Is Nothing OrElse data.Length = 0 Then
                Return result
            End If

            Try
                ' atom_bag通常存储为字典格式
                ' 格式可能是: [元素符号][数量]的序列
                Using ms As New MemoryStream(data)
                    Using reader As New BinaryReader(ms)
                        ' 跳过格式头
                        If data(0) = &H80 Then
                            reader.ReadByte() ' 0x80
                            reader.ReadByte() ' 类型标记
                        End If

                        ' 尝试解析键值对
                        While reader.BaseStream.Position < reader.BaseStream.Length - 1
                            Try
                                ' 读取元素符号 (通常是1-2个字符)
                                Dim elementName As New StringBuilder()
                                Dim b As Byte = reader.ReadByte()

                                ' 检查是否为字母 (元素符号)
                                While (b >= 65 AndAlso b <= 90) OrElse (b >= 97 AndAlso b <= 122)
                                    elementName.Append(ChrW(b))
                                    If reader.BaseStream.Position < reader.BaseStream.Length Then
                                        b = reader.ReadByte()
                                    Else
                                        Exit While
                                    End If
                                End While

                                If elementName.Length > 0 Then
                                    ' 读取数量
                                    Dim count As Integer = 0
                                    If b >= 48 AndAlso b <= 57 Then
                                        ' 数字字符
                                        count = b - 48
                                    Else
                                        ' 可能是二进制格式
                                        If reader.BaseStream.Position < reader.BaseStream.Length Then
                                            count = reader.ReadByte()
                                        End If
                                    End If

                                    If count > 0 Then
                                        result(elementName.ToString()) = count
                                    End If
                                End If
                            Catch
                                Exit While
                            End Try
                        End While
                    End Using
                End Using
            Catch ex As Exception
                ' 解析失败，返回空字典
            End Try

            Return result
        End Function

        ''' <summary>
        ''' 解析dissociation_constants BLOB数据
        ''' 返回解离常数列表 (pKa值)
        ''' </summary>
        Public Shared Function ParseDissociationConstants(data As Byte()) As List(Of Double)
            Dim result As New List(Of Double)()

            If data Is Nothing OrElse data.Length = 0 Then
                Return result
            End If

            Try
                ' 解离常数通常存储为float64数组
                ' 使用与group_vector类似的解析方法
                Dim values As Double() = ParseGroupVector(data)
                result.AddRange(values)
            Catch ex As Exception
                ' 解析失败
            End Try

            Return result
        End Function
    End Class

End Namespace