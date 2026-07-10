' ============================================================================
' eQuilibrator VB.NET —— 使用示例模块
' 文档化地演示各算法的典型用法，供 Program.vb 调用。
' ============================================================================

Imports eQuilibrator.EquilibratorApi.Core
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorThermodynamics
Imports eQuilibrator.EquilibratorApi.Core.Parsers

Module Examples

    ''' <summary>演示：从 CSV 懒加载一个化合物并打印其结构信息。</summary>
    Public Sub DemonstrateCompoundLoading(cache As CompoundCache, accession As String)
        Console.WriteLine()
        Console.WriteLine("--- 1) 化合物加载（组件贡献数据源） ---")
        Dim c = cache.GetCompound(accession)
        If c Is Nothing Then
            Console.WriteLine($"   未找到化合物: {accession}")
            Return
        End If
        Console.WriteLine($"   标识      : {c.Id}")
        Console.WriteLine($"   InChIKey : {c.InChIKey}")
        Console.WriteLine($"   分子式    : {c.Formula}")
        Console.WriteLine($"   分子量    : {If(c.MolecularWeight.HasValue, c.MolecularWeight.Value.ToString("F3"), "n/a")} g/mol")
        Console.WriteLine($"   微物种数  : {If(c.Microspecies, New List(Of CompoundMicrospecies)()).Count}")
        Console.WriteLine($"   基团向量  : {If(c.GroupVector Is Nothing, "n/a", String.Join(", ", c.GroupVector.Take(20).Select(Function(v) v.ToString("F0"))) & (If(c.GroupVector.Length > 20, " ...", "")))}")
        Console.WriteLine($"   解离常数  : {If(c.DissociationConstants Is Nothing, "n/a", String.Join(", ", c.DissociationConstants.Select(Function(d) d.ToString("F2"))))}")
        Console.WriteLine($"   标识符    : {If(c.Identifiers Is Nothing OrElse c.Identifiers.Count = 0, "n/a", c.Identifiers.Count.ToString() & " 个")}")
    End Sub

    ''' <summary>演示：组件贡献法下，化合物 ΔfG'° 随 pH 的变化（验证 pH 依赖形状）。</summary>
    Public Sub DemonstratePhDependence(cc As ComponentContribution, accession As String)
        Console.WriteLine()
        Console.WriteLine("--- 2) 组件贡献法：ΔfG'° 的 pH 依赖 ---")
        Dim c = cc.GetCompound(accession)
        If c Is Nothing OrElse c.Microspecies Is Nothing OrElse c.Microspecies.Count = 0 Then
            Console.WriteLine($"   {accession} 无微物种数据，跳过。")
            Return
        End If
        For Each pH In New Double() {5.0, 6.0, 7.0, 7.5, 8.0, 9.0}
            Dim dg = StandardFormationEnergyCalculator.StandardFormationEnergyTransformed(c, pH, cc.PMg, cc.IonicStrength, cc.Temperature)
            If dg.HasValue Then
                Console.WriteLine($"   pH {pH,4:F1}  ->  ΔfG'° = {dg.Value,12:F4} kJ/mol")
            End If
        Next
    End Sub

    ''' <summary>演示：解析反应式并用组件贡献法计算反应热力学。</summary>
    Public Sub DemonstrateReaction(cc As ComponentContribution, formula As String)
        Console.WriteLine()
        Console.WriteLine("--- 3) 反应热力学（组件贡献法） ---")
        Console.WriteLine($"   反应式: {formula}")
        Dim reaction = cc.Reaction(formula)
        Dim result = cc.StandardDgPrime(reaction)
        PrintReactionResult(result)
        Console.WriteLine($"   反应方向: {cc.GetReactionDirection(reaction)}")
    End Sub

    ''' <summary>演示：组贡献法对照计算。</summary>
    Public Sub DemonstrateGroupContribution(cc As ComponentContribution, formula As String)
        Console.WriteLine()
        Console.WriteLine("--- 4) 反应热力学（组贡献法，近似） ---")
        Dim reaction = cc.Reaction(formula)
        Dim result = cc.StandardDgPrimeGroupContribution(reaction)
        PrintReactionResult(result)
    End Sub

    Private Sub PrintReactionResult(result As GibbsEnergyResult)
        Console.WriteLine($"   ΔrG'°   (标准变换) = {result.StandardDgPrime.Value,12:F4} ± {result.Uncertainty.Value,8:F4} kJ/mol")
        Console.WriteLine($"   ΔrG'    (生理浓度) = {result.PhysiologicalDgPrime.Value,12:F4} kJ/mol")
        Console.WriteLine($"   ΔG'     (指定浓度) = {result.DgPrime.Value,12:F4} kJ/mol")
        Console.WriteLine($"   平衡常数 K'        = {result.EquilibriumConstant,12:E3}")
        Console.WriteLine($"   可行性 p 值        = {result.PValue,12:F4}")
    End Sub

    ''' <summary>演示：某一 pH 下的微物种分布与优势微物种。</summary>
    Public Sub DemonstrateMicrospecies(cache As CompoundCache, accession As String, pH As Double)
        Console.WriteLine()
        Console.WriteLine($"--- 5) 微物种分布 (pH = {pH}) ---")
        Dim c = cache.GetCompound(accession)
        If c Is Nothing OrElse c.Microspecies Is Nothing OrElse c.Microspecies.Count = 0 Then
            Console.WriteLine($"   {accession} 无微物种数据，跳过。")
            Return
        End If
        Dim calc = New MicrospeciesDistributionCalculator()
        Dim dist = calc.CalculateDistribution(c, pH)
        For Each ms In c.Microspecies.OrderByDescending(Function(m) If(dist.ContainsKey(m.Id), dist(m.Id), 0.0))
            Dim frac = If(dist.ContainsKey(ms.Id), dist(ms.Id), 0.0)
            Console.WriteLine($"   微物种 #{ms.Id}: charge={ms.Charge,3}, nH={ms.NumberProtons,3}, nMg={ms.NumberMagnesiums,2}, " &
                              $"ddg/RT={If(ms.DdgOverRt.HasValue, ms.DdgOverRt.Value.ToString("F3"), "n/a"),8} -> 摩尔分数 {frac,8:P3}")
        Next
        Dim dom = calc.GetDominantMicrospecies(c, pH)
        If dom IsNot Nothing Then
            Console.WriteLine($"   优势微物种: #{dom.Id} (charge={dom.Charge}, nH={dom.NumberProtons})")
        End If
    End Sub

    ''' <summary>演示：将多个反应式组装为化学计量矩阵。</summary>
    Public Sub DemonstrateStoichiometricMatrix(cc As ComponentContribution, formulas As String())
        Console.WriteLine()
        Console.WriteLine("--- 6) 化学计量矩阵 ---")
        Dim matrix = cc.CreateStoichiometricMatrix(formulas)
        Dim compounds = cc.GetCompoundIds(formulas)
        Console.WriteLine($"   化合物数 = {compounds.Count}, 反应数 = {formulas.Length}")
        For i = 0 To compounds.Count - 1
            Dim rowIndex = i
            Dim row = String.Join("  ", Enumerable.Range(0, formulas.Length).Select(Function(j) matrix(rowIndex, j).ToString("F0").PadLeft(3)))
            Console.WriteLine($"   {compounds(i),-14} | {row}")
        Next
        Console.WriteLine($"   （每行一个化合物，每列一个反应的化学计量系数）")
    End Sub

End Module
