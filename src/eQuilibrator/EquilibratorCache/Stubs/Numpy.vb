''' <summary>
''' NumPy 库的 VB.NET 占位实现。
''' 提供 numpy 中常用数学和数组操作的 .NET 基础实现。
''' </summary>
Public Module Numpy

    ''' <summary>正无穷大</summary>
    Public ReadOnly Inf As Double = Double.PositiveInfinity

    ''' <summary>
    ''' 计算两个数的 log(exp(a) + exp(b))，即 logaddexp。
    ''' 等价于 numpy.logaddexp(a, b)。
    ''' </summary>
    Public Function LogAddExp(a As Double, b As Double) As Double
        If a = Double.NegativeInfinity Then Return b
        If b = Double.NegativeInfinity Then Return a
        Dim maxVal As Double = Math.Max(a, b)
        Dim minVal As Double = Math.Min(a, b)
        Return maxVal + Math.Log(1.0 + Math.Exp(minVal - maxVal))
    End Function

    ''' <summary>
    ''' 计算 log-sum-exp：log(sum(exp(x) * b))。
    ''' 等价于 scipy.special.logsumexp(x, b=weights, return_sign=False)。
    ''' 当 returnSign=True 时，同时返回结果的符号。
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

        ' 计算带权重的 log-sum-exp
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
            Dim result As Double = maxVal + Math.Log(sumExp)
            Return New Double() {result, signProduct}
        Else
            Return maxVal + Math.Log(sumExp)
        End If
    End Function

    ''' <summary>
    ''' 创建一个全零的二维 Double 数组。
    ''' 等价于 numpy.zeros((rows, cols))。
    ''' </summary>
    Public Function Zeros(rows As Integer, cols As Integer) As Double(,)
        Return New Double(rows - 1, cols - 1) {}
    End Function

    ''' <summary>
    ''' 计算数组的绝对值。
    ''' 等价于 numpy.abs(x)。
    ''' </summary>
    Public Function Abs(x As Double) As Double
        Return Math.Abs(x)
    End Function

    ''' <summary>
    ''' 计算自然对数（以 e 为底）。
    ''' 等价于 numpy.log(x)。
    ''' </summary>
    Public Function Log(x As Double) As Double
        Return Math.Log(x)
    End Function

    ''' <summary>
    ''' 计算以 10 为底的对数。
    ''' 等价于 numpy.log10(x)。
    ''' </summary>
    Public Function Log10(x As Double) As Double
        Return Math.Log10(x)
    End Function

    ''' <summary>
    ''' 计算 e 的 x 次方。
    ''' 等价于 numpy.exp(x)。
    ''' </summary>
    Public Function Exp(x As Double) As Double
        Return Math.Exp(x)
    End Function

End Module
