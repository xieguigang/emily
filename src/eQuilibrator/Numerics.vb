Namespace EquilibratorApi.Core

    ''' <summary>
    ''' 数值计算辅助模块，替代被删除的 Python NumPy 桩代码。
    ''' 提供 log-sum-exp / log-add-exp，用于微物种 Legendre 变换的分区函数计算。
    ''' </summary>
    Public Module Numerics

        ''' <summary>
        ''' 计算 log(exp(a) + exp(b))（等价于 numpy.logaddexp）。
        ''' </summary>
        Public Function LogAddExp(a As Double, b As Double) As Double
            If a = Double.NegativeInfinity Then Return b
            If b = Double.NegativeInfinity Then Return a
            Dim maxVal As Double = Math.Max(a, b)
            Dim minVal As Double = Math.Min(a, b)
            Return maxVal + Math.Log(1.0 + Math.Exp(minVal - maxVal))
        End Function

        ''' <summary>
        ''' 计算 log(sum(exp(x) * b))（等价于 scipy.special.logsumexp）。
        ''' 当 returnSign=True 时返回 {value, sign} 的双元素数组。
        ''' </summary>
        Public Function LogSumExp(x As Double(), Optional b As Double() = Nothing, Optional returnSign As Boolean = False) As Object
            If x Is Nothing OrElse x.Length = 0 Then
                If returnSign Then Return New Double() {Double.NegativeInfinity, 1.0}
                Return Double.NegativeInfinity
            End If

            Dim maxVal As Double = Double.NegativeInfinity
            For i As Integer = 0 To x.Length - 1
                If x(i) > maxVal Then maxVal = x(i)
            Next

            If maxVal = Double.PositiveInfinity Then
                If returnSign Then Return New Double() {Double.PositiveInfinity, 1.0}
                Return Double.PositiveInfinity
            End If

            If maxVal = Double.NegativeInfinity Then
                If returnSign Then Return New Double() {Double.NegativeInfinity, 1.0}
                Return Double.NegativeInfinity
            End If

            Dim sumExp As Double = 0.0
            Dim signProduct As Double = 1.0

            For i As Integer = 0 To x.Length - 1
                Dim weight As Double = If(b IsNot Nothing, b(i), 1.0)
                Dim expVal As Double = Math.Exp(x(i) - maxVal)
                If returnSign AndAlso weight < 0 Then
                    sumExp += Math.Abs(weight) * expVal
                    signProduct *= -1.0
                Else
                    sumExp += Math.Abs(weight) * expVal
                End If
            Next

            If returnSign Then
                Return New Double() {maxVal + Math.Log(sumExp), signProduct}
            End If
            Return maxVal + Math.Log(sumExp)
        End Function
    End Module
End Namespace
