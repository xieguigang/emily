Imports System.Text
Imports System.Collections.Generic
Imports eQuilibrator.SmilesChem

Public Class SmilesTest

    Public Shared Sub ParserTest()
        Console.OutputEncoding = Encoding.UTF8
        Console.WriteLine(New String("="c, 70))
        Console.WriteLine("  SMILES 分子结构解析器 - 官能团拆解演示")
        Console.WriteLine(New String("="c, 70))
        Console.WriteLine()

        ' 定义测试用例：分子名称 + SMILES 字符串
        Dim testCases As Tuple(Of String, String)() = {
            Tuple.Create("乙醇 (Ethanol)", "CCO"),
            Tuple.Create("乙酸 (Acetic Acid)", "CC(=O)O"),
            Tuple.Create("乙酸乙酯 (Ethyl Acetate)", "CC(=O)OCC"),
            Tuple.Create("苯 (Benzene)", "c1ccccc1"),
            Tuple.Create("苯酚 (Phenol)", "c1ccccc1O"),
            Tuple.Create("甲苯 (Toluene)", "Cc1ccccc1"),
            Tuple.Create("苯胺 (Aniline)", "c1ccccc1N"),
            Tuple.Create("乙酰胺 (Acetamide)", "CC(=O)N"),
            Tuple.Create("丙酮 (Acetone)", "CC(=O)C"),
            Tuple.Create("乙醛 (Acetaldehyde)", "CC=O"),
            Tuple.Create("甲醛 (Formaldehyde)", "C=O"),
            Tuple.Create("乙腈 (Acetonitrile)", "CC#N"),
            Tuple.Create("乙炔 (Acetylene)", "C#C"),
            Tuple.Create("乙烯 (Ethylene)", "C=C"),
            Tuple.Create("硝基甲烷 (Nitromethane)", "C[N+](=O)[O-]"),
            Tuple.Create("甲胺 (Methylamine)", "CN"),
            Tuple.Create("二甲醚 (Dimethyl Ether)", "COC"),
            Tuple.Create("氯乙烷 (Chloroethane)", "CCCl"),
            Tuple.Create("溴苯 (Bromobenzene)", "c1ccccc1Br"),
            Tuple.Create("吡啶 (Pyridine)", "c1ccncc1"),
            Tuple.Create("呋喃 (Furan)", "c1ccoc1"),
            Tuple.Create("噻吩 (Thiophene)", "c1ccsc1"),
            Tuple.Create("环己烷 (Cyclohexane)", "C1CCCCC1"),
            Tuple.Create("四氢呋喃 (THF)", "C1CCOC1"),
            Tuple.Create("阿司匹林 (Aspirin)", "CC(=O)Oc1ccccc1C(=O)O"),
            Tuple.Create("甘氨酸 (Glycine)", "NCC(=O)O"),
            Tuple.Create("丙氨酸 (Alanine)", "CC(N)C(=O)O"),
            Tuple.Create("偶氮苯 (Azobenzene)", "c1ccccc1N=Nc1ccccc1"),
            Tuple.Create("二甲亚砜 (DMSO)", "CS(=O)C"),
            Tuple.Create("甲磺酸 (Methanesulfonic Acid)", "CS(=O)(=O)O")
        }

        Dim parser As New SmilesParser()
        Dim detector As New FunctionalGroupDetector()

        For Each tc In testCases
            Console.WriteLine(New String("-"c, 70))
            Console.WriteLine("分子: " & tc.Item1)
            Console.WriteLine("SMILES: " & tc.Item2)
            Console.WriteLine()

            ' 第一步：解析 SMILES
            Dim mol As SmilesMolecule = parser.Parse(tc.Item2)

            ' 输出原子信息
            Console.WriteLine("  [分子图]")
            Console.Write("    原子: ")
            For i As Integer = 0 To mol.Atoms.Count - 1
                Dim a = mol.Atoms(i)
                Dim sym As String = If(a.IsAromatic, a.Symbol.ToLower(), a.Symbol)
                Dim extra As String = If(a.InBracket, "(H" & a.ExplicitHCount & ")", "")
                Console.Write("[{0}]{1}{2}", i, sym, extra)
                If i < mol.Atoms.Count - 1 Then Console.Write(" - ")
            Next
            Console.WriteLine()
            Console.WriteLine("    化学键数: " & mol.Bonds.Count)

            ' 第二步：识别官能团
            Dim groups = detector.Detect(mol)

            Console.WriteLine()
            Console.WriteLine("  [拆解结果] 共识别 " & groups.Count & " 个基团/片段:")
            For Each g In groups
                Console.WriteLine("    " & g.ToString())
            Next
            Console.WriteLine()
        Next

        ' 汇总统计
        Console.WriteLine(New String("="c, 70))
        Console.WriteLine("  测试完成: 共测试 " & testCases.Length & " 种分子")
        Console.WriteLine(New String("="c, 70))
    End Sub

End Class


