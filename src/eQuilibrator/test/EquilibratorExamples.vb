' ============================================================================
' Equilibrator 完整使用示例
' 展示如何使用该库计算化合物标准生成能
' ============================================================================

Imports System.IO
Imports eQuilibrator.Cache
Imports eQuilibrator.EquilibratorApi.Core
Imports eQuilibrator.EquilibratorThermodynamics

Namespace EquilibratorThermodynamics

    ''' <summary>
    ''' 完整使用示例
    ''' </summary>
    Public Class CompleteExample

        ''' <summary>
        ''' 主入口点
        ''' </summary>
        Public Shared Sub Main2(args As String())
            Console.WriteLine("="c, 60)
            Console.WriteLine("Equilibrator 化合物标准生成能计算示例")
            Console.WriteLine("="c, 60)
            Console.WriteLine()

            ' 示例1: 直接创建化合物对象并计算
            Example1_DirectCalculation()

            ' 示例2: 从数据库加载并计算
            ' Example2_DatabaseCalculation()

            ' 示例3: 计算化学反应Gibbs自由能
            Example3_ReactionCalculation()

            ' 示例4: pH依赖性分析
            Example4_PHDependence()

            ' 示例5: 微物种分布分析
            Example5_MicrospeciesDistribution()

            Console.WriteLine()
            Console.WriteLine("按任意键退出...")
            Console.ReadKey()
        End Sub

        ''' <summary>
        ''' 示例1: 直接创建化合物对象并计算标准生成能
        ''' </summary>
        Public Shared Sub Example1_DirectCalculation()
            Console.WriteLine("-"c, 60)
            Console.WriteLine("示例1: 直接创建化合物对象并计算")
            Console.WriteLine("-"c, 60)

            ' 创建计算器
            Dim calculator As New StandardFormationEnergyCalculator()

            ' 创建葡萄糖化合物
            Dim glucose As New Compound(atomBag:=New Dictionary(Of String, Integer) From {
                    {"C", 6},
                    {"H", 12},
                    {"O", 6}
                },
                groupVector:=New Double() {
                    0, 0, 0, 0, 0,  ' 基础基团
                    5,  ' 5个-OH基团
                    0, 0, 0, 0, 0,  ' 其他基团
                    1,  ' 1个环状-O-
                    0, 0, 0, 0, 0,  ' 更多基团
                    1   ' 六元环
                }) With {
                .Id = 1,
                .InChIKey = "WQZGKKKJIJFFOK-GASJEMHNSA-N",
                .Smiles = "OC[C@H]1OC(O)[C@H](O)[C@@H](O)[C@@H]1O",
                .MolecularWeight = 180.156,
                .Microspecies = New List(Of CompoundMicrospecies)()
            }

            ' 添加微物种
            glucose.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1,
                .CompoundId = 1,
                .Charge = 0,
                .NumberProtons = 12,
                .NumberMagnesiums = 0,
                .IsMajor = True,
                .DdgOverRt = 0.0
            })

            Try
                ' 计算标准生成能
                Dim deltaG As Double = calculator.CalculateStandardFormationEnergy(glucose)
                Console.WriteLine($"葡萄糖的标准生成能 ΔfG° = {deltaG:F2} kJ/mol")

                ' 计算pH 7.0下的生成能
                Dim deltaG7 As Double = calculator.CalculateFormationEnergyAtPH(glucose, 7.0)
                Console.WriteLine($"葡萄糖在pH 7.0的生成能 ΔfG'° = {deltaG7:F2} kJ/mol")

            Catch ex As Exception
                Console.WriteLine($"计算错误: {ex.Message}")
            End Try

            Console.WriteLine()
        End Sub

        '''' <summary>
        '''' 示例2: 从数据库加载化合物并计算
        '''' </summary>
        'Public Shared Sub Example2_DatabaseCalculation()
        '    Console.WriteLine("-"c, 60)
        '    Console.WriteLine("示例2: 从数据库加载化合物并计算")
        '    Console.WriteLine("-"c, 60)

        '    Dim dbPath As String = "G:\compounds_2.sqlite"

        '    If Not File.Exists(dbPath) Then
        '        Console.WriteLine($"数据库文件不存在: {dbPath}")
        '        Console.WriteLine("请将dbPath变量设置为实际的数据库路径")
        '        Console.WriteLine()
        '        Return
        '    End If

        '    Try
        '        ' 创建数据库读取器
        '        Using reader As New EquilibratorDatabaseReader(dbPath)
        '            ' 读取所有化合物
        '            Dim compounds = reader.ReadAllCompounds()
        '            Console.WriteLine($"从数据库读取了 {compounds.Count} 个化合物")

        '            ' 创建计算器并加载数据
        '            Dim calculator As New StandardFormationEnergyCalculator()
        '            calculator.LoadCompounds(compounds)

        '            ' 计算前10个化合物的标准生成能
        '            Console.WriteLine()
        '            Console.WriteLine("前10个化合物的标准生成能:")
        '            For Each compound In compounds.Take(10)
        '                Try
        '                    Dim deltaG As Double = calculator.CalculateStandardFormationEnergy(compound)
        '                    Console.WriteLine($"  {compound.InChIKey}: ΔfG° = {deltaG:F2} kJ/mol")
        '                Catch ex As Exception
        '                    Console.WriteLine($"  {compound.InChIKey}: 计算失败 - {ex.Message}")
        '                End Try
        '            Next

        '            ' 通过InChI Key查找特定化合物
        '            Console.WriteLine()
        '            Dim phosphate = calculator.GetCompoundByInChIKey("NBIIXXVUZAFLBC-UHFFFAOYSA-L")
        '            If phosphate IsNot Nothing Then
        '                Dim deltaG As Double = calculator.CalculateStandardFormationEnergy(phosphate)
        '                Console.WriteLine($"磷酸根的标准生成能: ΔfG° = {deltaG:F2} kJ/mol")
        '            End If
        '        End Using

        '    Catch ex As Exception
        '        Console.WriteLine($"数据库读取错误: {ex.Message}")
        '    End Try

        '    Console.WriteLine()
        'End Sub

        ''' <summary>
        ''' 示例3: 计算化学反应的Gibbs自由能变化
        ''' </summary>
        Public Shared Sub Example3_ReactionCalculation()
            Console.WriteLine("-"c, 60)
            Console.WriteLine("示例3: 计算化学反应的Gibbs自由能变化")
            Console.WriteLine("-"c, 60)

            Dim calculator As New StandardFormationEnergyCalculator()

            ' 创建反应物和产物
            ' 反应: 葡萄糖 + ATP -> 葡萄糖-6-磷酸 + ADP

            ' 葡萄糖
            Dim glucose As New Compound(atomBag:=New Dictionary(Of String, Integer) From {{"C", 6}, {"H", 12}, {"O", 6}},
                groupVector:=New Double() {0, 0, 0, 0, 5, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1}) With {
                .Id = 1,
                .Smiles = "Glucose",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            glucose.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1, .CompoundId = 1, .Charge = 0, .NumberProtons = 12, .IsMajor = True, .DdgOverRt = 0.0
            })

            ' ATP
            Dim atp As New Compound(atomBag:=New Dictionary(Of String, Integer) From {{"C", 10}, {"H", 16}, {"N", 5}, {"O", 13}, {"P", 3}},
                groupVector:=New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0, 0, 0, 0}) With {
                .Id = 2,
                .Smiles = "ATP",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            atp.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 2, .CompoundId = 2, .Charge = -4, .NumberProtons = 12, .IsMajor = True, .DdgOverRt = 0.0
            })

            ' 葡萄糖-6-磷酸
            Dim g6p As New Compound(atomBag:=New Dictionary(Of String, Integer) From {{"C", 6}, {"H", 13}, {"O", 9}, {"P", 1}},
                groupVector:=New Double() {0, 0, 0, 0, 4, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1}) With {
                .Id = 3,
                .Smiles = "G6P",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            g6p.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 3, .CompoundId = 3, .Charge = -2, .NumberProtons = 11, .IsMajor = True, .DdgOverRt = 0.0
            })

            ' ADP
            Dim adp As New Compound(atomBag:=New Dictionary(Of String, Integer) From {{"C", 10}, {"H", 15}, {"N", 5}, {"O", 10}, {"P", 2}},
                groupVector:=New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2, 0, 0, 0, 0, 0}) With {
                .Id = 4,
                .Smiles = "ADP",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }
            adp.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 4, .CompoundId = 4, .Charge = -3, .NumberProtons = 12, .IsMajor = True, .DdgOverRt = 0.0
            })

            ' 定义反应
            Dim reactants As New Dictionary(Of Compound, Double) From {
                {glucose, 1.0},
                {atp, 1.0}
            }

            Dim products As New Dictionary(Of Compound, Double) From {
                {g6p, 1.0},
                {adp, 1.0}
            }

            Try
                ' 计算反应Gibbs自由能变化
                Dim deltaG As Double = calculator.CalculateReactionGibbsEnergy(reactants, products)
                Console.WriteLine($"葡萄糖磷酸化反应:")
                Console.WriteLine($"  葡萄糖 + ATP -> 葡萄糖-6-磷酸 + ADP")
                Console.WriteLine($"  ΔrG° = {deltaG:F2} kJ/mol")

                ' 计算平衡常数
                Dim keq As Double = calculator.CalculateEquilibriumConstant(deltaG)
                Console.WriteLine($"  平衡常数 K = {keq:E4}")

                ' 计算pH 7.0下的值
                Dim deltaG7 As Double = calculator.CalculateReactionGibbsEnergyAtPH(reactants, products, 7.0)
                Console.WriteLine($"  pH 7.0下: ΔrG'° = {deltaG7:F2} kJ/mol")

            Catch ex As Exception
                Console.WriteLine($"计算错误: {ex.Message}")
            End Try

            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' 示例4: pH依赖性分析
        ''' </summary>
        Public Shared Sub Example4_PHDependence()
            Console.WriteLine("-"c, 60)
            Console.WriteLine("示例4: pH依赖性分析")
            Console.WriteLine("-"c, 60)

            Dim calculator As New StandardFormationEnergyCalculator()

            ' 创建磷酸根化合物
            Dim phosphate As New Compound(atomBag:=New Dictionary(Of String, Integer) From {{"O", 4}, {"P", 1}},
                groupVector:=New Double() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0}) With {
                .Id = 1,
                .Smiles = "OP([O-])([O-])=O",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }

            ' 添加微物种 (H3PO4, H2PO4-, HPO4(2-), PO4(3-))
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1, .CompoundId = 1, .Charge = 0, .NumberProtons = 3, .IsMajor = False, .DdgOverRt = -4.6
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 2, .CompoundId = 1, .Charge = -1, .NumberProtons = 2, .IsMajor = False, .DdgOverRt = -2.2
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 3, .CompoundId = 1, .Charge = -2, .NumberProtons = 1, .IsMajor = True, .DdgOverRt = 0.0
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 4, .CompoundId = 1, .Charge = -3, .NumberProtons = 0, .IsMajor = False, .DdgOverRt = 6.2
            })

            Console.WriteLine("磷酸根在不同pH下的标准生成能:")
            Console.WriteLine("pH`t`tΔfG'° (kJ/mol)")
            Console.WriteLine("-"c, 30)

            For pH As Double = 0 To 14 Step 1
                Try
                    Dim deltaG As Double = calculator.CalculateFormationEnergyAtPH(phosphate, pH)
                    Console.WriteLine($"{pH:F0}`t`t{deltaG:F2}")
                Catch ex As Exception
                    Console.WriteLine($"{pH:F0}`t`t计算失败")
                End Try
            Next

            Console.WriteLine()
        End Sub

        ''' <summary>
        ''' 示例5: 微物种分布分析
        ''' </summary>
        Public Shared Sub Example5_MicrospeciesDistribution()
            Console.WriteLine("-"c, 60)
            Console.WriteLine("示例5: 微物种分布分析")
            Console.WriteLine("-"c, 60)

            ' 创建磷酸根化合物
            Dim phosphate As New Compound With {
                .Id = 1,
                .Smiles = "OP([O-])([O-])=O",
                .Microspecies = New List(Of CompoundMicrospecies)()
            }

            ' 添加微物种
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 1, .CompoundId = 1, .Charge = 0, .NumberProtons = 3, .IsMajor = False, .DdgOverRt = -4.6
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 2, .CompoundId = 1, .Charge = -1, .NumberProtons = 2, .IsMajor = False, .DdgOverRt = -2.2
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 3, .CompoundId = 1, .Charge = -2, .NumberProtons = 1, .IsMajor = True, .DdgOverRt = 0.0
            })
            phosphate.Microspecies.Add(New CompoundMicrospecies With {
                .Id = 4, .CompoundId = 1, .Charge = -3, .NumberProtons = 0, .IsMajor = False, .DdgOverRt = 6.2
            })

            Dim distCalc As New MicrospeciesDistributionCalculator()

            Console.WriteLine("磷酸根在不同pH下的微物种分布:")
            Console.WriteLine()

            Dim pHValues As Double() = {0, 2, 4, 6, 7, 8, 10, 12, 14}

            For Each pH In pHValues
                Console.WriteLine($"pH = {pH}:")

                Dim distribution = distCalc.CalculateDistribution(phosphate, pH)
                Dim dominant = distCalc.GetDominantMicrospecies(phosphate, pH)
                Dim avgCharge = distCalc.CalculateAverageCharge(phosphate, pH)

                For Each kvp In distribution
                    Dim ms = phosphate.Microspecies.FirstOrDefault(Function(m) m.Id = kvp.Key)
                    If ms IsNot Nothing AndAlso kvp.Value > 0.01 Then ' 只显示大于1%的物种
                        Console.WriteLine($"  H{ms.NumberProtons}PO4({If(ms.Charge >= 0, "+", "")}{ms.Charge}): {kvp.Value * 100:F1}%")
                    End If
                Next

                If dominant IsNot Nothing Then
                    Console.WriteLine($"  主要物种: H{dominant.NumberProtons}PO4({If(dominant.Charge >= 0, "+", "")}{dominant.Charge})")
                End If
                Console.WriteLine($"  平均电荷: {avgCharge:F2}")
                Console.WriteLine()
            Next
        End Sub

    End Class

    ' ========================================================================
    ' 批量计算工具
    ' ========================================================================

    ''' <summary>
    ''' 批量计算工具
    ''' 用于批量计算多个化合物的标准生成能
    ''' </summary>
    Public Class BatchCalculator

        Private _calculator As StandardFormationEnergyCalculator

        Public Sub New()
            _calculator = New StandardFormationEnergyCalculator()
        End Sub

        Public Sub New(compounds As List(Of Compound))
            _calculator = New StandardFormationEnergyCalculator()
            _calculator.LoadCompounds(compounds)
        End Sub

        ''' <summary>
        ''' 批量计算标准生成能
        ''' </summary>
        Public Function CalculateAllFormationEnergies(compounds As List(Of Compound)) As Dictionary(Of Integer, Double)
            Dim results As New Dictionary(Of Integer, Double)()

            For Each compound In compounds
                Try
                    Dim deltaG As Double = _calculator.CalculateStandardFormationEnergy(compound)
                    results(compound.Id) = deltaG
                Catch ex As Exception
                    ' 记录错误但不中断
                    results(compound.Id) = Double.NaN
                End Try
            Next

            Return results
        End Function

        ''' <summary>
        ''' 批量计算指定pH下的生成能
        ''' </summary>
        Public Function CalculateAllFormationEnergiesAtPH(compounds As List(Of Compound), pH As Double) As Dictionary(Of Integer, Double)
            Dim results As New Dictionary(Of Integer, Double)()

            For Each compound In compounds
                Try
                    Dim deltaG As Double = _calculator.CalculateFormationEnergyAtPH(compound, pH)
                    results(compound.Id) = deltaG
                Catch ex As Exception
                    results(compound.Id) = Double.NaN
                End Try
            Next

            Return results
        End Function

        ''' <summary>
        ''' 导出结果到CSV文件
        ''' </summary>
        Public Sub ExportToCSV(results As Dictionary(Of Integer, Double), compounds As List(Of Compound), filePath As String)
            Using writer As New StreamWriter(filePath)
                ' 写入表头
                writer.WriteLine("CompoundID,InChIKey,SMILES,Mass,DeltaG_kJ_mol")

                ' 写入数据
                For Each compound In compounds
                    If results.ContainsKey(compound.Id) Then
                        Dim deltaG As Double = results(compound.Id)
                        Dim deltaGStr As String = If(Double.IsNaN(deltaG), "NA", deltaG.ToString("F2"))
                        writer.WriteLine($"{compound.Id},{compound.InChIKey},{compound.Smiles},{compound.MolecularWeight:F3},{deltaGStr}")
                    End If
                Next
            End Using
        End Sub

        ''' <summary>
        ''' 计算反应数据库中所有反应的Gibbs自由能变化
        ''' </summary>
        Public Function CalculateAllReactionEnergies(reactions As List(Of ReactionInfo)) As Dictionary(Of Integer, Double)
            Dim results As New Dictionary(Of Integer, Double)()

            For Each reaction In reactions
                Try
                    Dim deltaG As Double = _calculator.CalculateReactionGibbsEnergy(reaction.Reactants, reaction.Products)
                    results(reaction.Id) = deltaG
                Catch ex As Exception
                    results(reaction.Id) = Double.NaN
                End Try
            Next

            Return results
        End Function

    End Class

    ''' <summary>
    ''' 反应信息类
    ''' </summary>
    Public Class ReactionInfo
        Public Property Id As Integer
        Public Property Name As String
        Public Property Reactants As Dictionary(Of Compound, Double)
        Public Property Products As Dictionary(Of Compound, Double)
    End Class

    ' ========================================================================
    ' 热力学分析工具
    ' ========================================================================

    ''' <summary>
    ''' 热力学分析工具
    ''' 提供更高级的分析功能
    ''' </summary>
    Public Class ThermodynamicAnalyzer

        Private _calculator As StandardFormationEnergyCalculator

        Public Sub New()
            _calculator = New StandardFormationEnergyCalculator()
        End Sub

        ''' <summary>
        ''' 分析代谢途径的热力学可行性
        ''' </summary>
        Public Function AnalyzePathway(reactions As List(Of ReactionInfo)) As PathwayAnalysisResult
            Dim result As New PathwayAnalysisResult()

            Dim totalDeltaG As Double = 0.0
            Dim reactionEnergies As New List(Of Double)()

            For Each reaction In reactions
                Try
                    Dim deltaG As Double = _calculator.CalculateReactionGibbsEnergy(reaction.Reactants, reaction.Products)
                    reactionEnergies.Add(deltaG)
                    totalDeltaG += deltaG

                    result.ReactionResults(reaction.Id) = New ReactionThermodynamics With {
                        .DeltaG = deltaG,
                        .IsFavorable = deltaG < 0,
                        .EquilibriumConstant = _calculator.CalculateEquilibriumConstant(deltaG)
                    }
                Catch ex As Exception
                    reactionEnergies.Add(Double.NaN)
                    result.Errors.Add($"反应 {reaction.Id}: {ex.Message}")
                End Try
            Next

            result.TotalDeltaG = totalDeltaG
            result.IsPathwayFeasible = totalDeltaG < 0
            result.AverageDeltaG = reactionEnergies.Where(Function(d) Not Double.IsNaN(d)).Average()

            Return result
        End Function

        ''' <summary>
        ''' 计算化合物的氧化还原电位
        ''' </summary>
        Public Function CalculateRedoxPotential(
            reducedCompound As Compound,
            oxidizedCompound As Compound,
            electronsTransferred As Integer) As Double

            ' Nernst方程: E°' = -ΔG°' / (n * F)
            ' 其中F是法拉第常数

            Dim reactants As New Dictionary(Of Compound, Double) From {
                {reducedCompound, 1.0}
            }

            Dim products As New Dictionary(Of Compound, Double) From {
                {oxidizedCompound, 1.0}
            }

            Dim deltaG As Double = _calculator.CalculateReactionGibbsEnergy(reactants, products)

            ' 转换为电位 (V)
            ' E = -ΔG / (n * F)
            ' ΔG单位是kJ/mol，需要转换为J/mol
            Dim potential As Double = -deltaG * 1000 / (electronsTransferred * StandardFormationEnergyCalculator.FARADAY_CONSTANT)

            Return potential
        End Function

        ''' <summary>
        ''' 分析化合物在不同条件下的稳定性
        ''' </summary>
        Public Function AnalyzeStability(compound As Compound) As StabilityAnalysisResult
            Dim result As New StabilityAnalysisResult With {
                .CompoundId = compound.Id,
                .CompoundName = compound.InChIKey
            }

            ' 分析pH稳定性
            Dim pHRange As Double() = {0, 2, 4, 6, 7, 8, 10, 12, 14}
            For Each pH In pHRange
                Try
                    Dim deltaG As Double = _calculator.CalculateFormationEnergyAtPH(compound, pH)
                    result.pHStability(pH) = deltaG
                Catch ex As Exception
                    result.pHStability(pH) = Double.NaN
                End Try
            Next

            ' 分析电荷状态
            If compound.Microspecies IsNot Nothing Then
                Dim distCalc As New MicrospeciesDistributionCalculator()
                For Each pH In {5, 7, 9}
                    Dim avgCharge = distCalc.CalculateAverageCharge(compound, pH)
                    result.ChargeAtPH(pH) = avgCharge
                Next
            End If

            Return result
        End Function

    End Class

    ''' <summary>
    ''' 途径分析结果
    ''' </summary>
    Public Class PathwayAnalysisResult
        Public Property TotalDeltaG As Double
        Public Property AverageDeltaG As Double
        Public Property IsPathwayFeasible As Boolean
        Public Property ReactionResults As New Dictionary(Of Integer, ReactionThermodynamics)()
        Public Property Errors As New List(Of String)()
    End Class

    ''' <summary>
    ''' 反应热力学信息
    ''' </summary>
    Public Class ReactionThermodynamics
        Public Property DeltaG As Double
        Public Property IsFavorable As Boolean
        Public Property EquilibriumConstant As Double
    End Class

    ''' <summary>
    ''' 稳定性分析结果
    ''' </summary>
    Public Class StabilityAnalysisResult
        Public Property CompoundId As Integer
        Public Property CompoundName As String
        Public Property pHStability As New Dictionary(Of Double, Double)()
        Public Property ChargeAtPH As New Dictionary(Of Double, Double)()
    End Class

End Namespace
