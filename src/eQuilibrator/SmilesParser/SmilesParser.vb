Imports System.Text
Imports System.Collections.Generic

' ============================================================================
' SmilesParser.vb - SMILES 字符串解析器
' ============================================================================
' 功能：
'   1. 词法分析：将 SMILES 字符串拆解为原子、化学键、分支、环闭合标记
'   2. 分子图构建：建立原子-化学键的图结构，处理分支和环闭合
'
' 支持的 SMILES 语法：
'   - 原子：单字母 (B,C,N,O,P,S,F,I) 或双字母 (Cl,Br)
'   - 方括号原子：[Na], [nH], [CH3], [N+], [O-], [15NH2] 等
'   - 芳香原子：小写字母 c,n,o,s
'   - 化学键：- (单), = (双), # (三), : (芳香), / \ (立体，按单键处理)
'   - 分支：( )
'   - 环闭合：数字 1-9, %10-%99
'   - 断开：. (表示多个不相连的片段)
' ============================================================================

Namespace SmilesChem

    ''' <summary>
    ''' SMILES 解析器：将 SMILES 字符串解析为 SmilesMolecule 分子图。
    ''' 采用逐字符扫描方式，处理原子、化学键、分支和环闭合。
    ''' </summary>
    Public Class SmilesParser

        Private _smiles As String
        Private _pos As Integer
        Private _molecule As SmilesMolecule
        Private _ringClosures As Dictionary(Of Integer, RingClosureInfo)
        Private _prevAtomIndex As Integer
        Private _pendingBond As String
        Private _branchStack As Stack(Of Integer)

        ''' <summary>已知双字母元素符号集合（用于方括号内原子解析）</summary>
        Private Shared ReadOnly TwoLetterElements As HashSet(Of String) = New HashSet(Of String) From {
            "He", "Li", "Be", "Ne", "Na", "Mg", "Al", "Si", "Cl", "Ar",
            "Ca", "Sc", "Ti", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn",
            "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Zr", "Nb",
            "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn", "Sb",
            "Te", "Xe", "Cs", "Ba", "Hf", "Ta", "Re", "Os", "Ir", "Pt",
            "Au", "Hg", "Tl", "Pb", "Bi", "Po", "At", "Rn"
        }

        ''' <summary>环闭合临时信息：记录环编号对应的第一个原子和待定键类型</summary>
        Private Class RingClosureInfo
            Public Property AtomIndex As Integer
            Public Property PendingBond As String
        End Class

        ''' <summary>
        ''' 解析 SMILES 字符串，返回分子图。
        ''' </summary>
        ''' <param name="smiles">SMILES 字符串，如 "CCO", "c1ccccc1"</param>
        ''' <returns>解析后的 SmilesMolecule 分子图</returns>
        Public Function Parse(smiles As String) As SmilesMolecule
            _smiles = smiles
            _pos = 0
            _molecule = New SmilesMolecule() With {.Smiles = smiles}
            _ringClosures = New Dictionary(Of Integer, RingClosureInfo)()
            _prevAtomIndex = -1
            _pendingBond = ""
            _branchStack = New Stack(Of Integer)()

            While _pos < _smiles.Length
                Dim c As Char = _smiles(_pos)

                If c = "("c Then
                    ' 分支开始：保存当前原子，以便分支结束后恢复
                    _branchStack.Push(_prevAtomIndex)
                    _pos += 1

                ElseIf c = ")"c Then
                    ' 分支结束：恢复到分支前的原子
                    If _branchStack.Count > 0 Then
                        _prevAtomIndex = _branchStack.Pop()
                    End If
                    _pos += 1

                ElseIf c = "["c Then
                    ' 方括号原子：[Na], [nH], [CH3], [N+], [O-] 等
                    ParseBracketAtom()

                ElseIf c = "="c OrElse c = "#"c OrElse c = "-"c OrElse c = ":"c OrElse
                       c = "/"c OrElse c = "\"c Then
                    ' 化学键符号：记录待用键类型，等待下一个原子
                    _pendingBond = c.ToString()
                    _pos += 1

                ElseIf c = "."c Then
                    ' 断开标记：表示不相连的片段
                    _prevAtomIndex = -1
                    _pendingBond = ""
                    _pos += 1

                ElseIf c = "%"c Then
                    ' 两位数字环闭合：%12, %99 等
                    ParseRingClosure(twoDigit:=True)

                ElseIf Char.IsDigit(c) Then
                    ' 单数字环闭合：1-9
                    ParseRingClosure(twoDigit:=False)

                ElseIf Char.IsLetter(c) Then
                    ' 普通原子（可能为双字母 Cl, Br）
                    ParseSimpleAtom()

                Else
                    ' 跳过未知字符（如空格、氢原子 H 等）
                    _pos += 1
                End If
            End While

            Return _molecule
        End Function

        ''' <summary>
        ''' 解析普通原子（非方括号）。
        ''' 处理单字母原子和双字母原子（Cl, Br）。
        ''' 大写字母为脂肪族原子，小写字母为芳香原子。
        ''' </summary>
        Private Sub ParseSimpleAtom()
            Dim symbol As String
            Dim isAromatic As Boolean = Char.IsLower(_smiles(_pos))

            ' 检查双字母原子 Cl, Br（仅大写形式）
            If _pos + 1 < _smiles.Length Then
                Dim twoChar As String = _smiles.Substring(_pos, 2)
                If twoChar = "Cl" OrElse twoChar = "Br" Then
                    symbol = twoChar
                    _pos += 2
                    Dim atom As New SmilesAtom() With {
                        .Symbol = symbol,
                        .IsAromatic = False,
                        .InBracket = False
                    }
                    AddAtom(atom)
                    Return
                End If
            End If

            ' 单字母原子
            symbol = Char.ToUpper(_smiles(_pos)).ToString()
            _pos += 1

            Dim a As New SmilesAtom() With {
                .Symbol = symbol,
                .IsAromatic = isAromatic,
                .InBracket = False
            }
            AddAtom(a)
        End Sub

        ''' <summary>
        ''' 解析方括号原子 [...]。
        ''' 格式：[<同位素><元素符号><手性><H数><电荷>]
        ''' 示例：[Na], [nH], [CH3], [N+], [O-], [15NH2], [Fe+3]
        ''' </summary>
        Private Sub ParseBracketAtom()
            _pos += 1  ' 跳过 '['
            Dim start As Integer = _pos
            While _pos < _smiles.Length AndAlso _smiles(_pos) <> "]"c
                _pos += 1
            End While
            Dim content As String = _smiles.Substring(start, _pos - start)
            If _pos < _smiles.Length Then _pos += 1  ' 跳过 ']'

            Dim atom As New SmilesAtom() With {.InBracket = True}
            Dim idx As Integer = 0

            ' 1. 解析同位素（前导数字）
            Dim isoStr As String = ""
            While idx < content.Length AndAlso Char.IsDigit(content(idx))
                isoStr &= content(idx)
                idx += 1
            End While
            If isoStr.Length > 0 Then
                Integer.TryParse(isoStr, atom.Isotope)
            End If

            ' 2. 解析元素符号（1或2个字符）
            If idx < content.Length Then
                Dim firstChar As Char = content(idx)
                atom.IsAromatic = Char.IsLower(firstChar)
                atom.Symbol = Char.ToUpper(firstChar).ToString()
                idx += 1

                ' 检查是否为双字母元素（如 Na, Cl, Fe）
                If idx < content.Length AndAlso Char.IsLower(content(idx)) Then
                    Dim possible As String = atom.Symbol & content(idx)
                    If TwoLetterElements.Contains(possible) Then
                        atom.Symbol = possible
                        idx += 1
                    End If
                End If
            End If

            ' 3. 跳过手性标记 @ 或 @@
            If idx < content.Length AndAlso content(idx) = "@"c Then
                idx += 1
                If idx < content.Length AndAlso content(idx) = "@"c Then
                    idx += 1
                End If
            End If

            ' 4. 解析显式氢原子数（H 或 H2, H3 等）
            Dim hCount As Integer = 0
            If idx < content.Length AndAlso content(idx) = "H"c Then
                idx += 1
                hCount = 1
                If idx < content.Length AndAlso Char.IsDigit(content(idx)) Then
                    Integer.TryParse(content(idx).ToString(), hCount)
                    idx += 1
                End If
            End If
            atom.ExplicitHCount = hCount

            ' 5. 解析电荷（+, ++, +2, -, --, -2 等）
            Dim charge As Integer = 0
            While idx < content.Length
                Dim ch As Char = content(idx)
                If ch = "+"c Then
                    idx += 1
                    If idx < content.Length AndAlso Char.IsDigit(content(idx)) Then
                        Dim n As Integer
                        Integer.TryParse(content(idx).ToString(), n)
                        charge += n
                        idx += 1
                    ElseIf idx < content.Length AndAlso content(idx) = "+"c Then
                        charge += 1
                        idx += 1
                    Else
                        charge += 1
                    End If
                ElseIf ch = "-"c Then
                    idx += 1
                    If idx < content.Length AndAlso Char.IsDigit(content(idx)) Then
                        Dim n As Integer
                        Integer.TryParse(content(idx).ToString(), n)
                        charge -= n
                        idx += 1
                    ElseIf idx < content.Length AndAlso content(idx) = "-"c Then
                        charge -= 1
                        idx += 1
                    Else
                        charge -= 1
                    End If
                Else
                    idx += 1
                End If
            End While
            atom.Charge = charge

            AddAtom(atom)
        End Sub

        ''' <summary>
        ''' 将新原子添加到分子中，并与前一个原子建立化学键。
        ''' </summary>
        Private Sub AddAtom(atom As SmilesAtom)
            atom.Index = _molecule.Atoms.Count
            _molecule.Atoms.Add(atom)

            ' 如果有前一个原子，创建化学键
            If _prevAtomIndex >= 0 Then
                CreateBond(_prevAtomIndex, atom.Index, _pendingBond, isRingClosure:=False)
            End If

            _prevAtomIndex = atom.Index
            _pendingBond = ""
        End Sub

        ''' <summary>
        ''' 在两个原子之间创建化学键。
        ''' 自动判断芳香键（当两端均为芳香原子且未指定键类型时）。
        ''' </summary>
        Private Sub CreateBond(fromIdx As Integer, toIdx As Integer, bondType As String, isRingClosure As Boolean)
            ' 如果未指定键类型，且两端均为芳香原子，则默认为芳香键
            If String.IsNullOrEmpty(bondType) AndAlso
               _molecule.Atoms(fromIdx).IsAromatic AndAlso
               _molecule.Atoms(toIdx).IsAromatic Then
                bondType = ":"
            End If

            Dim bond As New SmilesBond() With {
                .FromAtom = fromIdx,
                .ToAtom = toIdx,
                .BondType = bondType,
                .Order = GetBondOrder(bondType),
                .IsRingClosure = isRingClosure
            }
            _molecule.Bonds.Add(bond)
            _molecule.Atoms(fromIdx).Bonds.Add(bond)
            _molecule.Atoms(toIdx).Bonds.Add(bond)
        End Sub

        ''' <summary>
        ''' 解析环闭合标记。
        ''' 单数字（1-9）或 % 后跟两位数字（10-99）。
        ''' 第一次出现时记录原子，第二次出现时创建环闭合键。
        ''' </summary>
        Private Sub ParseRingClosure(twoDigit As Boolean)
            Dim ringNum As Integer
            If twoDigit Then
                _pos += 1  ' 跳过 '%'
                If _pos + 1 < _smiles.Length Then
                    ringNum = Integer.Parse(_smiles.Substring(_pos, 2))
                    _pos += 2
                Else
                    _pos = _smiles.Length
                    Return
                End If
            Else
                ringNum = Integer.Parse(_smiles(_pos).ToString())
                _pos += 1
            End If

            If _ringClosures.ContainsKey(ringNum) Then
                ' 第二次出现：闭合环
                Dim info = _ringClosures(ringNum)
                Dim bondType As String = _pendingBond
                If String.IsNullOrEmpty(bondType) AndAlso Not String.IsNullOrEmpty(info.PendingBond) Then
                    bondType = info.PendingBond
                End If
                CreateBond(info.AtomIndex, _prevAtomIndex, bondType, isRingClosure:=True)
                _ringClosures.Remove(ringNum)
            Else
                ' 第一次出现：记录原子
                _ringClosures(ringNum) = New RingClosureInfo() With {
                    .AtomIndex = _prevAtomIndex,
                    .PendingBond = _pendingBond
                }
            End If

            _pendingBond = ""
        End Sub

        ''' <summary>
        ''' 根据键类型符号获取键级。
        ''' </summary>
        Private Function GetBondOrder(bondType As String) As Double
            Select Case bondType
                Case "=" : Return 2.0
                Case "#" : Return 3.0
                Case ":" : Return 1.5
                Case Else : Return 1.0  ' "-", "/", "\", "" 均为单键
            End Select
        End Function

    End Class

End Namespace
