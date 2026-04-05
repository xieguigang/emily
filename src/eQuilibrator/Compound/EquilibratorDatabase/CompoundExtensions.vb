Imports System.Runtime.CompilerServices
Imports System.Text

Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 扩展方法
    ' ========================================================================

    Public Module CompoundExtensions

        ''' <summary>
        ''' 获取化合物的分子式
        ''' </summary>
        <Extension>
        Public Function GetMolecularFormula(compound As Compound) As String
            If compound.AtomBag Is Nothing OrElse compound.AtomBag.Count = 0 Then
                Return String.Empty
            End If

            ' 按照Hill系统排序: C先，H次之，其他按字母顺序
            Dim sb As New StringBuilder()

            ' 碳
            If compound.AtomBag.ContainsKey("C") Then
                sb.Append("C")
                If compound.AtomBag("C") > 1 Then
                    sb.Append(compound.AtomBag("C"))
                End If
            End If

            ' 氢
            If compound.AtomBag.ContainsKey("H") Then
                sb.Append("H")
                If compound.AtomBag("H") > 1 Then
                    sb.Append(compound.AtomBag("H"))
                End If
            End If

            ' 其他元素按字母顺序
            Dim otherElements = compound.AtomBag.Keys.Where(Function(k) k <> "C" AndAlso k <> "H").OrderBy(Function(k) k)
            For Each element In otherElements
                sb.Append(element)
                If compound.AtomBag(element) > 1 Then
                    sb.Append(compound.AtomBag(element))
                End If
            Next

            Return sb.ToString()
        End Function

        ''' <summary>
        ''' 获取化合物的净电荷
        ''' </summary>
        <Extension>
        Public Function GetNetCharge(compound As Compound) As Integer
            If compound.Microspecies Is Nothing OrElse compound.Microspecies.Count = 0 Then
                Return 0
            End If

            Dim majorMs = compound.Microspecies.FirstOrDefault(Function(m) m.IsMajor)
            If majorMs IsNot Nothing Then
                Return majorMs.Charge
            End If

            Return 0
        End Function

        ''' <summary>
        ''' 获取化合物的pKa值列表
        ''' </summary>
        <Extension>
        Public Function GetpKaValues(compound As Compound) As List(Of Double)
            If compound.DissociationConstants Is Nothing OrElse compound.DissociationConstants.Count = 0 Then
                Return New List(Of Double)()
            End If

            ' 解离常数转换为pKa
            Return compound.DissociationConstants.Select(Function(k) -Math.Log10(k)).ToList()
        End Function

        ''' <summary>
        ''' 获取化合物的简要描述
        ''' </summary>
        <Extension>
        Public Function GetDescription(compound As Compound) As String
            Dim sb As New StringBuilder()
            sb.AppendLine($"化合物ID: {compound.Id}")
            sb.AppendLine($"InChI Key: {compound.InChIKey}")
            sb.AppendLine($"SMILES: {compound.Smiles}")
            sb.AppendLine($"分子量: {compound.Mass:F3} g/mol")
            sb.AppendLine($"分子式: {compound.GetMolecularFormula()}")
            sb.AppendLine($"净电荷: {compound.GetNetCharge()}")

            If compound.Microspecies IsNot Nothing AndAlso compound.Microspecies.Count > 0 Then
                sb.AppendLine($"微物种数: {compound.Microspecies.Count}")
            End If

            If compound.GroupVector IsNot Nothing AndAlso compound.GroupVector.Length > 0 Then
                sb.AppendLine($"基团向量维度: {compound.GroupVector.Length}")
            End If

            Return sb.ToString()
        End Function

    End Module
End Namespace