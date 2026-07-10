Imports System.Text
Imports System.Collections.Generic

' ============================================================================
' SmilesCore.vb - SMILES 分子结构核心数据结构
' ============================================================================
' 本文件定义了 SMILES 解析过程中所需的所有基础数据结构：
'   - SmilesAtom        : 原子节点
'   - SmilesBond        : 化学键
'   - SmilesMolecule    : 分子图（原子集合 + 化学键集合）
'   - FunctionalGroup   : 官能团/片段识别结果
'   - GroupCategory     : 基团分类枚举
' ============================================================================

Namespace SmilesChem

    ''' <summary>
    ''' 基团分类枚举，区分"基团"和"片段"两大类。
    ''' 基团指简单的官能团（如 -OH, -CH3），
    ''' 片段指较复杂的结构单元（如苯环、环己烷、特定侧链等）。
    ''' </summary>
    Public Enum GroupCategory
        ''' <summary>简单基团：甲基、羟基、羰基等</summary>
        [Group]
        ''' <summary>结构片段：苯环、杂环、环烷烃、特定侧链等</summary>
        Fragment
    End Enum

    ''' <summary>
    ''' SMILES 原子节点。
    ''' 每个原子记录其元素符号、是否芳香、电荷、显式氢原子数等信息，
    ''' 并维护与其相连的所有化学键引用，方便图遍历。
    ''' </summary>
    Public Class SmilesAtom

        ''' <summary>原子在分子中的序号（从0开始）</summary>
        Public Property Index As Integer

        ''' <summary>元素符号，如 "C", "N", "O", "Cl", "Na" 等</summary>
        Public Property Symbol As String

        ''' <summary>是否为芳香原子（SMILES 中以小写字母表示，如 c, n, o）</summary>
        Public Property IsAromatic As Boolean

        ''' <summary>是否使用方括号表示（如 [Na], [nH], [CH3] 等）</summary>
        Public Property InBracket As Boolean

        ''' <summary>方括号内显式指定的氢原子数（如 [NH2] 的 ExplicitHCount=2）</summary>
        Public Property ExplicitHCount As Integer

        ''' <summary>原子形式电荷（如 [N+] 的 Charge=+1）</summary>
        Public Property Charge As Integer

        ''' <summary>同位素质量数（如 [15N] 的 Isotope=15）</summary>
        Public Property Isotope As Integer

        ''' <summary>与该原子相连的所有化学键</summary>
        Public Property Bonds As New List(Of SmilesBond)

        ''' <summary>
        ''' 获取该原子的标准化合价。
        ''' 用于计算隐式氢原子数。
        ''' </summary>
        Public Function GetValence() As Integer
            Select Case Symbol
                Case "C" : Return 4
                Case "N", "P" : Return 3
                Case "O", "S" : Return 2
                Case "F", "Cl", "Br", "I" : Return 1
                Case "B" : Return 3
                Case Else : Return 2
            End Select
        End Function

        ''' <summary>
        ''' 计算该原子所有化学键的键级总和。
        ''' 单键=1，双键=2，三键=3，芳香键=1.5。
        ''' </summary>
        Public Function GetTotalBondOrder() As Double
            Dim sum As Double = 0
            For Each b In Bonds
                sum += b.Order
            Next
            Return sum
        End Function

        ''' <summary>
        ''' 计算该原子的隐式氢原子数。
        ''' 对于方括号原子，返回显式指定的 H 数；
        ''' 对于普通原子，按"化合价 - 键级总和 - 电荷"计算。
        ''' </summary>
        Public Function GetImplicitHCount() As Integer
            If InBracket Then
                Return ExplicitHCount
            End If
            Dim valence As Integer = GetValence()
            Dim bondSum As Double = GetTotalBondOrder()
            Dim h As Integer = CInt(valence - bondSum - Charge)
            Return Math.Max(h, 0)
        End Function

        ''' <summary>
        ''' 获取所有相邻原子的序号列表。
        ''' </summary>
        Public Function GetNeighborIndices() As List(Of Integer)
            Dim result As New List(Of Integer)()
            For Each b In Bonds
                If b.FromAtom = Index Then
                    result.Add(b.ToAtom)
                ElseIf b.ToAtom = Index Then
                    result.Add(b.FromAtom)
                End If
            Next
            Return result
        End Function

        Public Overrides Function ToString() As String
            Dim s As String = If(IsAromatic, Symbol.ToLower(), Symbol)
            Dim h As Integer = GetImplicitHCount()
            Return String.Format("[{0}]{1}(H={2})", Index, s, h)
        End Function

    End Class

    ''' <summary>
    ''' SMILES 化学键。
    ''' 记录键的两个端点原子、键类型（单/双/三/芳香）、键级，
    ''' 以及该键是否为环闭合键。
    ''' </summary>
    Public Class SmilesBond

        ''' <summary>键的起始原子序号</summary>
        Public Property FromAtom As Integer

        ''' <summary>键的终止原子序号</summary>
        Public Property ToAtom As Integer

        ''' <summary>键类型符号："-" 单键, "=" 双键, "#" 三键, ":" 芳香键, "" 隐式单键</summary>
        Public Property BondType As String

        ''' <summary>键级：1.0=单键, 2.0=双键, 3.0=三键, 1.5=芳香键</summary>
        Public Property Order As Double

        ''' <summary>是否为环闭合键（由 SMILES 中的数字标记产生）</summary>
        Public Property IsRingClosure As Boolean

        Public Overrides Function ToString() As String
            Dim t As String = If(String.IsNullOrEmpty(BondType), "-", BondType)
            Return String.Format("{0} {1} {2}", FromAtom, t, ToAtom)
        End Function

    End Class

    ''' <summary>
    ''' 分子图：包含所有原子和化学键的完整分子结构表示。
    ''' 由 SmilesParser 解析 SMILES 字符串后构建。
    ''' </summary>
    Public Class SmilesMolecule

        ''' <summary>分子中所有原子的列表（按解析顺序排列）</summary>
        Public Property Atoms As New List(Of SmilesAtom)()

        ''' <summary>分子中所有化学键的列表</summary>
        Public Property Bonds As New List(Of SmilesBond)()

        ''' <summary>原始 SMILES 字符串</summary>
        Public Property Smiles As String

        ''' <summary>获取指定序号的原子的所有相邻原子序号</summary>
        Public Function GetNeighbors(atomIndex As Integer) As List(Of Integer)
            If atomIndex < 0 OrElse atomIndex >= Atoms.Count Then Return New List(Of Integer)()
            Return Atoms(atomIndex).GetNeighborIndices()
        End Function

        ''' <summary>获取指定原子的相邻原子中满足条件的原子序号列表</summary>
        Public Function GetNeighborsWhere(atomIndex As Integer, predicate As Func(Of SmilesAtom, Boolean)) As List(Of Integer)
            Dim result As New List(Of Integer)()
            If atomIndex < 0 OrElse atomIndex >= Atoms.Count Then Return result
            For Each b In Atoms(atomIndex).Bonds
                Dim other As Integer = If(b.FromAtom = atomIndex, b.ToAtom, b.FromAtom)
                If predicate(Atoms(other)) Then
                    result.Add(other)
                End If
            Next
            Return result
        End Function

        ''' <summary>获取两个原子之间的化学键；不存在则返回 Nothing</summary>
        Public Function GetBond(a As Integer, b As Integer) As SmilesBond
            For Each bond In Atoms(a).Bonds
                If (bond.FromAtom = a AndAlso bond.ToAtom = b) OrElse
                   (bond.FromAtom = b AndAlso bond.ToAtom = a) Then
                    Return bond
                End If
            Next
            Return Nothing
        End Function

        ''' <summary>判断两个原子之间是否存在双键</summary>
        Public Function IsDoubleBond(a As Integer, b As Integer) As Boolean
            Dim bond = GetBond(a, b)
            Return bond IsNot Nothing AndAlso bond.Order = 2.0
        End Function

        ''' <summary>判断两个原子之间是否存在三键</summary>
        Public Function IsTripleBond(a As Integer, b As Integer) As Boolean
            Dim bond = GetBond(a, b)
            Return bond IsNot Nothing AndAlso bond.Order = 3.0
        End Function

        ''' <summary>判断原子是否为碳原子</summary>
        Public Function IsCarbon(i As Integer) As Boolean
            Return i >= 0 AndAlso i < Atoms.Count AndAlso Atoms(i).Symbol = "C"
        End Function

        ''' <summary>判断原子是否为脂肪族碳（非芳香碳）</summary>
        Public Function IsAliphaticCarbon(i As Integer) As Boolean
            Return IsCarbon(i) AndAlso Not Atoms(i).IsAromatic
        End Function

        ''' <summary>判断原子是否为氮原子</summary>
        Public Function IsNitrogen(i As Integer) As Boolean
            Return i >= 0 AndAlso i < Atoms.Count AndAlso Atoms(i).Symbol = "N"
        End Function

        ''' <summary>判断原子是否为氧原子</summary>
        Public Function IsOxygen(i As Integer) As Boolean
            Return i >= 0 AndAlso i < Atoms.Count AndAlso Atoms(i).Symbol = "O"
        End Function

        ''' <summary>判断原子是否为硫原子</summary>
        Public Function IsSulfur(i As Integer) As Boolean
            Return i >= 0 AndAlso i < Atoms.Count AndAlso Atoms(i).Symbol = "S"
        End Function

        ''' <summary>判断原子是否为卤素（F, Cl, Br, I）</summary>
        Public Function IsHalogen(i As Integer) As Boolean
            If i < 0 OrElse i >= Atoms.Count Then Return False
            Dim s As String = Atoms(i).Symbol
            Return s = "F" OrElse s = "Cl" OrElse s = "Br" OrElse s = "I"
        End Function

        ''' <summary>获取指定原子的隐式氢原子数</summary>
        Public Function GetHCount(i As Integer) As Integer
            If i < 0 OrElse i >= Atoms.Count Then Return 0
            Return Atoms(i).GetImplicitHCount()
        End Function

        ''' <summary>输出分子的可读字符串表示</summary>
        Public Overrides Function ToString() As String
            Dim sb As New StringBuilder()
            sb.AppendFormat("分子: {0}  原子数: {1}  化学键数: {2}", Smiles, Atoms.Count, Bonds.Count)
            sb.AppendLine()
            sb.AppendLine("原子列表:")
            For Each a In Atoms
                sb.AppendLine("  " & a.ToString())
            Next
            sb.AppendLine("化学键列表:")
            For Each b In Bonds
                sb.AppendLine("  " & b.ToString() & If(b.IsRingClosure, "  [环闭合]", ""))
            Next
            Return sb.ToString()
        End Function

    End Class

    ''' <summary>
    ''' 官能团/片段识别结果。
    ''' 记录识别出的基团名称、简写符号、所涉及的原子序号列表，
    ''' 以及该基团属于"基团"还是"片段"。
    ''' </summary>
    Public Class FunctionalGroup

        ''' <summary>基团名称（中文+英文，如 "羟基 (Hydroxyl)"）</summary>
        Public Property Name As String

        ''' <summary>基团简写符号（如 -OH, -COOH, -CH3）</summary>
        Public Property Notation As String

        ''' <summary>分类：基团 or 片段</summary>
        Public Property Category As GroupCategory

        ''' <summary>涉及的原子序号列表</summary>
        Public Property AtomIndices As New List(Of Integer)()

        ''' <summary>匹配的模式描述（用于调试和文档）</summary>
        Public Property PatternDescription As String

        Public Overrides Function ToString() As String
            Dim cat As String = If(Category = GroupCategory.Group, "基团", "片段")
            Dim atomsStr As String = String.Join(",", AtomIndices)
            Return String.Format("[{0}] {1} ({2})  原子:[{3}]  模式:{4}",
                                 cat, Name, Notation, atomsStr, PatternDescription)
        End Function

    End Class

End Namespace
