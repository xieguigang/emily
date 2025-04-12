Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports std = System.Math

Namespace StructModels

    Public Class Sheet

        ReadOnly originalPath As Point3D()

        Sub New(originalPoints As Point3D())
            Me.originalPath = originalPoints
        End Sub

        Public Function GenerateSheetRibbonModel(thickness As Single, width As Single, segments As Integer) As List(Of Point3D())
            ' 1. 路径重采样
            Dim sampledPath = ResamplePath(segments)
            ' 2. 计算Frenet标架
            Dim frames = CalculateFrames(sampledPath)
            ' 3. 生成条带顶点
            Return GenerateStripVertices(sampledPath, frames, width, thickness)
        End Function

        Private Function ResamplePath(targetCount As Integer) As List(Of Point3D)
            ' 简化的线性重采样实现
            Dim resampled = New List(Of Point3D)
            Dim [step] = originalPath.Length / targetCount

            For i = 0 To targetCount - 1
                Dim idx = CInt(std.Floor(i * [step]))
                Dim t = i * [step] - idx

                If idx < originalPath.Count - 1 Then
                    resampled.Add(LinearInterpolate(originalPath(idx),
                                                  originalPath(idx + 1), t))
                Else
                    resampled.Add(originalPath.Last())
                End If
            Next

            Return resampled
        End Function

        Private Function CalculateFrames(path As List(Of Point3D)) As List(Of Frame)
            Dim frames = New List(Of Frame)

            For i = 0 To path.Count - 1
                Dim tangent = CalculateTangent(path, i)
                Dim normal = CalculateNormal(path, i, tangent)
                Dim binormal = CrossProduct(tangent, normal)

                frames.Add(New Frame With {
                    .Tangent = Normalize(tangent),
                    .Normal = Normalize(normal),
                    .Binormal = Normalize(binormal)
                })
            Next

            Return frames
        End Function

        Private Function GenerateStripVertices(path As List(Of Point3D),
                                         frames As List(Of Frame),
                                         width As Single,
                                         thickness As Single) As List(Of Point3D())
            Dim verticesList = New List(Of Point3D())
            Dim halfWidth = width / 2
            Dim halfThickness = thickness / 2

            For i = 0 To path.Count - 2
                Dim current = path(i)
                Dim [next] = path(i + 1)
                Dim frameCurrent = frames(i)
                Dim frameNext = frames(i + 1)

                ' 计算四个方向向量
                Dim widthOffsetCurrent = Multiply(frameCurrent.Normal, halfWidth)
                Dim thicknessOffsetCurrent = Multiply(frameCurrent.Binormal, halfThickness)
                Dim widthOffsetNext = Multiply(frameNext.Normal, halfWidth)
                Dim thicknessOffsetNext = Multiply(frameNext.Binormal, halfThickness)

                ' 生成当前段的长方体顶点
                Dim vertices = {
                Add(current, Add(widthOffsetCurrent, thicknessOffsetCurrent)),
                Add(current, Subtract(widthOffsetCurrent, thicknessOffsetCurrent)),
                Add(current, Subtract(thicknessOffsetCurrent, widthOffsetCurrent)),
                Add(current, Add(thicknessOffsetCurrent, widthOffsetCurrent)),
                Add([next], Add(widthOffsetNext, thicknessOffsetNext)),
                Add([next], Subtract(widthOffsetNext, thicknessOffsetNext)),
                Add([next], Subtract(thicknessOffsetNext, widthOffsetNext)),
                Add([next], Add(thicknessOffsetNext, widthOffsetNext))
            }

                verticesList.Add(vertices)
            Next

            Return verticesList
        End Function

#Region "Vector Operations"
        Private Structure Frame
            Public Tangent As Point3D
            Public Normal As Point3D
            Public Binormal As Point3D
        End Structure

        Private Function CalculateTangent(path As List(Of Point3D), index As Integer) As Point3D
            If index = 0 Then
                Return Subtract(path(1), path(0))
            ElseIf index = path.Count - 1 Then
                Return Subtract(path(index), path(index - 1))
            Else
                Return Add(
                Subtract(path(index), path(index - 1)),
                Subtract(path(index + 1), path(index))
            )
            End If
        End Function

        Private Function CalculateNormal(path As List(Of Point3D),
                                    index As Integer,
                                    tangent As Point3D) As Point3D
            If index = 0 OrElse index = path.Count - 1 Then
                Return New Point3D With {.X = 0, .Y = 1, .Z = 0}
            End If

            Dim prev = Subtract(path(index - 1), path(index))
            Dim [next] = Subtract(path(index + 1), path(index))
            Return CrossProduct(prev, [next])
        End Function

        Private Function LinearInterpolate(a As Point3D, b As Point3D, t As Single) As Point3D
            Return New Point3D With {
            .X = a.X + (b.X - a.X) * t,
            .Y = a.Y + (b.Y - a.Y) * t,
            .Z = a.Z + (b.Z - a.Z) * t
        }
        End Function

        Private Function Add(a As Point3D, b As Point3D) As Point3D
            Return New Point3D With {.X = a.X + b.X, .Y = a.Y + b.Y, .Z = a.Z + b.Z}
        End Function

        Private Function Subtract(a As Point3D, b As Point3D) As Point3D
            Return New Point3D With {.X = a.X - b.X, .Y = a.Y - b.Y, .Z = a.Z - b.Z}
        End Function

        Private Function Multiply(v As Point3D, scalar As Single) As Point3D
            Return New Point3D With {.X = v.X * scalar, .Y = v.Y * scalar, .Z = v.Z * scalar}
        End Function

        Private Function CrossProduct(a As Point3D, b As Point3D) As Point3D
            Return New Point3D With {
            .X = a.Y * b.Z - a.Z * b.Y,
            .Y = a.Z * b.X - a.X * b.Z,
            .Z = a.X * b.Y - a.Y * b.X
        }
        End Function

        Private Function Normalize(v As Point3D) As Point3D
            Dim length = CSng(std.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z))
            If length < 0.0001 Then Return v
            Return New Point3D With {.X = v.X / length, .Y = v.Y / length, .Z = v.Z / length}
        End Function
#End Region
    End Class
End Namespace