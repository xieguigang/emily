Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 微物种分布计算器
    ' ========================================================================

    ''' <summary>
    ''' 微物种分布计算器
    ''' 计算在不同pH下各微物种的分布比例
    ''' </summary>
    Public Class MicrospeciesDistributionCalculator

        ''' <summary>
        ''' 计算化合物在特定pH下各微物种的分布
        ''' </summary>
        ''' <param name="compound">化合物对象</param>
        ''' <param name="pH">pH值</param>
        ''' <returns>微物种ID到摩尔分数的映射</returns>
        Public Function CalculateDistribution(compound As Compound, pH As Double) As Dictionary(Of Integer, Double)
            Dim distribution As New Dictionary(Of Integer, Double)()

            If compound.Microspecies Is Nothing OrElse compound.Microspecies.Count = 0 Then
                Return distribution
            End If

            ' 找到主要微物种作为参考
            Dim majorMs = compound.Microspecies.FirstOrDefault(Function(m) m.IsMajor)
            If majorMs Is Nothing Then
                majorMs = compound.Microspecies.First()
            End If

            ' 计算每个微物种相对于主要物种的浓度比
            Dim concentrations As New Dictionary(Of Integer, Double)()
            Dim totalConc As Double = 0.0

            For Each ms In compound.Microspecies
                ' ΔG/RT = ddg_over_rt + (nH_major - nH) * ln(10) * pH
                Dim ddg As Double = If(ms.DdGOverRt, 0.0)
                Dim deltaH As Integer = majorMs.NumberProtons - ms.NumberProtons

                ' 相对浓度 = exp(-ΔG/RT)
                Dim relativeConc As Double = Math.Exp(-(ddg + deltaH * Math.Log(10) * pH))
                concentrations(ms.Id) = relativeConc
                totalConc += relativeConc
            Next

            ' 归一化得到摩尔分数
            For Each kvp In concentrations
                distribution(kvp.Key) = kvp.Value / totalConc
            Next

            Return distribution
        End Function

        ''' <summary>
        ''' 获取在特定pH下的主要微物种
        ''' </summary>
        Public Function GetDominantMicrospecies(compound As Compound, pH As Double) As CompoundMicrospecies
            Dim distribution = CalculateDistribution(compound, pH)

            If distribution.Count = 0 Then
                Return Nothing
            End If

            ' 找到摩尔分数最大的微物种
            Dim maxId As Integer = distribution.OrderByDescending(Function(kvp) kvp.Value).First().Key
            Return compound.Microspecies.FirstOrDefault(Function(m) m.Id = maxId)
        End Function

        ''' <summary>
        ''' 计算平均电荷
        ''' </summary>
        Public Function CalculateAverageCharge(compound As Compound, pH As Double) As Double
            Dim distribution = CalculateDistribution(compound, pH)
            Dim avgCharge As Double = 0.0

            For Each kvp In distribution
                Dim ms = compound.Microspecies.FirstOrDefault(Function(m) m.Id = kvp.Key)
                If ms IsNot Nothing Then
                    avgCharge += kvp.Value * ms.Charge
                End If
            Next

            Return avgCharge
        End Function
    End Class

End Namespace