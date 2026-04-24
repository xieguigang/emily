Imports eQuilibrator.EquilibratorThermodynamics

Module Example2

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
            Dim compounds As New List(Of Compound)(New EquilibratorDatabaseReader("G:\compounds_2.sqlite").ReadAllCompounds)

            ' 添加化合物 (实际应用中从数据库读取)

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

End Module
