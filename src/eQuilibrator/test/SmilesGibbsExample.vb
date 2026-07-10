' ============================================================================
' SmilesGibbsExample.vb
' 演示：对 CSV 数据库中不存在的化合物，
'   1) 通过 SMILES → 分子图 → 官能团列表 → 基团向量，
'   2) 用组贡献法估算其在任意 pH 下的标准生成吉布斯自由能 ΔfG'°，
'   3) 将自定义化合物加入缓存，参与反应的热力学计算。
'
' 重要说明（结果只能用于定性/演示，不能对标 equilibrator.org 定量值）：
'   * 本项目基团能量为占位值（非官方训练结果，见 GroupContributionParameters）。
'   * 对可解离化合物：BuildCompoundFromSMILES 先用 PkaPredictor 预测 pKa，再用
'     MicrospeciesBuilder 构造微物种阶梯，最终走组件贡献法（微物种 Legendre 分区函数
'     + 组贡献基线）。此时 Legendre 变换的 nH 为“可解离质子数”，pH 形状正确。
'   * 对无可解离位点的化合物（如醇）：回退组贡献法，仍用“分子总质子数”近似 nH。
'   * FunctionalGroupDetector 的基团词汇与官方 component-contribution 不保证一致，
'     未匹配上的基团会被忽略。
' ============================================================================

Imports System.Linq
Imports System.Text.RegularExpressions
Imports eQuilibrator.SmilesChem
Imports eQuilibrator.EquilibratorApi.Core
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorThermodynamics

Module SmilesGibbsExample

    ''' <summary>
    ''' 入口：运行 SMILES → 吉布斯自由能 的完整示例。
    ''' </summary>
    Public Sub Demonstrate()
        Console.WriteLine()
        Console.WriteLine("======================================================")
        Console.WriteLine(" SMILES → 基团 → ΔfG'°（CSV 外化合物）完整示例")
        Console.WriteLine("======================================================")

        DemonstrateSingleCompound("CC(=O)O", "乙酸 (acetic acid)")
        DemonstrateSingleCompound("c1ccccc1", "苯 (benzene)")
        DemonstrateSingleCompound("CCO", "乙醇 (ethanol)")

        DemonstratePkaAndMicrospecies()
        DemonstrateReactionWithCustomCompound()
    End Sub

    ' ------------------------------------------------------------------
    ' 1) 单个化合物：SMILES → Compound → ΔfG'°(pH)
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' 把一个 SMILES 解析、拆解基团、构造 Compound，并打印其在若干 pH 下的 ΔfG'°。
    ''' </summary>
    Private Sub DemonstrateSingleCompound(smiles As String, label As String)
        Console.WriteLine()
        Console.WriteLine($"--- 化合物: {label}  (SMILES: {smiles}) ---")

        Dim compound = BuildCompoundFromSMILES(smiles, label)
        If compound Is Nothing Then
            Console.WriteLine("   解析失败，跳过。")
            Return
        End If

        Console.WriteLine($"   分子式    : {compound.Formula}")
        Console.WriteLine($"   原子袋    : {String.Join(", ", compound.AtomBag.Select(Function(k) k.Key & "=" & k.Value))}")
        Console.WriteLine($"   总电荷    : {compound.Charge}")
        Console.WriteLine($"   总质子数  : {compound.ProtonCount}")

        ' 打印识别出的官能团（仅用于展示分解结果）
        Dim mol = (New SmilesParser()).Parse(smiles)
        Dim groups = (New FunctionalGroupDetector()).Detect(mol)
        Console.WriteLine($"   识别基团  : {String.Join(" | ", groups.Select(Function(g) g.Notation))}")

        If compound.GroupVector Is Nothing OrElse compound.GroupVector.Length = 0 Then
            Console.WriteLine("   ⚠ 无可用基团向量，组贡献法无法估算（基团均未匹配参数表）。")
            Return
        End If

        Console.WriteLine($"   基团向量  : {String.Join(", ", compound.GroupVector.Select(Function(v) v.ToString("F0")))}")
        Console.WriteLine("   pH 依赖的 ΔfG'° (组贡献法, 近似):")
        For Each pH In New Double() {5.0, 7.0, 9.0}
            Dim dg = StandardFormationEnergyCalculator.StandardFormationEnergyGroupContribution(
                         compound, pH,
                         ThermodynamicConstants.DefaultPMg,
                         ThermodynamicConstants.DefaultIonicStrength,
                         ThermodynamicConstants.DefaultTemperature)
            If dg.HasValue Then
                Console.WriteLine($"     pH {pH,4:F1}  ->  ΔfG'° = {dg.Value,12:F4} kJ/mol")
            End If
        Next
    End Sub

    ' ------------------------------------------------------------------
    ' 2) 反应：自定义化合物加入缓存后参与 ΔrG'° 计算
    ' ------------------------------------------------------------------

    Private Sub DemonstrateReactionWithCustomCompound()
        Console.WriteLine()
        Console.WriteLine("--- 反应热力学（自定义化合物入缓存）---")
        Console.WriteLine("   反应式: 2 CCO <=> CCOCC + H2O  (乙醇脱水生成二乙醚)")

        Dim cc As New ComponentContribution()
        cc.PH = 7.0
        cc.PMg = ThermodynamicConstants.DefaultPMg
        cc.IonicStrength = ThermodynamicConstants.DefaultIonicStrength
        cc.Temperature = ThermodynamicConstants.DefaultTemperature

        ' 必须先把 CSV 外的化合物加入缓存，否则反应式中的 id 会被当作 missing 而不计入。
        ' 这里两种有机物都能被官能团检测器拆解成已知基团；H2O 由缓存以“水”特例处理（ΔfG'°=0）。
        cc.Cache.AddCompound(BuildCompoundFromSMILES("CCO", "CCO"))
        cc.Cache.AddCompound(BuildCompoundFromSMILES("CCOCC", "CCOCC"))

        Dim result = cc.StandardDgPrime("2 CCO <=> CCOCC + H2O")
        Console.WriteLine($"   ΔrG'°   (标准变换) = {result.StandardDgPrime.Value,12:F4} ± {result.Uncertainty.Value,8:F4} kJ/mol")
        Console.WriteLine($"   ΔrG'    (指定浓度) = {result.DgPrime.Value,12:F4} kJ/mol")
        Console.WriteLine($"   平衡常数 K'        = {result.EquilibriumConstant,12:E3}")
        Console.WriteLine($"   反应方向            = {cc.GetReactionDirection(cc.Reaction("2 CCO <=> CCOCC + H2O"))}")
    End Sub

    ' ------------------------------------------------------------------
    ' 3) pKa 预测 → 微物种 → 组件贡献法 ΔfG'°
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' 演示：对某 SMILES，先用官能团预测 pKa，再构造微物种，
    ''' 最后用组件贡献法（微物种分区函数 + 组贡献基线）计算多 pH 下的 ΔfG'°。
    ''' </summary>
    Private Sub DemonstratePkaAndMicrospecies()
        Console.WriteLine()
        Console.WriteLine("======================================================")
        Console.WriteLine(" pKa 预测 → 微物种 → 组件贡献法 ΔfG'°（推荐方案）")
        Console.WriteLine("======================================================")

        DemonstratePkaMicrospeciesItem("CC(=O)O", "乙酸 (acetic acid)")
        DemonstratePkaMicrospeciesItem("NCC(=O)O", "甘氨酸 (glycine)")
        DemonstratePkaMicrospeciesItem("c1ccc(cc1)O", "苯酚 (phenol)")
        DemonstratePkaMicrospeciesItem("c1ccncc1", "吡啶 (pyridine)")
        DemonstratePkaMicrospeciesItem("CCO", "乙醇 (ethanol, 无可解离位点)")
    End Sub

    ''' <summary>
    ''' 单个化合物：打印 pKa 预测、微物种列表、pH=7 微物种分布，以及多 pH 的 ΔfG'°。
    ''' </summary>
    Private Sub DemonstratePkaMicrospeciesItem(smiles As String, label As String)
        Console.WriteLine()
        Console.WriteLine($"--- {label}  (SMILES: {smiles}) ---")

        Dim mol = (New SmilesParser()).Parse(smiles)
        If mol Is Nothing Then
            Console.WriteLine("   解析失败，跳过。")
            Return
        End If

        Dim groups = (New FunctionalGroupDetector()).Detect(mol)
        Dim predictions = PkaPredictor.Predict(mol, groups)

        Console.WriteLine("   预测 pKa:")
        If predictions.Count = 0 Then
            Console.WriteLine("     （无识别到的解离位点）")
        Else
            For Each p In predictions
                Dim tag = If(p.IsSignificant, If(p.IsAcidic, "酸性", "碱性"), "水相中基本不离解(忽略)")
                Console.WriteLine($"     {p.GroupNotation,-9} pKa={p.Pka,7:F2}  [{tag}]  {p.Description}")
            Next
        End If

        Dim compound = BuildCompoundFromSMILES(smiles, label)
        If compound Is Nothing Then Return

        If compound.Microspecies Is Nothing OrElse compound.Microspecies.Count = 0 Then
            Console.WriteLine("   无微物种 → 组贡献法回退（无 pH 依赖形状修正）。")
        Else
            Console.WriteLine($"   微物种数 : {compound.Microspecies.Count}")
            For Each ms In compound.Microspecies
                Console.WriteLine($"     k 电荷={ms.Charge,3} 质子={ms.NumberProtons,3} ddg_over_rt={If(ms.DdgOverRt, 0.0),10:F3}")
            Next

            Console.WriteLine("   pH=7 微物种分布 (按分区函数 exp(-x)，与 ΔfG'° 一致):")
            Dim dist = ComputeMicrospeciesDistribution(compound, 7.0)
            For Each kvp In dist
                Dim ms = compound.Microspecies.First(Function(m) m.Id = kvp.Key)
                Console.WriteLine($"     电荷={ms.Charge,3} 质子={ms.NumberProtons,3} 摩尔分数={kvp.Value,8:P3}")
            Next
        End If

        Console.WriteLine("   ΔfG'° (kJ/mol, 组件贡献 + 组贡献基线):")
        For Each pH In New Double() {5.0, 7.0, 9.0}
            Dim dg = ComputeDgPrime(compound, pH)
            If dg.HasValue Then
                Console.WriteLine($"     pH {pH,4:F1} -> ΔfG'° = {dg.Value,12:F4}")
            Else
                Console.WriteLine($"     pH {pH,4:F1} -> (无法估算)")
            End If
        Next
    End Sub

    ''' <summary>
    ''' 计算化合物在指定 pH 下的 ΔfG'°：优先组件贡献（微物种 + 组贡献基线），
    ''' 无微物种时回退组贡献法。与 ComponentContribution 中 StandardDgPrimeCore 的逻辑一致。
    ''' </summary>
    Private Function ComputeDgPrime(compound As Compound, pH As Double) As Double?
        If compound.Microspecies IsNot Nothing AndAlso compound.Microspecies.Count > 0 Then
            Dim baseE As Double = 0.0
            If compound.GroupVector IsNot Nothing AndAlso compound.GroupVector.Length > 0 Then
                baseE = StandardFormationEnergyCalculator.CalculateFromGroupVector(compound.GroupVector)
            End If
            Dim dg = StandardFormationEnergyCalculator.StandardFormationEnergyTransformed(
                         compound, pH,
                         ThermodynamicConstants.DefaultPMg,
                         ThermodynamicConstants.DefaultIonicStrength,
                         ThermodynamicConstants.DefaultTemperature, baseE)
            If dg.HasValue Then Return dg
        End If
        Return StandardFormationEnergyCalculator.StandardFormationEnergyGroupContribution(
                   compound, pH,
                   ThermodynamicConstants.DefaultPMg,
                   ThermodynamicConstants.DefaultIonicStrength,
                   ThermodynamicConstants.DefaultTemperature)
    End Function

    ''' <summary>
    ''' 按与 StandardFormationEnergyTransformed 一致的分区函数 exp(-x) 计算各微物种摩尔分数。
    ''' （注意：既有的 MicrospeciesDistributionCalculator 的质子项符号与分区函数相反，
    '''  此处直接使用 TransformedDdgOverRt 以保证与 ΔfG'° 一致。）
    ''' </summary>
    Private Function ComputeMicrospeciesDistribution(compound As Compound, pH As Double) As Dictionary(Of Integer, Double)
        Dim dist As New Dictionary(Of Integer, Double)()
        If compound.Microspecies Is Nothing OrElse compound.Microspecies.Count = 0 Then Return dist

        Dim weights As New List(Of Double)()
        Dim total As Double = 0.0
        For Each ms In compound.Microspecies
            Dim x = ms.TransformedDdgOverRt(pH, ThermodynamicConstants.DefaultPMg,
                                            ThermodynamicConstants.DefaultIonicStrength,
                                            ThermodynamicConstants.DefaultTemperature)
            Dim w = Math.Exp(-x)
            weights.Add(w)
            total += w
        Next
        For i As Integer = 0 To compound.Microspecies.Count - 1
            dist(compound.Microspecies(i).Id) = If(total > 0, weights(i) / total, 0.0)
        Next
        Return dist
    End Function

    ' ------------------------------------------------------------------
    ' 核心：SMILES → Compound
    ' ------------------------------------------------------------------

    ''' <summary>
    ''' 将 SMILES 解析为分子图、识别官能团、映射到基团贡献参数表的基团 ID，
    ''' 并构造一个可用于组贡献法估算的 Compound。
    ''' 返回 Nothing 表示解析失败。
    ''' </summary>
    Public Function BuildCompoundFromSMILES(smiles As String, id As String) As Compound
        Dim parser As New SmilesParser()
        Dim mol As SmilesMolecule = parser.Parse(smiles)
        If mol Is Nothing Then Return Nothing

        Dim detector As New FunctionalGroupDetector()
        Dim groups As List(Of FunctionalGroup) = detector.Detect(mol)

        ' 把检测器识别的 Notation 映射到 GroupContributionParameters 的基团 ID 并计数
        Dim counts As New Dictionary(Of Integer, Integer)()
        For Each g In groups
            Dim gid As Integer? = MapNotationToGroupId(g.Notation)
            If gid.HasValue Then
                counts(gid.Value) = If(counts.ContainsKey(gid.Value), counts(gid.Value), 0) + 1
            End If
        Next

        ' 构建 GroupVector（下标 = groupId - 1）
        Dim groupVector As Double()
        If counts.Count = 0 Then
            groupVector = New Double(-1) {}   ' 空向量（组贡献法将返回 Nothing）
        Else
            Dim maxId = counts.Keys.Max()
            groupVector = New Double(maxId - 1) {}
            For Each kvp In counts
                groupVector(kvp.Key - 1) = kvp.Value
            Next
        End If

        ' 从分子图推算原子袋（含隐式氢）、总电荷与总质子数
        Dim atomBag = ComputeAtomBag(mol)
        Dim totalCharge = mol.Atoms.Sum(Function(a) a.Charge)
        Dim protonCount = If(atomBag.ContainsKey("H"), atomBag("H"), 0)

        ' ---- SMILES 衍生：pKa 预测 → 构造微物种（组件贡献路径）----
        ' 仅用已识别的基团推断可解离位点；对酚/醇、吡啶/吡咯做环上下文修正。
        Dim predictions = PkaPredictor.Predict(mol, groups)
        Dim microspecies = MicrospeciesBuilder.Build(predictions, totalCharge, protonCount)

        Return New Compound() With {
            .Id = id,
            .SMILES = smiles,
            .AtomBag = atomBag,
            .GroupVector = groupVector,
            .Charge = totalCharge,
            .ProtonCount = protonCount,
            .StandardFormationEnergy = StandardFormationEnergyCalculator.CalculateFromGroupVector(groupVector),
            .IsSmilesDerived = True,
            .Microspecies = microspecies
        }
    End Function

    ''' <summary>
    ''' 推算元素组成（含隐式氢），用于守恒校验与质子数。
    ''' 显式氢原子（如 [H+]）按独立原子计数；其余重原子的隐式氢由 SmilesAtom 计算。
    ''' </summary>
    Private Function ComputeAtomBag(mol As SmilesMolecule) As Dictionary(Of String, Integer)
        Dim bag As New Dictionary(Of String, Integer)()
        For Each a In mol.Atoms
            If a.Symbol = "H" Then
                Increment(bag, "H")          ' 显式氢原子
                Continue For
            End If
            Increment(bag, a.Symbol)          ' 重原子
            Dim h = a.GetImplicitHCount()     ' 该重原子所连的隐式氢
            If h > 0 Then Increment(bag, "H", h)
        Next
        Return bag
    End Function

    ''' <summary>
    ''' 将 FunctionalGroupDetector 的 Notation 映射到 GroupContributionParameters 的基团 ID。
    ''' 返回 Nothing 表示该基团暂无对应参数（将被忽略）。
    ''' </summary>
    Private Function MapNotationToGroupId(notation As String) As Integer?
        ' 简单官能团：与参数表 ID 直接对应
        Dim direct As New Dictionary(Of String, Integer) From {
            {"-CH3", 1}, {"-CH2-", 2}, {"=CH-", 3}, {">C<", 4},
            {"-OH", 5}, {"-COOH", 6}, {"-CHO", 7}, {">C=O", 8}, {"-C=O", 8}, {"-O-", 9},
            {"-NH2", 10}, {"-NH-", 11}, {">N-", 12}, {"-C#N", 13},
            {"-SH", 14}, {"-S-", 15},
            {"-OPO3(2-)", 16}, {"-OPO3H-", 17}, {"-OPO3H2", 18},
            {"-COO-", 40}
        }
        If direct.ContainsKey(notation) Then Return direct(notation)

        ' 芳香环片段 → 芳香环修正 (id 22)
        If notation.StartsWith("-C6H5") OrElse notation.StartsWith("-C5H4N") OrElse
           notation.StartsWith("-C4H3O") OrElse notation.StartsWith("-C4H3S") OrElse
           notation.StartsWith("-C4H4N") OrElse notation = "ArHet" Then
            Return 22
        End If

        ' 环烷烃片段 C{n}H{2n} → 五元环(20) / 六元环(21)
        Dim m = Regex.Match(notation, "^C(\d+)H\d+$")
        If m.Success Then
            Dim n = Integer.Parse(m.Groups(1).Value)
            Return If(n >= 6, 21, 20)
        End If

        ' 杂环片段 Het / Het[x] → 六元环近似 (21)
        If notation.StartsWith("Het") Then Return 21

        Return Nothing
    End Function

    ''' <summary>字典计数自增辅助。</summary>
    Private Sub Increment(bag As Dictionary(Of String, Integer), key As String, Optional by As Integer = 1)
        bag(key) = If(bag.ContainsKey(key), bag(key), 0) + by
    End Sub

End Module
