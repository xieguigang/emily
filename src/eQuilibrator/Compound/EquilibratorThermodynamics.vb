' ============================================================================
' Equilibrator API 化合物热力学计算模块
' 基于基团贡献法估算化合物标准生成能
' ============================================================================

Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 基团贡献参数
    ' ========================================================================

    ''' <summary>
    ''' 基团贡献参数
    ''' 存储每个基团对标准生成能的贡献值
    ''' </summary>
    Public Class GroupContributionParameters

        ''' <summary>
        ''' 基团ID到标准生成能贡献的映射
        ''' 单位: kJ/mol
        ''' </summary>
        Public Property GroupEnergies As Dictionary(Of Integer, Double)

        ''' <summary>
        ''' 基团ID到基团名称的映射
        ''' </summary>
        Public Property GroupNames As Dictionary(Of Integer, String)

        ''' <summary>
        ''' 标准温度 (K)
        ''' </summary>
        Public Property StandardTemperature As Double = 298.15

        ''' <summary>
        ''' 气体常数 R (kJ/(mol·K))
        ''' </summary>
        Public ReadOnly Property GasConstant As Double = 0.008314

        ''' <summary>
        ''' RT 在标准温度下的值 (kJ/mol)
        ''' </summary>
        Public ReadOnly Property RT As Double
            Get
                Return GasConstant * StandardTemperature
            End Get
        End Property

        Public Sub New()
            GroupEnergies = New Dictionary(Of Integer, Double)()
            GroupNames = New Dictionary(Of Integer, String)()
            InitializeDefaultParameters()
        End Sub

        ''' <summary>
        ''' 初始化默认的基团贡献参数
        ''' 这些参数来自Alberty和eQuilibrator的研究
        ''' </summary>
        Private Sub InitializeDefaultParameters()
            ' 这里存储的是简化的示例参数
            ' 实际应用中需要从训练数据中获取完整的参数集

            ' 常见基团的贡献值 (kJ/mol)
            ' 这些值是基于Alberty的热力学数据

            ' 碳氢基团
            GroupEnergies(1) = -17.9   ' -CH3
            GroupEnergies(2) = 8.6     ' -CH2-
            GroupEnergies(3) = 26.0    ' >CH-
            GroupEnergies(4) = 40.0    ' >C<

            ' 含氧基团
            GroupEnergies(5) = -181.0  ' -OH (醇)
            GroupEnergies(6) = -361.0  ' -COOH (羧酸)
            GroupEnergies(7) = -276.0  ' -CHO (醛)
            GroupEnergies(8) = -289.0  ' >C=O (酮)
            GroupEnergies(9) = -235.0  ' -O- (醚)

            ' 含氮基团
            GroupEnergies(10) = 62.0   ' -NH2
            GroupEnergies(11) = 50.0   ' >NH
            GroupEnergies(12) = 27.0   ' >N-
            GroupEnergies(13) = 95.0   ' -CN

            ' 含硫基团
            GroupEnergies(14) = -42.0  ' -SH
            GroupEnergies(15) = -30.0  ' -S-

            ' 磷酸基团
            GroupEnergies(16) = -1025.0 ' -OPO3(2-)
            GroupEnergies(17) = -980.0  ' -OPO3H-
            GroupEnergies(18) = -935.0  ' -OPO3H2

            ' 环结构修正
            GroupEnergies(20) = 15.0   ' 五元环
            GroupEnergies(21) = 10.0   ' 六元环
            GroupEnergies(22) = 25.0   ' 芳香环

            ' 共轭系统修正
            GroupEnergies(30) = -15.0  ' 共轭双键

            ' 离子化修正
            GroupEnergies(40) = -20.0  ' 羧酸解离 (-COO-)
            GroupEnergies(41) = 30.0   ' 胺质子化 (-NH3+)

            ' 基团名称
            GroupNames(1) = "-CH3"
            GroupNames(2) = "-CH2-"
            GroupNames(3) = ">CH-"
            GroupNames(4) = ">C<"
            GroupNames(5) = "-OH"
            GroupNames(6) = "-COOH"
            GroupNames(7) = "-CHO"
            GroupNames(8) = ">C=O"
            GroupNames(9) = "-O-"
            GroupNames(10) = "-NH2"
            GroupNames(11) = ">NH"
            GroupNames(12) = ">N-"
            GroupNames(13) = "-CN"
            GroupNames(14) = "-SH"
            GroupNames(15) = "-S-"
            GroupNames(16) = "-OPO3(2-)"
            GroupNames(17) = "-OPO3H-"
            GroupNames(18) = "-OPO3H2"
            GroupNames(20) = "五元环"
            GroupNames(21) = "六元环"
            GroupNames(22) = "芳香环"
            GroupNames(30) = "共轭双键"
            GroupNames(40) = "-COO-"
            GroupNames(41) = "-NH3+"
        End Sub

        ''' <summary>
        ''' 获取基团的贡献值
        ''' </summary>
        Public Function GetGroupEnergy(groupId As Integer) As Double
            If GroupEnergies.ContainsKey(groupId) Then
                Return GroupEnergies(groupId)
            End If
            Return 0.0
        End Function

        ''' <summary>
        ''' 设置基团的贡献值
        ''' </summary>
        Public Sub SetGroupEnergy(groupId As Integer, energy As Double, Optional name As String = Nothing)
            GroupEnergies(groupId) = energy
            If name IsNot Nothing Then
                GroupNames(groupId) = name
            End If
        End Sub
    End Class

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
            Dim majorMs = compound.Microspecies?.FirstOrDefault(Function(m) m.IsMajor)
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
            Dim ddg1 As Double = If(ms1.DdGOverRt, 0.0)
            Dim ddg2 As Double = If(ms2.DdGOverRt, 0.0)

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

    ' ========================================================================
    ' 使用示例
    ' ========================================================================

    ''' <summary>
    ''' 使用示例类
    ''' </summary>
    Public Class EquilibratorExample

        ''' <summary>
        ''' 示例：计算磷酸根的标准生成能
        ''' </summary>
        Public Shared Sub ExampleCalculatePhosphateEnergy()
            ' 创建计算器
            Dim calculator As New StandardFormationEnergyCalculator()

            ' 创建磷酸根化合物对象 (示例数据)
            Dim phosphate As New Compound With {
                .Id = 12,
                .InChIKey = "NBIIXXVUZAFLBC-UHFFFAOYSA-L",
                .InChI = "InChI=1S/H3O4P/c1-5(2,3)4/h(H3,1,2,3,4)/p-2",
                .Smiles = "OP([O-])([O-])=O",
                .Mass = 95.979,
                .AtomBag = New Dictionary(Of String, Integer) From {
                    {"O", 4},
                    {"P", 1}
                },
                .GroupVector = New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                .Microspecies = New List(Of CompoundMicrospecies)()
            }

            ' 添加微物种数据
            ' H3PO4
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1,
                .CompoundId = 12,
                .Charge = 0,
                .NumberProtons = 3,
                .NumberMagnesiums = 0,
                .IsMajor = False,
                .DdGOverRt = -4.6 ' 相对于HPO4(2-)
            })

            ' H2PO4-
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 2,
                .CompoundId = 12,
                .Charge = -1,
                .NumberProtons = 2,
                .NumberMagnesiums = 0,
                .IsMajor = False,
                .DdGOverRt = -2.2
            })

            ' HPO4(2-)
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 3,
                .CompoundId = 12,
                .Charge = -2,
                .NumberProtons = 1,
                .NumberMagnesiums = 0,
                .IsMajor = True,
                .DdGOverRt = 0.0
            })

            ' PO4(3-)
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 4,
                .CompoundId = 12,
                .Charge = -3,
                .NumberProtons = 0,
                .NumberMagnesiums = 0,
                .IsMajor = False,
                .DdGOverRt = 6.2
            })

            ' 计算标准生成能
            Try
                Dim deltaG0 As Double = calculator.CalculateStandardFormationEnergy(phosphate)
                Console.WriteLine($"磷酸根的标准生成能 ΔfG° = {deltaG0:F2} kJ/mol")

                ' 计算pH 7.0下的生成能
                Dim deltaG7 As Double = calculator.CalculateFormationEnergyAtPH(phosphate, 7.0)
                Console.WriteLine($"磷酸根在pH 7.0的生成能 ΔfG'° = {deltaG7:F2} kJ/mol")

                ' 计算pH 7.0, pMg 3.0下的生成能
                Dim deltaG7Mg As Double = calculator.CalculateFormationEnergyAtPHAndMg(phosphate, 7.0, 3.0)
                Console.WriteLine($"磷酸根在pH 7.0, pMg 3.0的生成能 = {deltaG7Mg:F2} kJ/mol")

            Catch ex As Exception
                Console.WriteLine($"计算错误: {ex.Message}")
            End Try

            ' 计算微物种分布
            Dim distCalc As New MicrospeciesDistributionCalculator()
            Dim distribution = distCalc.CalculateDistribution(phosphate, 7.0)

            Console.WriteLine(vbCrLf & "pH 7.0下的微物种分布:")
            For Each kvp In distribution
                Dim ms = phosphate.Microspecies.FirstOrDefault(Function(m) m.Id = kvp.Key)
                If ms IsNot Nothing Then
                    Console.WriteLine($"  电荷 {ms.Charge}, 质子数 {ms.NumberProtons}: {kvp.Value * 100:F2}%")
                End If
            Next

            ' 获取主要物种
            Dim dominant = distCalc.GetDominantMicrospecies(phosphate, 7.0)
            If dominant IsNot Nothing Then
                Console.WriteLine($"  主要物种: 电荷 {dominant.Charge}, 质子数 {dominant.NumberProtons}")
            End If
        End Sub

        ''' <summary>
        ''' 示例：计算ATP水解反应的Gibbs自由能变化
        ''' ATP + H2O -> ADP + Pi
        ''' </summary>
        Public Shared Sub ExampleCalculateATPHydrolysis()
            Dim calculator As New StandardFormationEnergyCalculator()

            ' 创建化合物对象 (简化示例)
            Dim atp As New Compound With {
                .Id = 1,
                .Smiles = "ATP",
                .GroupVector = New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0, 0},
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            atp.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1,
                .CompoundId = 1,
                .Charge = -4,
                .NumberProtons = 12,
                .IsMajor = True,
                .DdGOverRt = 0.0
            })

            Dim adp As New Compound With {
                .Id = 2,
                .Smiles = "ADP",
                .GroupVector = New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0, 0},
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            adp.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 2,
                .CompoundId = 2,
                .Charge = -3,
                .NumberProtons = 12,
                .IsMajor = True,
                .DdGOverRt = 0.0
            })

            Dim pi As New Compound With {
                .Id = 3,
                .Smiles = "Pi",
                .GroupVector = New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0},
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            pi.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 3,
                .CompoundId = 3,
                .Charge = -2,
                .NumberProtons = 1,
                .IsMajor = True,
                .DdGOverRt = 0.0
            })

            Dim h2o As New Compound With {
                .Id = 4,
                .Smiles = "O",
                .GroupVector = New Double() {0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0},
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            h2o.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 4,
                .CompoundId = 4,
                .Charge = 0,
                .NumberProtons = 2,
                .IsMajor = True,
                .DdGOverRt = 0.0
            })

            ' 定义反应: ATP + H2O -> ADP + Pi
            Dim reactants As New Dictionary(Of Compound, Double) From {
                {atp, 1.0},
                {h2o, 1.0}
            }

            Dim products As New Dictionary(Of Compound, Double) From {
                {adp, 1.0},
                {pi, 1.0}
            }

            ' 计算反应Gibbs自由能变化
            Try
                Dim deltaG As Double = calculator.CalculateReactionGibbsEnergy(reactants, products)
                Console.WriteLine($"ATP水解反应 ΔrG° = {deltaG:F2} kJ/mol")

                Dim keq As Double = calculator.CalculateEquilibriumConstant(deltaG)
                Console.WriteLine($"平衡常数 K = {keq:E4}")

                ' 计算pH 7.0下的值
                Dim deltaG7 As Double = calculator.CalculateReactionGibbsEnergyAtPH(reactants, products, 7.0)
                Console.WriteLine($"ATP水解反应在pH 7.0: ΔrG'° = {deltaG7:F2} kJ/mol")

            Catch ex As Exception
                Console.WriteLine($"计算错误: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' 示例：从数据库加载数据并计算
        ''' </summary>
        Public Shared Sub ExampleLoadFromDatabase()
            ' 假设已经从SQLite数据库读取数据
            ' 这里展示如何使用这些数据

            Dim calculator As New StandardFormationEnergyCalculator()

            ' 模拟从数据库读取的化合物列表
            Dim compounds As New List(Of Compound)()

            ' 添加化合物 (实际应用中从数据库读取)
            ' compounds = LoadCompoundsFromDatabase("path/to/database.db")

            ' 加载到计算器
            calculator.LoadCompounds(compounds)

            ' 通过ID获取化合物并计算
            Dim compoundId As Integer = 12 ' 磷酸根的ID
            Dim compound = calculator.GetCompound(compoundId)

            If compound IsNot Nothing Then
                Try
                    Dim deltaG As Double = calculator.CalculateStandardFormationEnergy(compound)
                    Console.WriteLine($"化合物 {compound.InChIKey} 的标准生成能 = {deltaG:F2} kJ/mol")
                Catch ex As Exception
                    Console.WriteLine($"计算失败: {ex.Message}")
                End Try
            Else
                Console.WriteLine($"未找到ID为 {compoundId} 的化合物")
            End If
        End Sub
    End Class

End Namespace
