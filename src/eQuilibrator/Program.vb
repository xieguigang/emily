' ============================================================================
' eQuilibrator VB.NET —— 测试 Demo 入口
' 演示：CSV 数据源接入、组件贡献法 / 组贡献法估算 ΔfG'°、
' 反应方向判定、微物种分布与化学计量矩阵，并包含基础自检。
' ============================================================================

Imports eQuilibrator.EquilibratorApi.Core
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorThermodynamics

Module Program

    ' 默认 CSV 目录（eQuilibrator 文件夹）。可用命令行参数或环境变量 EQuilibratorCsv 覆盖。
    Private Const DefaultCsvDir As String = "G:\emily\data\eQuilibrator"

    Private _pass As Integer = 0
    Private _fail As Integer = 0

    Sub Main(args() As String)
        Dim csvDir = ResolveCsvDir(args)
        Console.WriteLine("============================================================")
        Console.WriteLine(" eQuilibrator VB.NET —— 热力学估算 Demo")
        Console.WriteLine("============================================================")
        Console.WriteLine($" CSV 数据源 : {csvDir}")
        If Not System.IO.Directory.Exists(csvDir) Then
            Console.WriteLine(" [错误] 找不到 CSV 目录，无法运行 Demo。")
            Console.WriteLine("   可通过命令行参数或环境变量 EQuilibratorCsv 指定路径。")
            Return
        End If

        ' 1) 构建化合物缓存（懒加载 CSV）
        Dim cache = New CompoundCache(csvDir)
        Dim cc = New ComponentContribution(cache)

        ' 2) 加载化合物（按 accession）
        Examples.DemonstrateCompoundLoading(cache, "kegg:C00002")

        ' 3) 组件贡献法：pH 依赖的标准生成能
        Examples.DemonstratePhDependence(cc, "kegg:C00002")

        ' 4) 反应热力学（ATP 水解）
        Dim atpHydrolysis = "kegg:C00002 + kegg:C00001 => kegg:C00008 + kegg:C00009"
        Examples.DemonstrateReaction(cc, atpHydrolysis)

        ' 5) 组贡献法对照
        Examples.DemonstrateGroupContribution(cc, atpHydrolysis)

        ' 6) 微物种分布
        Examples.DemonstrateMicrospecies(cache, "kegg:C00002", 7.0)

        ' 7) 化学计量矩阵
        Examples.DemonstrateStoichiometricMatrix(cc, New String() {atpHydrolysis, "kegg:C00008 + kegg:C00001 => kegg:C00002"})

        ' 8) 自检
        RunSelfChecks(cache, cc, atpHydrolysis)

        Console.WriteLine()
        Console.WriteLine("============================================================")
        Console.WriteLine($" 自检结果: {_pass} PASS / {_fail} FAIL")
        Console.WriteLine("============================================================")
    End Sub

    ' ------------------------------------------------------------------
    ' 目录解析与自检辅助
    ' ------------------------------------------------------------------

    Private Function ResolveCsvDir(args() As String) As String
        If args IsNot Nothing AndAlso args.Length > 0 AndAlso System.IO.Directory.Exists(args(0)) Then
            Return args(0)
        End If
        Dim env = Environment.GetEnvironmentVariable("EQuilibratorCsv")
        If Not String.IsNullOrEmpty(env) AndAlso System.IO.Directory.Exists(env) Then
            Return env
        End If
        Return DefaultCsvDir
    End Function

    Public Sub Check(name As String, condition As Boolean)
        If condition Then
            _pass += 1
            Console.WriteLine($"   [PASS] {name}")
        Else
            _fail += 1
            Console.WriteLine($"   [FAIL] {name}")
        End If
    End Sub

    Private Sub RunSelfChecks(cache As CompoundCache, cc As ComponentContribution, formula As String)
        Console.WriteLine()
        Console.WriteLine("--- 自检 (Self-checks) ---")

        Dim atp = cache.GetCompound("kegg:C00002")
        Check("加载 ATP (kegg:C00002) 成功", atp IsNot Nothing)
        Check("ATP 基团向量非空", atp IsNot Nothing AndAlso atp.GroupVector IsNot Nothing AndAlso atp.GroupVector.Length > 0)
        Check("ATP 微物种非空", atp IsNot Nothing AndAlso atp.Microspecies IsNot Nothing AndAlso atp.Microspecies.Count > 0)

        ' pH 依赖性：组件贡献法在 pH 7 与 pH 8 的 ΔfG'° 应不同
        If atp IsNot Nothing Then
            Dim cc7 = StandardFormationEnergyCalculator.StandardFormationEnergyTransformed(atp, 7.0, cc.PMg, cc.IonicStrength, cc.Temperature)
            Dim cc8 = StandardFormationEnergyCalculator.StandardFormationEnergyTransformed(atp, 8.0, cc.PMg, cc.IonicStrength, cc.Temperature)
            Check("组件贡献法存在 pH 依赖 (pH7 ≠ pH8)", cc7.HasValue AndAlso cc8.HasValue AndAlso Math.Abs(cc7.Value - cc8.Value) > 1E-6)

            ' 组贡献法 pH 依赖（经由参考微物种的 Legendre 变换）
            Dim gc7 = StandardFormationEnergyCalculator.StandardFormationEnergyGroupContribution(atp, 7.0, cc.PMg, cc.IonicStrength, cc.Temperature)
            Dim gc8 = StandardFormationEnergyCalculator.StandardFormationEnergyGroupContribution(atp, 8.0, cc.PMg, cc.IonicStrength, cc.Temperature)
            Check("组贡献法存在 pH 依赖 (pH7 ≠ pH8)", gc7.HasValue AndAlso gc8.HasValue AndAlso Math.Abs(gc7.Value - gc8.Value) > 1E-6)
        End If

        ' 反应可解析且结果有限
        Dim res = cc.StandardDgPrime(formula)
        Check("反应 ΔrG'° 可计算且非 NaN", res IsNot Nothing AndAlso Not Double.IsNaN(res.StandardDgPrime.Value))
        Check("平衡常数 > 0", res IsNot Nothing AndAlso res.EquilibriumConstant > 0)

        Dim dir = cc.GetReactionDirection(cc.Reaction(formula))
        Check("反应方向可判定", dir = ReactionDirection.Forward OrElse dir = ReactionDirection.Reverse OrElse dir = ReactionDirection.Equilibrium)
    End Sub

End Module
