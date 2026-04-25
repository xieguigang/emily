''' <summary>
''' 生化反应模块。
''' 等价于 Python equilibrator_cache/reaction.py。
''' 定义了反应类和化学计量矩阵构建函数。
''' </summary>
Imports System.Text.RegularExpressions
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports EquilibratorCache.Stubs

''' <summary>
''' 生化反应类。
''' 等价于 Python Reaction。
''' 表示一个生化反应，包含反应物、产物及其化学计量系数。
''' </summary>
Public Class Reaction

    ''' <summary>
    ''' 可能的反应箭头符号列表。
    ''' 等价于 Python POSSIBLE_REACTION_ARROWS。
    ''' </summary>
    Public Shared ReadOnly POSSIBLE_REACTION_ARROWS As String() = {
        "<=>", "<->", "-->", "<--", ' 三字符箭头       
        "=>", "<=", "->", "<-",  ' 两字符箭头       
        "=", ChrW(&H21CC), ChrW(&H2100), ChrW(&H210B), ChrW(&H21BD)  ' 单字符箭头
    }

    ' =========================================================================
    ' 属性
    ' =========================================================================

    ''' <summary>
    ''' 稀疏表示的反应物字典。
    ''' 键为化合物，值为化学计量系数（负数为反应物，正数为产物）。
    ''' </summary>
    Public Property Sparse As Dictionary(Of Compound, Double)

    ''' <summary>
    ''' 反应箭头符号。
    ''' </summary>
    Public Property Arrow As String

    ''' <summary>
    ''' 反应 ID。
    ''' </summary>
    Public Property Rid As String

    ' =========================================================================
    ' 构造函数
    ' =========================================================================

    ''' <summary>
    ''' 创建一个新的反应对象。
    ''' 等价于 Python Reaction.__init__(sparse, arrow, rid)。
    ''' </summary>
    ''' <param name="sparse">反应物字典（化合物 -> 化学计量系数）</param>
    ''' <param name="arrow">反应箭头符号</param>
    ''' <param name="rid">反应 ID</param>
    Public Sub New(sparse As Dictionary(Of Compound, Double),
                   Optional arrow As String = Nothing,
                   Optional rid As String = Nothing)
        ' 过滤掉系数为 0 的化合物
        Me.Sparse = New Dictionary(Of Compound, Double)()
        For Each kvp In sparse
            If kvp.Value <> 0 Then
                Me.Sparse(kvp.Key) = kvp.Value
            End If
        Next
        Me.Arrow = If(arrow, POSSIBLE_REACTION_ARROWS(0))
        Me.Rid = rid
    End Sub

    ' =========================================================================
    ' 公共方法
    ' =========================================================================

    ''' <summary>
    ''' 获取反应物数量。
    ''' 等价于 Python __len__。
    ''' </summary>
    Public ReadOnly Property Count As Integer
        Get
            Return Sparse.Count
        End Get
    End Property

    ''' <summary>
    ''' 创建此反应的副本。
    ''' 等价于 Python clone()。
    ''' </summary>
    ''' <returns>新的 Reaction 对象</returns>
    Public Function Clone() As Reaction
        Return New Reaction(New Dictionary(Of Compound, Double)(Sparse), Arrow, Rid)
    End Function

    ''' <summary>
    ''' 迭代反应物。
    ''' 等价于 Python keys(protons, water)。
    ''' </summary>
    ''' <param name="protons">是否包含质子</param>
    ''' <param name="water">是否包含水</param>
    ''' <returns>化合物迭代器</returns>
    Public Iterator Function Keys(Optional protons As Boolean = True,
                                   Optional water As Boolean = True) As IEnumerable(Of Compound)
        For Each c In Sparse.Keys
            If Not water AndAlso c.InChI = EquilibratorConstants.WATER_INCHI Then Continue For
            If Not protons AndAlso c.InChI = EquilibratorConstants.PROTON_INCHI Then Continue For
            Yield c
        Next
    End Function

    ''' <summary>
    ''' 迭代反应物及其化学计量系数。
    ''' 等价于 Python items(protons, water)。
    ''' </summary>
    ''' <param name="protons">是否包含质子</param>
    ''' <param name="water">是否包含水</param>
    ''' <returns>键值对迭代器</returns>
    Public Iterator Function Items(Optional protons As Boolean = True,
                                    Optional water As Boolean = True) As IEnumerable(Of KeyValuePair(Of Compound, Double))
        For Each kvp In Sparse
            If Not water AndAlso kvp.Key.InChI = EquilibratorConstants.WATER_INCHI Then Continue For
            If Not protons AndAlso kvp.Key.InChI = EquilibratorConstants.PROTON_INCHI Then Continue For
            Yield kvp
        Next
    End Function

    ''' <summary>
    ''' 获取指定化合物的化学计量系数。
    ''' 等价于 Python get_coeff(compound)。
    ''' </summary>
    ''' <param name="compound">目标化合物</param>
    ''' <returns>化学计量系数，如果化合物不在反应中则返回 0</returns>
    Public Function GetCoeff(compound As Compound) As Double
        If Sparse.ContainsKey(compound) Then
            Return Sparse(compound)
        Else
            Return 0.0
        End If
    End Function

    ''' <summary>
    ''' 反转反应方向。
    ''' 等价于 Python reverse()。
    ''' 将所有化学计量系数取反。
    ''' </summary>
    ''' <returns>反向反应</returns>
    Public Function Reverse() As Reaction
        Dim newSparse As New Dictionary(Of Compound, Double)()
        For Each kvp In Sparse
            newSparse(kvp.Key) = -kvp.Value
        Next
        Return New Reaction(newSparse, Arrow, Rid)
    End Function

    ''' <summary>
    ''' 解析反应式的一侧。
    ''' 等价于 Python parse_formula_side(s, str_to_compound)。
    ''' 例如 "2 kegg:C00001 + kegg:C00002 + 3 kegg:C00003"。
    ''' </summary>
    ''' <param name="s">反应式一侧的字符串</param>
    ''' <param name="strToCompound">将字符串转换为化合物的函数</param>
    ''' <returns>化合物到化学计量系数的字典</returns>
    Public Shared Function ParseFormulaSide(s As String,
                                             strToCompound As Func(Of String, Compound)) As Dictionary(Of Compound, Double)
        If s.Trim() = "null" Then Return New Dictionary(Of Compound, Double)()

        Dim compoundBag As New Dictionary(Of Compound, Double)()
        Dim members As String() = Regex.Split(s, "\s+\+\s+")

        For Each member In members
            Dim tokens As String() = member.Split(New Char() {" "c, vbTab(0)}, StringSplitOptions.RemoveEmptyEntries)

            If tokens.Length = 0 Then Continue For

            Dim amount As Double
            Dim compound As Compound

            If tokens.Length = 1 Then
                amount = 1.0
                compound = strToCompound(member.Trim())
            Else
                If Not Double.TryParse(tokens(0), amount) Then
                    Throw New ParseException($"非特异性反应: {s}")
                End If
                compound = strToCompound(tokens(1).Trim())
            End If

            If compound Is Nothing Then
                Throw New ParseException($"{member} 未在化合物缓存中找到")
            End If

            If compoundBag.ContainsKey(compound) Then
                compoundBag(compound) += amount
            Else
                compoundBag(compound) = amount
            End If
        Next

        Return compoundBag
    End Function

    ''' <summary>
    ''' 解析双侧反应式。
    ''' 等价于 Python parse_formula(str_to_compound, formula, rid)。
    ''' 
    ''' 示例：
    '''   Reaction.ParseFormula(parseCompound, "2 C00001 = C00002 + C00003")
    ''' </summary>
    ''' <param name="strToCompound">将字符串转换为化合物的函数</param>
    ''' <param name="formula">反应式字符串</param>
    ''' <param name="rid">反应 ID（可选）</param>
    ''' <returns>解析后的 Reaction 对象</returns>
    Public Shared Function ParseFormula(strToCompound As Func(Of String, Compound),
                                          formula As String,
                                          Optional rid As String = Nothing) As Reaction
        Dim tokens As String() = Nothing
        Dim arrow As String = Nothing

        For Each a In POSSIBLE_REACTION_ARROWS
            If formula.Contains(a) Then
                tokens = formula.Split(New String() {a}, 2, StringSplitOptions.None)
                arrow = a
                Exit For
            End If
        Next

        If tokens Is Nothing OrElse tokens.Length < 2 Then
            Throw New ParseException(
                $"反应式不包含允许的箭头符号 ({arrow}): {formula}")
        End If

        Dim left As String = tokens(0).Trim()
        Dim right As String = tokens(1).Trim()

        Dim sparseReaction As New Dictionary(Of Compound, Double)()
        Dim leftDict As Dictionary(Of Compound, Double) = ParseFormulaSide(left, strToCompound)
        Dim rightDict As Dictionary(Of Compound, Double) = ParseFormulaSide(right, strToCompound)

        For Each kvp In leftDict
            If sparseReaction.ContainsKey(kvp.Key) Then
                sparseReaction(kvp.Key) -= kvp.Value
            Else
                sparseReaction(kvp.Key) = -kvp.Value
            End If
        Next

        For Each kvp In rightDict
            If sparseReaction.ContainsKey(kvp.Key) Then
                sparseReaction(kvp.Key) += kvp.Value
            Else
                sparseReaction(kvp.Key) = kvp.Value
            End If
        Next

        ' 移除系数为 0 的化合物
        Dim filtered As New Dictionary(Of Compound, Double)()
        For Each kvp In sparseReaction
            If kvp.Value <> 0 Then filtered(kvp.Key) = kvp.Value
        Next

        Return New Reaction(filtered, arrow, rid)
    End Function

    ''' <summary>
    ''' 将化合物和系数格式化为字符串。
    ''' 等价于 Python write_compound_and_coeff(compound, coeff)。
    ''' </summary>
    Public Shared Function WriteCompoundAndCoeff(compound As Compound, coeff As Double) As String
        If Math.Abs(coeff - 1.0) < Double.Epsilon Then
            Return compound.ToString()
        Else
            Return $"{coeff:g} {compound}"
        End If
    End Function

    ''' <summary>
    ''' 返回反应式的字符串表示。
    ''' 等价于 Python __str__。
    ''' </summary>
    Public Overrides Function ToString() As String
        Dim left As New List(Of String)()
        Dim right As New List(Of String)()

        For Each kvp In Sparse
            If kvp.Value < 0 Then
                left.Add(WriteCompoundAndCoeff(kvp.Key, -kvp.Value))
            ElseIf kvp.Value > 0 Then
                right.Add(WriteCompoundAndCoeff(kvp.Key, kvp.Value))
            End If
        Next

        Return $"{String.Join(" + ", left)} {Arrow} {String.Join(" + ", right)}"
    End Function

    ''' <summary>
    ''' 获取反应物元素组成的数据表。
    ''' 等价于 Python get_element_data_frame()。
    ''' </summary>
    ''' <returns>元素组成 DataFrame</returns>
    Public Function GetElementDataFrame() As DataFrame
        Dim atomBags As New Dictionary(Of Compound, Dictionary(Of String, Integer))()
        For Each c In Keys()
            atomBags(c) = If(c.AtomBag, New Dictionary(Of String, Integer)())
        Next

        ' 创建 DataFrame
        Dim df As New DataFrame()
        ' 占位实现：实际应构建元素矩阵
        Return df
    End Function

    ''' <summary>
    ''' 获取反应的原子袋（用于检查元素守恒）。
    ''' 等价于 Python _get_reaction_atom_bag(minimal_stoichiometry)。
    ''' </summary>
    ''' <param name="minimalStoichiometry">最小化学计量系数阈值（可选）</param>
    ''' <returns>未平衡的原子差异字典，如果无法计算则返回 Nothing</returns>
    Public Function GetReactionAtomBag(Optional minimalStoichiometry As Double? = Nothing) As Dictionary(Of String, Integer)
        Dim elementDf As DataFrame = GetElementDataFrame()

        ' 占位实现：实际应计算元素矩阵与化学计量系数的乘积
        ' 如果某些化合物的分子式缺失，返回 Nothing
        Return Nothing
    End Function

    ''' <summary>
    ''' 添加化学计量系数。
    ''' 等价于 Python add_stoichiometry(cpd, coeff)。
    ''' 如果化合物已在反应中，则增加其系数；否则添加新条目。
    ''' </summary>
    ''' <param name="cpd">化合物</param>
    ''' <param name="coeff">要添加的系数</param>
    Public Sub AddStoichiometry(cpd As Compound, coeff As Double)
        If Sparse.ContainsKey(cpd) Then
            If Sparse(cpd) = -coeff Then
                Sparse.Remove(cpd)
            Else
                Sparse(cpd) += coeff
            End If
        Else
            Sparse(cpd) = coeff
        End If
    End Sub

    ''' <summary>
    ''' 检查反应是否已平衡。
    ''' 等价于 Python is_balanced(ignore_atoms)。
    ''' </summary>
    ''' <param name="ignoreAtoms">要忽略的元素元组（默认忽略 "H"）</param>
    ''' <returns>如果反应已平衡则返回 True</returns>
    Public Function IsBalanced(Optional ignoreAtoms As String() = Nothing) As Boolean
        If ignoreAtoms Is Nothing Then ignoreAtoms = New String() {"H"}

        Dim reactionAtomBag = GetReactionAtomBag()
        If reactionAtomBag Is Nothing Then Return False

        For Each atom In ignoreAtoms
            reactionAtomBag.Remove(atom)
        Next

        Return reactionAtomBag.Count = 0
    End Function

    ''' <summary>
    ''' 使用指定化合物尝试平衡反应。
    ''' 等价于 Python balance_with_compound(compound, ignore_atoms)。
    ''' </summary>
    ''' <param name="compound">用于平衡的化合物</param>
    ''' <param name="ignoreAtoms">要忽略的元素元组</param>
    ''' <returns>平衡后的反应，如果无法平衡则返回 Nothing</returns>
    Public Function BalanceWithCompound(compound As Compound,
                                         Optional ignoreAtoms As String() = Nothing) As Reaction
        If ignoreAtoms Is Nothing Then ignoreAtoms = New String() {"H"}

        If IsBalanced(ignoreAtoms) Then Return Me

        Dim reactionAtomBag = GetReactionAtomBag()
        If reactionAtomBag Is Nothing Then Return Nothing

        ' 获取枢轴原子
        Dim pivotAtom As String = Nothing
        Dim pivotCount As Integer = 0
        If compound.AtomBag Is Nothing Then
            Throw New Exception(
                $"无法使用此化合物平衡反应，它没有分子式: {compound.Formula}")
        End If

        For Each kvp In compound.AtomBag
            If ignoreAtoms Is Nothing OrElse Not ignoreAtoms.Contains(kvp.Key) Then
                pivotAtom = kvp.Key
                pivotCount = kvp.Value
                Exit For
            End If
        Next

        If pivotAtom Is Nothing Then
            Throw New Exception(
                $"无法使用此化合物平衡反应，它没有相关原子: {compound.Formula}")
        End If

        Dim newReaction As Reaction = Clone()
        If reactionAtomBag.ContainsKey(pivotAtom) Then
            newReaction.AddStoichiometry(compound, -reactionAtomBag(pivotAtom) / CSng(pivotCount))
        End If

        If newReaction.IsBalanced(ignoreAtoms) Then
            Return newReaction
        Else
            Return Nothing
        End If
    End Function

    ''' <summary>
    ''' 检查反应是否为空。
    ''' 等价于 Python is_empty()。
    ''' </summary>
    Public ReadOnly Property IsEmpty As Boolean
        Get
            Return Sparse.Count = 0
        End Get
    End Property

    ''' <summary>
    ''' 返回反应的密集向量表示。
    ''' 等价于 Python dense(cids)。
    ''' </summary>
    ''' <param name="cids">化合物顺序列表</param>
    ''' <returns>化学计量系数数组</returns>
    Public Function Dense(cids As List(Of Compound)) As Double()
        Dim s(cids.Count - 1) As Double
        For Each kvp In Items()
            Dim idx As Integer = cids.IndexOf(kvp.Key)
            If idx >= 0 Then
                s(idx) = kvp.Value
            End If
        Next
        Return s
    End Function

    ''' <summary>
    ''' 检查此反应是否可以进行 Legendre 变换。
    ''' 等价于 Python can_be_transformed()。
    ''' 即所有反应物都可以被变换（拥有微物种数据）。
    ''' </summary>
    Public Function CanBeTransformed() As Boolean
        For Each kvp In Items()
            If Not kvp.Key.CanBeTransformed() Then
                Return False
            End If
        Next
        Return True
    End Function

    ''' <summary>
    ''' 计算反应的 Legendre 变换。
    ''' 等价于 Python transform(p_h, ionic_strength, temperature, p_mg)。
    ''' 
    ''' 对每个化合物应用 Legendre 变换，并根据化学计量系数求和。
    ''' 忽略质子（因为 pH 是受控参数）。
    ''' </summary>
    ''' <param name="pH">pH 值</param>
    ''' <param name="ionicStrength">离子强度</param>
    ''' <param name="temperature">温度</param>
    ''' <param name="pMg">pMg 值（可选，默认 14.0）</param>
    ''' <returns>变换后的相对 Gibbs 自由能</returns>
    Public Function Transform(pH As Quantity, ionicStrength As Quantity,
                               temperature As Quantity,
                               Optional pMg As Quantity = Nothing) As Quantity
        If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

        Dim ddg As New Quantity(0.0, "kJ/mol")

        For Each kvp In Items(protons:=False)
            Try
                Dim compoundTransform As Quantity = kvp.Key.Transform(
                    pH:=pH,
                    ionicStrength:=ionicStrength,
                    temperature:=temperature,
                    pMg:=pMg)
                ddg += kvp.Value * compoundTransform
            Catch ex As MissingDissociationConstantsException
                ' 跳过无法计算 Legendre 变换的化合物
                Console.WriteLine(
                    $"无法计算 {kvp.Key} 的 Legendre 变换: {ex.Message}")
            End Try
        Next

        Return ddg
    End Function

    ''' <summary>
    ''' 计算反应的 ΔG' 对 pH 的灵敏度。
    ''' 等价于 Python sensitivity_to_p_h(p_h, ionic_strength, temperature, p_mg)。
    ''' </summary>
    Public Function SensitivityToPH(pH As Quantity, ionicStrength As Quantity,
                                     temperature As Quantity,
                                     Optional pMg As Quantity = Nothing) As Quantity
        If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

        Dim sensitivity As New Quantity(0.0, "kJ/mol")

        For Each kvp In Items(protons:=False)
            Try
                Dim compoundSensitivity As Quantity = kvp.Key.SensitivityToPH(
                    pH:=pH,
                    ionicStrength:=ionicStrength,
                    temperature:=temperature,
                    pMg:=pMg)
                sensitivity += kvp.Value * compoundSensitivity
            Catch ex As MissingDissociationConstantsException
                Console.WriteLine(
                    $"无法计算 {kvp.Key} 的 Legendre 变换: {ex.Message}")
            End Try
        Next

        Return sensitivity
    End Function

    ''' <summary>
    ''' 计算反应的 ΔG' 对离子强度的灵敏度。
    ''' 等价于 Python sensitivity_to_I(p_h, ionic_strength, temperature, p_mg)。
    ''' </summary>
    Public Function SensitivityToI(pH As Quantity, ionicStrength As Quantity,
                                    temperature As Quantity,
                                    Optional pMg As Quantity = Nothing) As Quantity
        If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

        Dim sensitivity As New Quantity(0.0, "kJ/mol/molar")

        For Each kvp In Items(protons:=False)
            Try
                Dim compoundSensitivity As Quantity = kvp.Key.SensitivityToI(
                    pH:=pH,
                    ionicStrength:=ionicStrength,
                    temperature:=temperature,
                    pMg:=pMg)
                sensitivity += kvp.Value * compoundSensitivity
            Catch ex As MissingDissociationConstantsException
                Console.WriteLine(
                    $"无法计算 {kvp.Key} 的 Legendre 变换: {ex.Message}")
            End Try
        Next

        Return sensitivity
    End Function

    ''' <summary>
    ''' 计算所有化学计量系数之和（排除质子和水）。
    ''' 等价于 Python _sum_coefficients()。
    ''' 用于将 ΔG0 转换到另一组标准浓度（如 1 mM）。
    ''' </summary>
    Public Function SumCoefficients() As Double
        Dim total As Double = 0.0
        For Each kvp In Items(protons:=False, water:=False)
            total += kvp.Value
        Next
        Return total
    End Function

    ''' <summary>
    ''' 计算所有化学计量系数绝对值之和（排除质子和水）。
    ''' 等价于 Python _sum_absolute_coefficients()。
    ''' 用于计算可逆性指数。
    ''' </summary>
    Public Function SumAbsoluteCoefficients() As Double
        Dim total As Double = 0.0
        For Each kvp In Items(protons:=False, water:=False)
            total += Math.Abs(kvp.Value)
        Next
        Return total
    End Function

    ''' <summary>
    ''' 检查此反应是否为平衡的半反应（氧化还原）。
    ''' 等价于 Python check_half_reaction_balancing()。
    ''' </summary>
    ''' <returns>缺失的电子数，如果反应不是原子级平衡则返回 Nothing</returns>
    Public Function CheckHalfReactionBalancing() As Integer?
        Dim atomBag = GetReactionAtomBag()
        If atomBag Is Nothing Then Return Nothing

        ' 忽略质子平衡
        atomBag.Remove("H")

        Dim nE As Integer = 0
        If atomBag.ContainsKey("e-") Then
            nE = atomBag("e-")
            atomBag.Remove("e-")
        End If

        If atomBag.Count > 0 Then
            Return Nothing
        Else
            Return nE
        End If
    End Function

    ''' <summary>
    ''' 生成可哈希的反应物元组。
    ''' 等价于 Python _hashable_reactants(sparse)。
    ''' </summary>
    Private Shared Function HashableReactants(sparse As Dictionary(Of Compound, Double)) As Tuple(Of Integer, Double)()
        If sparse.Count = 0 Then Return Array.Empty(Of Tuple(Of Integer, Double))()

        ' 按 Compound.ID 排序
        Dim sortedCompounds = sparse.Keys.OrderBy(Function(c) c.Id).ToList()
        Dim sortedList As New List(Of Tuple(Of Integer, Double))()

        For Each cpd In sortedCompounds
            Dim coeff As Double = sparse(cpd)
            If coeff <> 0 Then
                sortedList.Add(Tuple.Create(cpd.Id, coeff))
            End If
        Next

        If sortedList.Count = 0 Then
            Console.WriteLine("所有化学计量系数为 0")
            Return Array.Empty(Of Tuple(Of Integer, Double))()
        End If

        ' 归一化：使第一个化合物的系数为 1
        Dim normFactor As Double = 1.0 / sortedList(0).Item2
        Dim result(sortedList.Count - 1) As Tuple(Of Integer, Double)
        For i As Integer = 0 To sortedList.Count - 1
            result(i) = Tuple.Create(sortedList(i).Item1, normFactor * sortedList(i).Item2)
        Next

        Return result
    End Function

    ''' <summary>
    ''' 比较两个反应是否相等（忽略质子）。
    ''' 等价于 Python __eq__。
    ''' </summary>
    Public Overrides Function Equals(obj As Object) As Boolean
        If TypeOf obj IsNot Reaction Then Return False
        Dim other As Reaction = DirectCast(obj, Reaction)

        Dim cpds As New HashSet(Of Compound)()
        For Each c In Keys(protons:=False) : cpds.Add(c) : Next
        For Each c In other.Keys(protons:=False) : cpds.Add(c) : Next

        For Each c In cpds
            If GetCoeff(c) <> other.GetCoeff(c) Then Return False
        Next
        Return True
    End Function

    ''' <summary>
    ''' 获取反应的哈希码。
    ''' 等价于 Python __hash__。
    ''' </summary>
    Public Overrides Function GetHashCode() As Integer
        Dim reactants = HashableReactants(Sparse)
        Dim hash As Integer = 17
        For Each t In reactants
            hash = hash * 31 + t.Item1.GetHashCode() + t.Item2.GetHashCode()
        Next
        Return hash
    End Function

End Class

''' <summary>
''' 化学计量矩阵构建模块。
''' 等价于 Python create_stoichiometric_matrix_from_reactions()。
''' </summary>
Public Module StoichiometricMatrix

    ''' <summary>
    ''' 从反应列表构建化学计量矩阵。
    ''' 等价于 Python create_stoichiometric_matrix_from_reactions(reactions, is_proton, is_water, water)。
    ''' </summary>
    ''' <param name="reactions">反应集合</param>
    ''' <param name="isProton">判断化合物是否为质子的函数</param>
    ''' <param name="isWater">判断化合物是否为水的函数</param>
    ''' <param name="water">水化合物对象</param>
    ''' <returns>化学计量矩阵（DataFrame 形式）</returns>
    Public Function CreateStoichiometricMatrixFromReactions(
        reactions As IEnumerable(Of Reaction),
        isProton As Func(Of Compound, Boolean),
        isWater As Func(Of Compound, Boolean),
        water As Compound) As DataFrame

        ' 收集所有化合物（排除质子）
        Dim compounds As New HashSet(Of Compound)()
        For Each r As Reaction In reactions
            For Each c In r.Sparse.Keys
                If Not isProton(c) Then
                    compounds.Add(c)
                End If
            Next
        Next

        ' 确保水在列表中
        If Not compounds.Any(isWater) Then
            compounds.Add(water)
        End If

        Dim sortedCompounds = compounds.OrderBy(Function(c) c.Id).ToList()

        ' 构建每个反应的稀疏向量
        Dim sparseReactions As New List(Of Series)()
        For Each rxn In reactions
            Dim series As New Series(sortedCompounds.Select(Function(c) CType(c, Object)).ToArray(), GetType(Double))
            For Each kvp In rxn.Items(protons:=False)
                Dim idx As Integer = sortedCompounds.IndexOf(kvp.Key)
                If idx >= 0 Then
                    series(idx) = kvp.Value
                End If
            Next
            sparseReactions.Add(series)
        Next

        Return PandasFactory.ConcatAndFill(sparseReactions, 0.0)
    End Function

End Module
