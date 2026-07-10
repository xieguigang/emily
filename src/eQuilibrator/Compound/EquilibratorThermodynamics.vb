' ============================================================================
' Equilibrator API 化合物热力学计算模块
' 提供组件贡献法（基于微物种 Legendre 分区函数）与组贡献法（基于基团向量）
' 估算化合物在任意 pH / pMg / 离子强度 / 温度下的标准生成吉布斯自由能 ΔfG'°。
' ============================================================================

Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Numerics

Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 标准生成能计算器
    ' ========================================================================

    ''' <summary>
    ''' 化合物标准生成能计算器。
    ''' <list type="bullet">
    '''   <item><description>组件贡献法 <see cref="StandardFormationEnergyTransformed"/>：基于微物种的
    '''   Legendre 变换分区函数（log-sum-exp）计算 ΔfG'°。</description></item>
    '''   <item><description>组贡献法 <see cref="StandardFormationEnergyGroupContribution"/>：基团向量 × 基团贡献
    '''   得到 ΔfG°，再叠加参考微物种的 Legendre 变换得到 ΔfG'°。</description></item>
    ''' </list>
    ''' 注：绝对数值为近似/占位（CSV 无标准 ΔfG° 列），重点演示算法流程与 pH/pMg/离子强度/温度依赖形状。
    ''' </summary>
    Public Class StandardFormationEnergyCalculator

        Private _groupParams As GroupContributionParameters
        Private _compounds As New Dictionary(Of String, Compound)()
        Private _microspecies As New Dictionary(Of Integer, CompoundMicrospecies)()

        ''' <summary>标准温度 (K)</summary>
        Public Const STANDARD_TEMPERATURE As Double = 298.15

        ''' <summary>气体常数 R (kJ/(mol·K))</summary>
        Public Const GAS_CONSTANT As Double = 0.008314

        ''' <summary>RT 在标准温度下 (kJ/mol)</summary>
        Public Const RT As Double = GAS_CONSTANT * STANDARD_TEMPERATURE

        ''' <summary>法拉第常数 (C/mol)</summary>
        Public Const FARADAY_CONSTANT As Double = 96485.33212

        ''' <summary>标准 pH 值</summary>
        Public Property StandardPH As Double = 7.0

        ''' <summary>标准 Mg2+ 浓度 (M)</summary>
        Public Property StandardMgConcentration As Double = 0.001

        Public Sub New()
            _groupParams = New GroupContributionParameters()
        End Sub

        Public Sub New(groupParams As GroupContributionParameters)
            _groupParams = groupParams
        End Sub

        ''' <summary>加载化合物数据（实例 API 兼容）。</summary>
        Public Sub LoadCompounds(compounds As List(Of Compound))
            _compounds.Clear()
            _microspecies.Clear()
            For Each compound In compounds
                _compounds(compound.Id) = compound
                If compound.Microspecies IsNot Nothing Then
                    For Each ms In compound.Microspecies
                        _microspecies(ms.Id) = ms
                    Next
                End If
            Next
        End Sub

        Public Function GetCompoundByInChIKey(inchiKey As String) As Compound
            Return _compounds.Values.FirstOrDefault(Function(c) c.InChIKey = inchiKey)
        End Function

        Public Function GetCompound(compoundId As String) As Compound
            If _compounds.ContainsKey(compoundId) Then Return _compounds(compoundId)
            Return Nothing
        End Function

        ' =====================================================================
        ' 核心：组件贡献法（基于微物种 Legendre 分区函数）
        ' =====================================================================

        ''' <summary>
        ''' 组件贡献法：基于微物种的 Legendre 变换分区函数计算化合物在指定条件下的变换标准生成能 ΔfG'°。
        ''' 公式（参考 Alberty / eQuilibrator）：
        '''   ΔfG'°(c) = -RT · logsumexp_m( -x_m )，其中
        '''   x_m = ddg_over_rt_m · (T0/T) + LegendreTransform(pH, pMg, I, T, charge_m, nH_m, nMg_m)
        ''' 相对基准取 0（近似），以正确呈现 pH/pMg/离子强度/温度依赖形状。
        ''' 若化合物无微物种则返回 Nothing（调用方应回退到组贡献法）。
        ''' </summary>
        Public Shared Function StandardFormationEnergyTransformed(
            compound As Compound,
            pH As Double, pMg As Double, ionicStrength As Double, T_in_K As Double,
            Optional baseFreeEnergy As Double? = Nothing) As Double?

            If compound Is Nothing OrElse compound.Microspecies Is Nothing OrElse compound.Microspecies.Count = 0 Then
                Return Nothing
            End If

            ' 绝对基线：SMILES 衍生化合物传入组贡献法估算的全分子 ΔfG°；
            ' CSV 化合物（microspecies 的 ddg 已为绝对基准）不传，默认 0 保持原行为。
            Dim baseEnergy As Double = If(baseFreeEnergy.HasValue, baseFreeEnergy.Value, 0.0)

            Dim RT = ThermodynamicConstants.default_R_in_kJ_per_mol_per_K * T_in_K
            Dim args(compound.Microspecies.Count - 1) As Double

            For i = 0 To compound.Microspecies.Count - 1
                Dim ms = compound.Microspecies(i)
                ' x_m = ddg_over_rt·(T0/T) + LegendreTransform(...)，单位 RT
                Dim x = ms.TransformedDdgOverRt(pH, pMg, ionicStrength, T_in_K)
                args(i) = -x
            Next

            ' logsumexp(-x) 再乘以 -RT，并叠加绝对基线
            Dim lse As Object = LogSumExp(args)
            Return baseEnergy - RT * CDbl(lse)
        End Function

        ' =====================================================================
        ' 核心：组贡献法（基团向量 × 基团能量 + Legendre 变换）
        ' =====================================================================

        ''' <summary>
        ''' 组贡献法：基团向量 × 占位基团能量得到 ΔfG°，再叠加参考微物种的 Legendre 变换得到 ΔfG'°。
        ''' 若化合物无基团向量则返回 Nothing（调用方应回退）。
        ''' </summary>
        Public Shared Function StandardFormationEnergyGroupContribution(
            compound As Compound,
            pH As Double, pMg As Double, ionicStrength As Double, T_in_K As Double) As Double?

            If compound Is Nothing OrElse compound.GroupVector Is Nothing OrElse compound.GroupVector.Length = 0 Then
                Return Nothing
            End If

            Dim dg0 = CalculateFromGroupVector(compound.GroupVector)
            Dim RT = ThermodynamicConstants.default_R_in_kJ_per_mol_per_K * T_in_K

            Dim refMs = GetReferenceMicrospecies(compound)
            Dim charge = If(refMs?.Charge, compound.Charge)
            Dim nH = If(refMs?.NumberProtons, compound.ProtonCount)
            Dim nMg = If(refMs?.NumberMagnesiums, 0)

            Dim legendre = ThermodynamicConstants.LegendreTransform(pH, pMg, ionicStrength, T_in_K, charge, nH, nMg)
            Return dg0 + RT * legendre
        End Function

        ''' <summary>
        ''' 组件贡献法与组贡献法的统一入口：
        ''' 优先用微物种（组件贡献），无微物种则回退到基团向量（组贡献），均无则返回 Nothing。
        ''' </summary>
        Public Shared Function StandardFormationEnergy(
            compound As Compound,
            pH As Double, pMg As Double, ionicStrength As Double, T_in_K As Double) As Double?

            Dim cc = StandardFormationEnergyTransformed(compound, pH, pMg, ionicStrength, T_in_K)
            If cc.HasValue Then Return cc
            Return StandardFormationEnergyGroupContribution(compound, pH, pMg, ionicStrength, T_in_K)
        End Function

        ' =====================================================================
        ' 非变换（标准态）估算，作为回退/校验
        ' =====================================================================

        ''' <summary>
        ''' 估算标准生成能 ΔfG°（非变换）：优先基团向量，其次原子袋。
        ''' </summary>
        Public Shared Function CalculateStandardFormationEnergy(compound As Compound) As Double?
            If compound Is Nothing Then Return Nothing
            If compound.GroupVector IsNot Nothing AndAlso compound.GroupVector.Length > 0 Then
                Return CalculateFromGroupVector(compound.GroupVector)
            End If
            If compound.AtomBag IsNot Nothing AndAlso compound.AtomBag.Count > 0 Then
                Return EstimateFromAtomBag(compound.AtomBag)
            End If
            Return Nothing
        End Function

        ''' <summary>从基团向量计算标准生成能（组贡献法，近似值）。</summary>
        Public Shared Function CalculateFromGroupVector(groupVector As Double()) As Double
            Dim deltaG As Double = 0.0
            For i As Integer = 0 To groupVector.Length - 1
                deltaG += groupVector(i) * GroupContributionParameters.DefaultGroupEnergy(i + 1)
            Next
            Return deltaG
        End Function

        ''' <summary>从原子组成估算标准生成能（简化原子贡献法，近似值）。</summary>
        Public Shared Function EstimateFromAtomBag(atomBag As Dictionary(Of String, Integer)) As Double
            Dim atomicContributions As New Dictionary(Of String, Double) From {
                {"C", 15.0}, {"H", 5.0}, {"O", -140.0},
                {"N", 30.0}, {"S", -20.0}, {"P", -250.0}
            }
            Dim deltaG As Double = 0.0
            For Each kvp In atomBag
                If atomicContributions.ContainsKey(kvp.Key) Then
                    deltaG += atomicContributions(kvp.Key) * kvp.Value
                End If
            Next
            Return deltaG
        End Function

        ' =====================================================================
        ' 实例兼容方法（保留原 API）
        ' =====================================================================

        ''' <summary>从微物种数据计算标准生成能（组贡献近似：取主要微物种对应的基团向量估算）。</summary>
        Private Function CalculateFromMicrospecies(compound As Compound) As Double
            If compound.GroupVector IsNot Nothing AndAlso compound.GroupVector.Length > 0 Then
                Return CalculateFromGroupVector(compound.GroupVector)
            End If
            If compound.AtomBag IsNot Nothing AndAlso compound.AtomBag.Count > 0 Then
                Return EstimateFromAtomBag(compound.AtomBag)
            End If
            Return 0.0
        End Function

        ''' <summary>取用于 Legendre 变换的参考微物种（优先主要物种，否则质子数最多者）。</summary>
        Private Shared Function GetReferenceMicrospecies(compound As Compound) As CompoundMicrospecies
            If compound.Microspecies IsNot Nothing AndAlso compound.Microspecies.Count > 0 Then
                Dim major = compound.Microspecies.FirstOrDefault(Function(m) m.IsMajor)
                If major Is Nothing Then
                    major = compound.Microspecies.OrderByDescending(Function(m) m.NumberProtons).First()
                End If
                Return major
            End If
            Return Nothing
        End Function

        ''' <summary>计算两个微物种之间的转换能 ΔG = (ddg2 - ddg1)·RT。</summary>
        Public Function CalculateMicrospeciesTransitionEnergy(ms1 As CompoundMicrospecies, ms2 As CompoundMicrospecies) As Double
            Return (If(ms2.DdgOverRt, 0.0) - If(ms1.DdgOverRt, 0.0)) * RT
        End Function

        ''' <summary>计算化学反应的标准 Gibbs 自由能变化 ΔrG° = Σ n·ΔfG°。</summary>
        Public Function CalculateReactionGibbsEnergy(
            reactants As Dictionary(Of Compound, Double),
            products As Dictionary(Of Compound, Double)) As Double

            Dim deltaG As Double = 0.0
            For Each kvp In products
                Dim v = CalculateStandardFormationEnergy(kvp.Key)
                If v.HasValue Then deltaG += kvp.Value * v.Value
            Next
            For Each kvp In reactants
                Dim v = CalculateStandardFormationEnergy(kvp.Key)
                If v.HasValue Then deltaG -= kvp.Value * v.Value
            Next
            Return deltaG
        End Function

        ''' <summary>计算反应的平衡常数 K = exp(-ΔrG° / RT)。</summary>
        Public Function CalculateEquilibriumConstant(deltaG As Double) As Double
            Return Math.Exp(-deltaG / RT)
        End Function
    End Class

End Namespace
