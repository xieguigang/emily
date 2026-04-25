' ============================================================================
' Equilibrator API 化合物热力学计算模块
' 基于基团贡献法估算化合物标准生成能
' ============================================================================

Imports eQuilibrator.Cache

Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 标准生成能计算器
    ' ========================================================================

    ''' <summary>
    ''' 化合物标准生成能计算器
    ''' 使用基团贡献法估算ΔfG°
    ''' </summary>
    Public Class StandardFormationEnergyCalculator

        Private _groupParams As GroupContributionParameters
        Private _compounds As Dictionary(Of Integer, Compound)
        Private _microspecies As Dictionary(Of Integer, CompoundMicrospecies)

        ''' <summary>
        ''' 标准温度 (K)
        ''' </summary>
        Public Const STANDARD_TEMPERATURE As Double = 298.15

        ''' <summary>
        ''' 气体常数 R (kJ/(mol·K))
        ''' </summary>
        Public Const GAS_CONSTANT As Double = 0.008314

        ''' <summary>
        ''' RT 在标准温度下 (kJ/mol)
        ''' </summary>
        Public Const RT As Double = GAS_CONSTANT * STANDARD_TEMPERATURE

        ''' <summary>
        ''' 法拉第常数 (C/mol)
        ''' </summary>
        Public Const FARADAY_CONSTANT As Double = 96485.33212

        ''' <summary>
        ''' 标准pH值
        ''' </summary>
        Public Property StandardPH As Double = 7.0

        ''' <summary>
        ''' 标准Mg2+浓度 (M)
        ''' </summary>
        Public Property StandardMgConcentration As Double = 0.001

        Public Sub New()
            _groupParams = New GroupContributionParameters()
            _compounds = New Dictionary(Of Integer, Compound)()
            _microspecies = New Dictionary(Of Integer, CompoundMicrospecies)()
        End Sub

        ''' <summary>
        ''' 使用自定义基团参数初始化
        ''' </summary>
        Public Sub New(groupParams As GroupContributionParameters)
            _groupParams = groupParams
            _compounds = New Dictionary(Of Integer, Compound)()
            _microspecies = New Dictionary(Of Integer, CompoundMicrospecies)()
        End Sub

        ''' <summary>
        ''' 加载化合物数据
        ''' </summary>
        Public Sub LoadCompounds(compounds As List(Of Compound))
            _compounds.Clear()
            _microspecies.Clear()

            For Each compound In compounds
                ' 解析BLOB数据
                If compound.GroupVectorRaw IsNot Nothing Then
                    compound.GroupVector = EquilibratorBlobParser.ParseGroupVector(compound.GroupVectorRaw)
                End If
                If compound.AtomBagRaw IsNot Nothing Then
                    compound.AtomBag = EquilibratorBlobParser.ParseAtomBag(compound.AtomBagRaw)
                End If
                If compound.DissociationConstantsRaw IsNot Nothing Then
                    compound.DissociationConstants = EquilibratorBlobParser.ParseDissociationConstants(compound.DissociationConstantsRaw)
                End If

                _compounds(compound.Id) = compound

                ' 加载微物种数据
                If compound.Microspecies IsNot Nothing Then
                    For Each ms In compound.Microspecies
                        _microspecies(ms.Id) = ms
                    Next
                End If
            Next
        End Sub

        ''' <summary>
        ''' 获取化合物
        ''' </summary>
        Public Function GetCompound(compoundId As Integer) As Compound
            If _compounds.ContainsKey(compoundId) Then
                Return _compounds(compoundId)
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' 通过InChI Key获取化合物
        ''' </summary>
        Public Function GetCompoundByInChIKey(inchiKey As String) As Compound
            Return _compounds.Values.FirstOrDefault(Function(c) c.InChIKey = inchiKey)
        End Function

        ''' <summary>
        ''' 计算化合物的标准生成能 ΔfG°
        ''' 使用基团贡献法
        ''' </summary>
        ''' <param name="compound">化合物对象</param>
        ''' <returns>标准生成能 (kJ/mol)</returns>
        Public Function CalculateStandardFormationEnergy(compound As Compound) As Double
            If compound Is Nothing Then
                Throw New ArgumentNullException("compound")
            End If

            ' 方法1: 如果有group_vector，使用基团贡献法
            If compound.GroupVector IsNot Nothing AndAlso compound.GroupVector.Length > 0 Then
                Return CalculateFromGroupVector(compound.GroupVector)
            End If

            ' 方法2: 如果有微物种数据，使用微物种数据计算
            If compound.Microspecies IsNot Nothing AndAlso compound.Microspecies.Count > 0 Then
                Return CalculateFromMicrospecies(compound)
            End If

            ' 方法3: 使用原子贡献法估算
            If compound.AtomBag IsNot Nothing AndAlso compound.AtomBag.Count > 0 Then
                Return EstimateFromAtomBag(compound.AtomBag)
            End If

            Throw New InvalidOperationException("无法计算标准生成能：缺少必要的数据")
        End Function

        ''' <summary>
        ''' 从基团向量计算标准生成能
        ''' </summary>
        Private Function CalculateFromGroupVector(groupVector As Double()) As Double
            Dim deltaG As Double = 0.0

            ' 基团向量中的每个元素代表一个基团的计数
            ' 将计数乘以对应的基团贡献值
            For i As Integer = 0 To groupVector.Length - 1
                Dim count As Double = groupVector(i)
                Dim groupEnergy As Double = _groupParams.GetGroupEnergy(i + 1)
                deltaG += count * groupEnergy
            Next

            Return deltaG
        End Function

        ''' <summary>
        ''' 从微物种数据计算标准生成能
        ''' </summary>
        Private Function CalculateFromMicrospecies(compound As Compound) As Double
            ' 找到主要微物种
            Dim majorMs = compound.Microspecies.FirstOrDefault(Function(m) m.IsMajor)

            If majorMs Is Nothing Then
                ' 如果没有标记主要物种，选择电荷最接近中性的
                majorMs = compound.Microspecies.OrderBy(Function(m) Math.Abs(m.Charge)).FirstOrDefault()
            End If

            If majorMs Is Nothing Then
                Return 0.0
            End If

            ' 主要微物种的ΔG°就是化合物的标准生成能
            ' ddg_over_rt是相对于主要物种的ΔG/RT
            ' 如果主要物种的ddg_over_rt为null或0，需要估算

            Dim deltaG As Double = 0.0

            ' 使用基团贡献法估算主要物种的生成能
            If compound.GroupVector IsNot Nothing Then
                deltaG = CalculateFromGroupVector(compound.GroupVector)
            End If

            Return deltaG
        End Function

        ''' <summary>
        ''' 从原子组成估算标准生成能
        ''' 使用简化的原子贡献法
        ''' </summary>
        Private Function EstimateFromAtomBag(atomBag As Dictionary(Of String, Integer)) As Double
            ' 原子贡献值 (kJ/mol)
            ' 这些是简化的估算值
            Dim atomicContributions As New Dictionary(Of String, Double) From {
                {"C", 15.0},
                {"H", 5.0},
                {"O", -140.0},
                {"N", 30.0},
                {"S", -20.0},
                {"P", -250.0}
            }

            Dim deltaG As Double = 0.0

            For Each kvp In atomBag
                Dim element As String = kvp.Key
                Dim count As Integer = kvp.Value

                If atomicContributions.ContainsKey(element) Then
                    deltaG += atomicContributions(element) * count
                End If
            Next

            Return deltaG
        End Function

        ''' <summary>
        ''' 计算化合物在特定pH下的标准生成能
        ''' ΔfG'° = ΔfG° + nH * RT * ln(10) * (pH - 7)
        ''' </summary>
        ''' <param name="compound">化合物对象</param>
        ''' <param name="pH">pH值</param>
        ''' <returns>标准生成能 (kJ/mol)</returns>
        Public Function CalculateFormationEnergyAtPH(compound As Compound, pH As Double) As Double
            ' 首先计算标准生成能
            Dim deltaG0 As Double = CalculateStandardFormationEnergy(compound)

            ' 计算pH修正
            ' 找到主要微物种的质子数
            Dim majorMs = compound.Microspecies.Where(Function(m) m.IsMajor).FirstOrDefault
            Dim nH As Integer = If(majorMs?.NumberProtons, 0)

            ' pH修正项
            Dim pHCorrection As Double = nH * RT * Math.Log(10) * (pH - StandardPH)

            Return deltaG0 + pHCorrection
        End Function

        ''' <summary>
        ''' 计算化合物在特定pH和Mg2+浓度下的标准生成能
        ''' </summary>
        ''' <param name="compound">化合物对象</param>
        ''' <param name="pH">pH值</param>
        ''' <param name="pMg">pMg = -log10[Mg2+]</param>
        ''' <returns>标准生成能 (kJ/mol)</returns>
        Public Function CalculateFormationEnergyAtPHAndMg(compound As Compound, pH As Double, pMg As Double) As Double
            ' 计算pH修正后的生成能
            Dim deltaG As Double = CalculateFormationEnergyAtPH(compound, pH)

            ' 计算Mg2+修正
            If compound.MagnesiumDissociationConstants IsNot Nothing AndAlso
               compound.MagnesiumDissociationConstants.Count > 0 Then

                ' Mg2+结合修正
                Dim mgCorrection As Double = 0.0
                For Each mgDiss In compound.MagnesiumDissociationConstants
                    ' Mg解离常数修正
                    Dim nMg As Integer = mgDiss.NumberMagnesiums
                    Dim kd As Double = mgDiss.DissociationConstant

                    ' 修正项: -nMg * RT * ln(1 + [Mg2+]/Kd)
                    Dim mgConc As Double = Math.Pow(10, -pMg)
                    mgCorrection -= nMg * RT * Math.Log(1 + mgConc / kd)
                Next

                deltaG += mgCorrection
            End If

            Return deltaG
        End Function

        ''' <summary>
        ''' 计算两个微物种之间的转换能
        ''' </summary>
        Public Function CalculateMicrospeciesTransitionEnergy(ms1 As CompoundMicrospecies, ms2 As CompoundMicrospecies) As Double
            ' ΔG = (ddg2 - ddg1) * RT
            Dim ddg1 As Double = ms1.DdgOverRt
            Dim ddg2 As Double = ms2.DdgOverRt

            Return (ddg2 - ddg1) * RT
        End Function

        ''' <summary>
        ''' 计算化学反应的标准Gibbs自由能变化
        ''' ΔrG° = Σ(n_products * ΔfG°_products) - Σ(n_reactants * ΔfG°_reactants)
        ''' </summary>
        ''' <param name="reactants">反应物列表及其化学计量系数</param>
        ''' <param name="products">产物列表及其化学计量系数</param>
        ''' <returns>反应标准Gibbs自由能变化 (kJ/mol)</returns>
        Public Function CalculateReactionGibbsEnergy(
            reactants As Dictionary(Of Compound, Double),
            products As Dictionary(Of Compound, Double)) As Double

            Dim deltaG As Double = 0.0

            ' 产物的生成能总和
            For Each kvp In products
                Dim compound As Compound = kvp.Key
                Dim stoich As Double = kvp.Value
                deltaG += stoich * CalculateStandardFormationEnergy(compound)
            Next

            ' 减去反应物的生成能总和
            For Each kvp In reactants
                Dim compound As Compound = kvp.Key
                Dim stoich As Double = kvp.Value
                deltaG -= stoich * CalculateStandardFormationEnergy(compound)
            Next

            Return deltaG
        End Function

        ''' <summary>
        ''' 计算反应的平衡常数
        ''' K = exp(-ΔrG° / RT)
        ''' </summary>
        Public Function CalculateEquilibriumConstant(deltaG As Double) As Double
            Return Math.Exp(-deltaG / RT)
        End Function

        ''' <summary>
        ''' 计算反应在特定pH下的Gibbs自由能变化
        ''' </summary>
        Public Function CalculateReactionGibbsEnergyAtPH(
            reactants As Dictionary(Of Compound, Double),
            products As Dictionary(Of Compound, Double),
            pH As Double) As Double

            Dim deltaG As Double = 0.0

            ' 产物的生成能总和
            For Each kvp In products
                Dim compound As Compound = kvp.Key
                Dim stoich As Double = kvp.Value
                deltaG += stoich * CalculateFormationEnergyAtPH(compound, pH)
            Next

            ' 减去反应物的生成能总和
            For Each kvp In reactants
                Dim compound As Compound = kvp.Key
                Dim stoich As Double = kvp.Value
                deltaG -= stoich * CalculateFormationEnergyAtPH(compound, pH)
            Next

            Return deltaG
        End Function
    End Class

End Namespace
