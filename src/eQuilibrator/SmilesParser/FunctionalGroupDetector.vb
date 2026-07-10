Imports System.Text
Imports System.Collections.Generic

' ============================================================================
' FunctionalGroupDetector.vb - 官能团与片段识别器
' ============================================================================
' 功能：
'   对已解析的 SmilesMolecule 分子图进行"拆解"，识别出一系列预先定义的
'   "基团"（如 -CH3, -OH, -COO-, =CH-）和"片段"（如苯环、吡啶、环己烷）。
'
' 识别策略：
'   1. 先进行环检测（DFS 找所有最小环），标记成环原子
'   2. 先识别环状片段（苯环、杂环、环烷烃），避免环内原子被简单基团抢先
'   3. 再按优先级依次匹配各官能团模式（高优先级先匹配，避免重复识别）
'   4. 每个基团记录其涉及的原子序号，已识别的原子在后续匹配中跳过
'
' 支持的基团/片段：
'   片段：苯环、吡啶、呋喃、噻吩、吡咯、咪唑、环己烷、环戊烷、环醚、环胺
'   基团：羧基、酯基、酰胺、醛基、酮基、羰基、磺酸基、砜基、亚砜基、
'         硝基、氰基、偶氮基、亚胺基、羟基、醚键、过氧键、伯/仲/叔胺、
'         巯基、硫醚键、炔键、烯键、亚甲基、次甲基、甲基、卤素
' ============================================================================

Namespace SmilesChem

    ''' <summary>
    ''' 官能团与片段识别器。
    ''' 对分子图进行模式匹配，将分子"拆解"为预定义的基团和片段。
    ''' </summary>
    Public Class FunctionalGroupDetector

        Private _mol As SmilesMolecule
        Private _ringAtoms As HashSet(Of Integer)
        Private _allRings As List(Of List(Of Integer))
        Private _usedAtoms As HashSet(Of Integer)

        ''' <summary>
        ''' 对分子进行完整拆解，返回所有识别到的基团和片段。
        ''' </summary>
        ''' <param name="mol">已解析的分子图</param>
        ''' <returns>识别到的官能团和片段列表</returns>
        Public Function Detect(mol As SmilesMolecule) As List(Of FunctionalGroup)
            _mol = mol
            _usedAtoms = New HashSet(Of Integer)()
            _ringAtoms = New HashSet(Of Integer)()
            _allRings = FindAllRings()

            ' 标记所有成环原子
            For Each ring In _allRings
                For Each a In ring
                    _ringAtoms.Add(a)
                Next
            Next

            Dim groups As New List(Of FunctionalGroup)()

            ' ===== 第一步：先识别环状片段（避免环内原子被简单基团抢先） =====
            DetectAromaticRings(groups)
            DetectCycloalkanes(groups)
            DetectHeterocycles(groups)

            ' ===== 第二步：识别复合官能团（按优先级从高到低） =====
            DetectCarboxylicAcid(groups)
            DetectEster(groups)
            DetectAmide(groups)
            DetectAldehyde(groups)
            DetectKetone(groups)
            DetectCarbonyl(groups)
            DetectSulfonicAcid(groups)
            DetectSulfonyl(groups)
            DetectSulfoxide(groups)
            DetectNitro(groups)
            DetectNitrile(groups)
            DetectAzo(groups)
            DetectImine(groups)

            ' ===== 第三步：识别简单官能团 =====
            DetectHydroxyl(groups)
            DetectEther(groups)
            DetectPeroxide(groups)
            DetectAmine(groups)
            DetectThiol(groups)
            DetectThioether(groups)

            ' ===== 第四步：识别不饱和键和烷基 =====
            DetectAlkyne(groups)
            DetectAlkene(groups)
            DetectMethylene(groups)
            DetectMethine(groups)
            DetectMethyl(groups)
            DetectHalides(groups)

            Return groups
        End Function

        ' ====================================================================
        ' 环检测：使用 DFS 查找所有最小环
        ' ====================================================================

        ''' <summary>
        ''' 查找分子中所有的环（最小环集合）。
        ''' 使用 DFS 从每个原子出发搜索回到自身的路径，限制最大环大小为8。
        ''' </summary>
        Private Function FindAllRings() As List(Of List(Of Integer))
            Dim rings As New List(Of List(Of Integer))()
            Dim seen As New HashSet(Of String)()

            For i As Integer = 0 To _mol.Atoms.Count - 1
                Dim path As New List(Of Integer) From {i}
                Dim visited As New HashSet(Of Integer) From {i}
                FindRingsDFS(i, i, path, visited, rings, seen, 0, 8)
            Next

            Return rings
        End Function

        ''' <summary>DFS 递归搜索环</summary>
        Private Sub FindRingsDFS(start As Integer, current As Integer,
                                 path As List(Of Integer), visited As HashSet(Of Integer),
                                 rings As List(Of List(Of Integer)), seen As HashSet(Of String),
                                 depth As Integer, maxDepth As Integer)
            If depth > maxDepth Then Return

            For Each neighbor In _mol.GetNeighbors(current)
                If neighbor = start AndAlso path.Count >= 3 Then
                    ' 找到环
                    Dim ring As New List(Of Integer)(path)
                    Dim key As String = CanonicalRingKey(ring)
                    If Not seen.Contains(key) Then
                        seen.Add(key)
                        rings.Add(ring)
                    End If
                ElseIf Not visited.Contains(neighbor) AndAlso neighbor > start Then
                    ' 只探索序号大于起点的原子，避免重复
                    visited.Add(neighbor)
                    path.Add(neighbor)
                    FindRingsDFS(start, neighbor, path, visited, rings, seen, depth + 1, maxDepth)
                    path.RemoveAt(path.Count - 1)
                    visited.Remove(neighbor)
                End If
            Next
        End Sub

        ''' <summary>生成环的规范化键，用于去重（旋转和翻转无关）</summary>
        Private Function CanonicalRingKey(ring As List(Of Integer)) As String
            Dim minVal As Integer = Integer.MaxValue
            Dim minIdx As Integer = 0
            For i As Integer = 0 To ring.Count - 1
                If ring(i) < minVal Then
                    minVal = ring(i)
                    minIdx = i
                End If
            Next
            Dim sb As New StringBuilder()
            For i As Integer = 0 To ring.Count - 1
                sb.Append(ring((minIdx + i) Mod ring.Count))
                sb.Append(","c)
            Next
            Return sb.ToString()
        End Function

        ' ====================================================================
        ' 辅助方法
        ' ====================================================================

        ''' <summary>获取指定原子的第一个双键邻居原子序号（不存在返回-1）</summary>
        Private Function GetDoubleBondedNeighbor(atomIdx As Integer) As Integer
            For Each b In _mol.Atoms(atomIdx).Bonds
                If b.Order = 2.0 Then
                    Return If(b.FromAtom = atomIdx, b.ToAtom, b.FromAtom)
                End If
            Next
            Return -1
        End Function

        ''' <summary>获取指定原子的第一个三键邻居原子序号（不存在返回-1）</summary>
        Private Function GetTripleBondedNeighbor(atomIdx As Integer) As Integer
            For Each b In _mol.Atoms(atomIdx).Bonds
                If b.Order = 3.0 Then
                    Return If(b.FromAtom = atomIdx, b.ToAtom, b.FromAtom)
                End If
            Next
            Return -1
        End Function

        ''' <summary>判断原子是否未被使用（未被已识别的基团占用）</summary>
        Private Function IsAvailable(idx As Integer) As Boolean
            Return Not _usedAtoms.Contains(idx)
        End Function

        ''' <summary>添加一个识别到的官能团，并标记其原子为已使用</summary>
        Private Sub AddGroup(groups As List(Of FunctionalGroup), name As String, notation As String,
                             category As GroupCategory, pattern As String, atoms As IEnumerable(Of Integer))
            Dim g As New FunctionalGroup() With {
                .Name = name,
                .Notation = notation,
                .Category = category,
                .PatternDescription = pattern
            }
            g.AtomIndices.AddRange(atoms)
            groups.Add(g)
            For Each a In atoms
                _usedAtoms.Add(a)
            Next
        End Sub

        ' ====================================================================
        ' 环状片段识别
        ' ====================================================================

        ''' <summary>
        ''' 识别芳香环（苯环、吡啶等）。
        ''' 条件：环大小为5或6，所有原子均为芳香原子。
        ''' </summary>
        Private Sub DetectAromaticRings(groups As List(Of FunctionalGroup))
            For Each ring In _allRings
                If ring.Count < 5 OrElse ring.Count > 6 Then Continue For
                If Not IsAvailable(ring(0)) Then Continue For

                Dim allAromatic As Boolean = True
                For Each a In ring
                    If Not _mol.Atoms(a).IsAromatic Then
                        allAromatic = False
                        Exit For
                    End If
                Next
                If Not allAromatic Then Continue For

                ' 统计杂原子
                Dim heteroAtoms As New List(Of String)()
                For Each a In ring
                    Dim sym As String = _mol.Atoms(a).Symbol
                    If sym <> "C" Then heteroAtoms.Add(sym)
                Next

                Dim name As String, notation As String, pattern As String
                If heteroAtoms.Count = 0 Then
                    name = "苯环 (Benzene/Phenyl)"
                    notation = "-C6H5"
                    pattern = "c1ccccc1"
                ElseIf heteroAtoms.Count = 1 AndAlso heteroAtoms(0) = "N" Then
                    name = "吡啶环 (Pyridine)"
                    notation = "-C5H4N"
                    pattern = "c1ccncc1"
                ElseIf heteroAtoms.Count = 1 AndAlso heteroAtoms(0) = "O" Then
                    name = "呋喃环 (Furan)"
                    notation = "-C4H3O"
                    pattern = "c1ccoc1"
                ElseIf heteroAtoms.Count = 1 AndAlso heteroAtoms(0) = "S" Then
                    name = "噻吩环 (Thiophene)"
                    notation = "-C4H3S"
                    pattern = "c1ccsc1"
                ElseIf heteroAtoms.Count = 1 AndAlso heteroAtoms(0) = "N" AndAlso ring.Count = 5 Then
                    name = "吡咯环 (Pyrrole)"
                    notation = "-C4H4N"
                    pattern = "c1cc[nH]c1"
                Else
                    name = "芳香杂环 (Aromatic Heterocycle, " & String.Join("/", heteroAtoms) & ")"
                    notation = "ArHet"
                    pattern = ring.Count & "-membered aromatic ring"
                End If

                AddGroup(groups, name, notation, GroupCategory.Fragment, pattern, ring)
            Next
        End Sub

        ''' <summary>
        ''' 识别环烷烃（环己烷、环戊烷等）。
        ''' 条件：环大小3-7，所有原子均为脂肪族碳，且无双键/三键。
        ''' </summary>
        Private Sub DetectCycloalkanes(groups As List(Of FunctionalGroup))
            For Each ring In _allRings
                If ring.Count < 3 OrElse ring.Count > 7 Then Continue For
                If Not IsAvailable(ring(0)) Then Continue For

                Dim allCarbon As Boolean = True
                For Each a In ring
                    If Not _mol.IsAliphaticCarbon(a) Then
                        allCarbon = False
                        Exit For
                    End If
                Next
                If Not allCarbon Then Continue For

                ' 检查环内无双键/三键
                Dim hasUnsaturation As Boolean = False
                For i As Integer = 0 To ring.Count - 1
                    Dim j As Integer = (i + 1) Mod ring.Count
                    Dim b = _mol.GetBond(ring(i), ring(j))
                    If b IsNot Nothing AndAlso b.Order > 1.0 Then
                        hasUnsaturation = True
                        Exit For
                    End If
                Next
                If hasUnsaturation Then Continue For

                Dim name As String, notation As String
                Select Case ring.Count
                    Case 3
                        name = "环丙烷 (Cyclopropane)"
                        notation = "C3H6"
                    Case 4
                        name = "环丁烷 (Cyclobutane)"
                        notation = "C4H8"
                    Case 5
                        name = "环戊烷 (Cyclopentane)"
                        notation = "C5H10"
                    Case 6
                        name = "环己烷 (Cyclohexane)"
                        notation = "C6H12"
                    Case 7
                        name = "环庚烷 (Cycloheptane)"
                        notation = "C7H14"
                    Case Else
                        name = "环烷烃 (Cycloalkane, " & ring.Count & "-membered)"
                        notation = "C" & ring.Count & "H" & (ring.Count * 2)
                End Select

                AddGroup(groups, name, notation, GroupCategory.Fragment,
                         ring.Count & "-membered carbon ring", ring)
            Next
        End Sub

        ''' <summary>
        ''' 识别杂环（非芳香的含杂原子环，如四氢呋喃、吡咯烷等）。
        ''' 条件：环大小3-7，含至少一个杂原子，所有原子非芳香。
        ''' </summary>
        Private Sub DetectHeterocycles(groups As List(Of FunctionalGroup))
            For Each ring In _allRings
                If ring.Count < 3 OrElse ring.Count > 7 Then Continue For
                If Not IsAvailable(ring(0)) Then Continue For

                ' 检查所有原子非芳香
                Dim anyAromatic As Boolean = False
                For Each a In ring
                    If _mol.Atoms(a).IsAromatic Then
                        anyAromatic = True
                        Exit For
                    End If
                Next
                If anyAromatic Then Continue For

                ' 统计杂原子
                Dim heteroSyms As New List(Of String)()
                For Each a In ring
                    Dim sym As String = _mol.Atoms(a).Symbol
                    If sym <> "C" AndAlso sym <> "H" Then
                        heteroSyms.Add(sym)
                    End If
                Next
                If heteroSyms.Count = 0 Then Continue For

                ' 检查环内无双键/三键（饱和杂环）
                Dim hasUnsaturation As Boolean = False
                For i As Integer = 0 To ring.Count - 1
                    Dim j As Integer = (i + 1) Mod ring.Count
                    Dim b = _mol.GetBond(ring(i), ring(j))
                    If b IsNot Nothing AndAlso b.Order > 1.0 Then
                        hasUnsaturation = True
                        Exit For
                    End If
                Next
                If hasUnsaturation Then Continue For

                Dim name As String, notation As String
                If heteroSyms.Count = 1 Then
                    Select Case heteroSyms(0)
                        Case "O"
                            name = "环醚 (Cyclic Ether / Oxolane)"
                            notation = "C" & (ring.Count - 1) & "H" & (ring.Count * 2 - 2) & "O"
                        Case "N"
                            name = "环胺 (Cyclic Amine / Pyrrolidine)"
                            notation = "C" & (ring.Count - 1) & "H" & (ring.Count * 2 - 1) & "N"
                        Case "S"
                            name = "环硫醚 (Cyclic Thioether / Thiolane)"
                            notation = "C" & (ring.Count - 1) & "H" & (ring.Count * 2 - 2) & "S"
                        Case Else
                            name = "杂环 (Heterocycle with " & heteroSyms(0) & ")"
                            notation = "Het[" & heteroSyms(0) & "]"
                    End Select
                Else
                    name = "杂环 (Heterocycle, " & String.Join("/", heteroSyms) & ")"
                    notation = "Het"
                End If

                AddGroup(groups, name, notation, GroupCategory.Fragment,
                         ring.Count & "-membered heterocycle", ring)
            Next
        End Sub

        ' ====================================================================
        ' 含氧官能团识别
        ' ====================================================================

        ''' <summary>
        ''' 识别羧基 -COOH。
        ''' 模式：C 同时连接 =O 和 -OH（O 有氢）。
        ''' </summary>
        Private Sub DetectCarboxylicAcid(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For

                ' 查找连接的 -OH 氧原子
                Dim ohOxygens = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "O")
                Dim ohO As Integer = -1
                For Each o In ohOxygens
                    If o <> co AndAlso _mol.GetHCount(o) >= 1 Then
                        ohO = o
                        Exit For
                    End If
                Next
                If ohO < 0 Then Continue For

                AddGroup(groups, "羧基 (Carboxyl)", "-COOH", GroupCategory.Group,
                         "C(=O)OH", {i, co, ohO})
            Next
        End Sub

        ''' <summary>
        ''' 识别酯基 -COO-。
        ''' 模式：C 连接 =O 和 -O-C（O 无氢，连接另一个碳）。
        ''' </summary>
        Private Sub DetectEster(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For

                ' 查找连接的 -O-C 氧原子（无氢，连接另一个碳）
                Dim oxygens = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "O")
                Dim esterO As Integer = -1
                For Each o In oxygens
                    If o = co Then Continue For
                    If _mol.GetHCount(o) > 0 Then Continue For
                    ' 检查该氧是否连接另一个碳
                    For Each n In _mol.GetNeighbors(o)
                        If n <> i AndAlso _mol.IsCarbon(n) Then
                            esterO = o
                            Exit For
                        End If
                    Next
                    If esterO >= 0 Then Exit For
                Next
                If esterO < 0 Then Continue For

                AddGroup(groups, "酯基 (Ester)", "-COO-", GroupCategory.Group,
                         "C(=O)O-C", {i, co, esterO})
            Next
        End Sub

        ''' <summary>
        ''' 识别醛基 -CHO。
        ''' 模式：C 连接 =O，且 C 有至少1个氢（即 C-H）。
        ''' </summary>
        Private Sub DetectAldehyde(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For

                If _mol.GetHCount(i) < 1 Then Continue For

                AddGroup(groups, "醛基 (Aldehyde)", "-CHO", GroupCategory.Group,
                         "C(=O)H", {i, co})
            Next
        End Sub

        ''' <summary>
        ''' 识别酮基 >C=O。
        ''' 模式：C 连接 =O，且 C 连接2个碳取代基，无氢。
        ''' </summary>
        Private Sub DetectKetone(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For

                If _mol.GetHCount(i) > 0 Then Continue For

                ' 检查是否有2个碳取代基
                Dim carbonCount As Integer = 0
                For Each n In _mol.GetNeighbors(i)
                    If n <> co AndAlso _mol.IsCarbon(n) Then
                        carbonCount += 1
                    End If
                Next
                If carbonCount < 2 Then Continue For

                AddGroup(groups, "酮基 (Ketone)", ">C=O", GroupCategory.Group,
                         "C(=O) with 2 C substituents", {i, co})
            Next
        End Sub

        ''' <summary>
        ''' 识别羰基 C=O（未被以上基团覆盖的残留羰基）。
        ''' </summary>
        Private Sub DetectCarbonyl(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For
                If Not IsAvailable(co) Then Continue For

                AddGroup(groups, "羰基 (Carbonyl)", "-C=O", GroupCategory.Group,
                         "C=O", {i, co})
            Next
        End Sub

        ''' <summary>
        ''' 识别羟基 -OH。
        ''' 模式：O 连接到碳，且有至少1个氢。
        ''' </summary>
        Private Sub DetectHydroxyl(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsOxygen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) < 1 Then Continue For

                ' 检查是否连接碳
                Dim hasC As Boolean = False
                For Each n In _mol.GetNeighbors(i)
                    If _mol.IsCarbon(n) Then
                        hasC = True
                        Exit For
                    End If
                Next
                If Not hasC Then Continue For

                AddGroup(groups, "羟基 (Hydroxyl)", "-OH", GroupCategory.Group,
                         "O-H on carbon", {i})
            Next
        End Sub

        ''' <summary>
        ''' 识别醚键 -O-。
        ''' 模式：O 连接2个碳，无氢。
        ''' </summary>
        Private Sub DetectEther(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsOxygen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) > 0 Then Continue For

                Dim carbonNeighbors = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "C")
                If carbonNeighbors.Count < 2 Then Continue For

                AddGroup(groups, "醚键 (Ether)", "-O-", GroupCategory.Group,
                         "C-O-C", {i})
            Next
        End Sub

        ''' <summary>
        ''' 识别过氧键 -O-O-。
        ''' 模式：两个 O 直接相连。
        ''' </summary>
        Private Sub DetectPeroxide(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsOxygen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                For Each n In _mol.GetNeighbors(i)
                    If n > i AndAlso _mol.IsOxygen(n) AndAlso IsAvailable(n) Then
                        AddGroup(groups, "过氧键 (Peroxide)", "-O-O-", GroupCategory.Group,
                                 "O-O", {i, n})
                        Exit For
                    End If
                Next
            Next
        End Sub

        ' ====================================================================
        ' 含氮官能团识别
        ' ====================================================================

        ''' <summary>
        ''' 识别酰胺基 -CONH-。
        ''' 模式：C 连接 =O 和 N。
        ''' </summary>
        Private Sub DetectAmide(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim co As Integer = GetDoubleBondedNeighbor(i)
                If co < 0 OrElse Not _mol.IsOxygen(co) Then Continue For

                Dim nitrogens = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "N")
                If nitrogens.Count = 0 Then Continue For

                Dim nAtom As Integer = nitrogens(0)
                If Not IsAvailable(nAtom) Then Continue For

                AddGroup(groups, "酰胺基 (Amide)", "-CONH-", GroupCategory.Group,
                         "C(=O)N", {i, co, nAtom})
            Next
        End Sub

        ''' <summary>
        ''' 识别硝基 -NO2。
        ''' 模式：N 连接2个以上 O，其中至少1个为双键。
        ''' </summary>
        Private Sub DetectNitro(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsNitrogen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim oxygens = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "O")
                If oxygens.Count < 2 Then Continue For

                Dim hasDouble As Boolean = False
                For Each o In oxygens
                    If _mol.IsDoubleBond(i, o) Then
                        hasDouble = True
                        Exit For
                    End If
                Next
                If Not hasDouble Then Continue For

                Dim atoms As New List(Of Integer) From {i}
                atoms.AddRange(oxygens)
                AddGroup(groups, "硝基 (Nitro)", "-NO2", GroupCategory.Group,
                         "[N+](=O)[O-]", atoms)
            Next
        End Sub

        ''' <summary>
        ''' 识别氰基 -C#N。
        ''' 模式：C 通过三键连接 N。
        ''' </summary>
        Private Sub DetectNitrile(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim n As Integer = GetTripleBondedNeighbor(i)
                If n < 0 OrElse Not _mol.IsNitrogen(n) Then Continue For
                If Not IsAvailable(n) Then Continue For

                AddGroup(groups, "氰基 (Nitrile)", "-C#N", GroupCategory.Group,
                         "C#N", {i, n})
            Next
        End Sub

        ''' <summary>
        ''' 识别偶氮基 -N=N-。
        ''' 模式：两个 N 通过双键相连。
        ''' </summary>
        Private Sub DetectAzo(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsNitrogen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim n As Integer = GetDoubleBondedNeighbor(i)
                If n < 0 OrElse Not _mol.IsNitrogen(n) Then Continue For
                If n <= i Then Continue For
                If Not IsAvailable(n) Then Continue For

                AddGroup(groups, "偶氮基 (Azo)", "-N=N-", GroupCategory.Group,
                         "N=N", {i, n})
            Next
        End Sub

        ''' <summary>
        ''' 识别亚胺基 C=N。
        ''' 模式：C 通过双键连接 N（未被酰胺等覆盖的）。
        ''' </summary>
        Private Sub DetectImine(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim n As Integer = GetDoubleBondedNeighbor(i)
                If n < 0 OrElse Not _mol.IsNitrogen(n) Then Continue For
                If Not IsAvailable(n) Then Continue For

                AddGroup(groups, "亚胺基 (Imine)", "C=N", GroupCategory.Group,
                         "C=N", {i, n})
            Next
        End Sub

        ''' <summary>
        ''' 识别胺基 -NH2, -NH-, >N-。
        ''' 根据氮原子上的氢数和碳取代基数区分伯/仲/叔胺。
        ''' </summary>
        Private Sub DetectAmine(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsNitrogen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim hCount As Integer = _mol.GetHCount(i)
                Dim carbonNeighbors = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "C")

                Dim name As String, notation As String, pattern As String
                If hCount >= 2 Then
                    name = "伯氨基 (Primary Amine)"
                    notation = "-NH2"
                    pattern = "N with 2 H"
                ElseIf hCount = 1 Then
                    name = "仲氨基 (Secondary Amine)"
                    notation = "-NH-"
                    pattern = "N with 1 H"
                ElseIf hCount = 0 AndAlso carbonNeighbors.Count >= 3 Then
                    name = "叔氨基 (Tertiary Amine)"
                    notation = ">N-"
                    pattern = "N with 0 H, 3 C"
                Else
                    Continue For
                End If

                AddGroup(groups, name, notation, GroupCategory.Group, pattern, {i})
            Next
        End Sub

        ' ====================================================================
        ' 含硫官能团识别
        ' ====================================================================

        ''' <summary>
        ''' 识别磺酸基 -SO3H。
        ''' 模式：S 连接3个以上 O，其中至少2个为双键。
        ''' </summary>
        Private Sub DetectSulfonicAcid(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsSulfur(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim oxygens = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "O")
                If oxygens.Count < 3 Then Continue For

                Dim dblCount As Integer = 0
                For Each o In oxygens
                    If _mol.IsDoubleBond(i, o) Then dblCount += 1
                Next
                If dblCount < 2 Then Continue For

                Dim atoms As New List(Of Integer) From {i}
                atoms.AddRange(oxygens)
                AddGroup(groups, "磺酸基 (Sulfonic Acid)", "-SO3H", GroupCategory.Group,
                         "S(=O)(=O)O", atoms)
            Next
        End Sub

        ''' <summary>
        ''' 识别砜基 -SO2-。
        ''' 模式：S 连接2个双键 O（未被磺酸基覆盖）。
        ''' </summary>
        Private Sub DetectSulfonyl(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsSulfur(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim dblOxygens As New List(Of Integer)()
                For Each n In _mol.GetNeighbors(i)
                    If _mol.IsOxygen(n) AndAlso _mol.IsDoubleBond(i, n) Then
                        dblOxygens.Add(n)
                    End If
                Next
                If dblOxygens.Count < 2 Then Continue For

                Dim atoms As New List(Of Integer) From {i}
                atoms.AddRange(dblOxygens)
                AddGroup(groups, "砜基 (Sulfonyl)", "-SO2-", GroupCategory.Group,
                         "S(=O)(=O)", atoms)
            Next
        End Sub

        ''' <summary>
        ''' 识别亚砜基 -S=O。
        ''' 模式：S 连接1个双键 O（未被以上覆盖）。
        ''' </summary>
        Private Sub DetectSulfoxide(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsSulfur(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim o As Integer = GetDoubleBondedNeighbor(i)
                If o < 0 OrElse Not _mol.IsOxygen(o) Then Continue For
                If Not IsAvailable(o) Then Continue For

                AddGroup(groups, "亚砜基 (Sulfoxide)", "-S=O", GroupCategory.Group,
                         "S=O", {i, o})
            Next
        End Sub

        ''' <summary>
        ''' 识别巯基 -SH。
        ''' 模式：S 有至少1个氢。
        ''' </summary>
        Private Sub DetectThiol(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsSulfur(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) < 1 Then Continue For

                AddGroup(groups, "巯基 (Thiol)", "-SH", GroupCategory.Group,
                         "S-H", {i})
            Next
        End Sub

        ''' <summary>
        ''' 识别硫醚键 -S-。
        ''' 模式：S 连接2个碳，无氢，无双键。
        ''' </summary>
        Private Sub DetectThioether(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsSulfur(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) > 0 Then Continue For

                Dim carbonNeighbors = _mol.GetNeighborsWhere(i, Function(a) a.Symbol = "C")
                If carbonNeighbors.Count < 2 Then Continue For

                ' 确保无双键（排除亚砜/砜）
                Dim hasDouble As Boolean = False
                For Each b In _mol.Atoms(i).Bonds
                    If b.Order = 2.0 Then
                        hasDouble = True
                        Exit For
                    End If
                Next
                If hasDouble Then Continue For

                AddGroup(groups, "硫醚键 (Thioether)", "-S-", GroupCategory.Group,
                         "C-S-C", {i})
            Next
        End Sub

        ' ====================================================================
        ' 不饱和键识别
        ' ====================================================================

        ''' <summary>
        ''' 识别炔键 -C#C-。
        ''' 模式：两个 C 通过三键相连。
        ''' </summary>
        Private Sub DetectAlkyne(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim c As Integer = GetTripleBondedNeighbor(i)
                If c < 0 OrElse Not _mol.IsCarbon(c) Then Continue For
                If c <= i Then Continue For
                If Not IsAvailable(c) Then Continue For

                AddGroup(groups, "炔键 (Alkyne)", "-C#C-", GroupCategory.Group,
                         "C#C", {i, c})
            Next
        End Sub

        ''' <summary>
        ''' 识别烯键 -C=C-。
        ''' 模式：两个 C 通过双键相连（非芳香，非羰基等）。
        ''' </summary>
        Private Sub DetectAlkene(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim c As Integer = GetDoubleBondedNeighbor(i)
                If c < 0 OrElse Not _mol.IsAliphaticCarbon(c) Then Continue For
                If c <= i Then Continue For
                If Not IsAvailable(c) Then Continue For

                AddGroup(groups, "烯键 (Alkene)", "-C=C-", GroupCategory.Group,
                         "C=C", {i, c})
            Next
        End Sub

        ' ====================================================================
        ' 烷基识别
        ' ====================================================================

        ''' <summary>
        ''' 识别甲基 -CH3。
        ''' 模式：C 有3个氢，且连接1个非氢原子。
        ''' </summary>
        Private Sub DetectMethyl(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) <> 3 Then Continue For

                AddGroup(groups, "甲基 (Methyl)", "-CH3", GroupCategory.Group,
                         "CH3-", {i})
            Next
        End Sub

        ''' <summary>
        ''' 识别亚甲基 -CH2-。
        ''' 模式：C 有2个氢，连接2个非氢原子。
        ''' </summary>
        Private Sub DetectMethylene(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) <> 2 Then Continue For

                AddGroup(groups, "亚甲基 (Methylene)", "-CH2-", GroupCategory.Group,
                         "-CH2-", {i})
            Next
        End Sub

        ''' <summary>
        ''' 识别次甲基 =CH-。
        ''' 模式：C 有1个氢，连接3个非氢原子。
        ''' </summary>
        Private Sub DetectMethine(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsAliphaticCarbon(i) Then Continue For
                If Not IsAvailable(i) Then Continue For
                If _mol.GetHCount(i) <> 1 Then Continue For

                AddGroup(groups, "次甲基 (Methine)", "=CH-", GroupCategory.Group,
                         "-CH<", {i})
            Next
        End Sub

        ' ====================================================================
        ' 卤素识别
        ' ====================================================================

        ''' <summary>
        ''' 识别卤素 -F, -Cl, -Br, -I。
        ''' </summary>
        Private Sub DetectHalides(groups As List(Of FunctionalGroup))
            For i As Integer = 0 To _mol.Atoms.Count - 1
                If Not _mol.IsHalogen(i) Then Continue For
                If Not IsAvailable(i) Then Continue For

                Dim sym As String = _mol.Atoms(i).Symbol
                Dim name As String, notation As String
                Select Case sym
                    Case "F"
                        name = "氟 (Fluoro)"
                        notation = "-F"
                    Case "Cl"
                        name = "氯 (Chloro)"
                        notation = "-Cl"
                    Case "Br"
                        name = "溴 (Bromo)"
                        notation = "-Br"
                    Case "I"
                        name = "碘 (Iodo)"
                        notation = "-I"
                    Case Else
                        Continue For
                End Select

                AddGroup(groups, name, notation, GroupCategory.Group,
                         sym & " on carbon", {i})
            Next
        End Sub

    End Class

End Namespace
