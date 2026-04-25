''' <summary>
''' 生化化合物模型。
''' 等价于 Python equilibrator_cache/models/compound.py。
''' 表示一个生化化合物，包含其热力学性质、标识符、微物种等信息。
''' 这是 equilibrator_cache 的核心数据模型。
''' </summary>
Imports System.Text.RegularExpressions
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports Microsoft.VisualBasic.Data.IO.Pickle

Namespace Cache

    Public Class Compound

        ' =========================================================================
        ' 注册表优先级排序常量
        ' =========================================================================

        ''' <summary>
        ''' 标识符排序的注册表优先级顺序。
        ''' 从最特定/最可信到最不特定/最不可信。
        ''' 等价于 Python ORDER_OF_REGISTRIES。
        ''' </summary>
        Public Shared ReadOnly ORDER_OF_REGISTRIES As String() = {
        "MIR:00000567",  ' MetaNetX chemical
        "MIR:00000578",  ' KEGG
        "MIR:00000556",  ' BiGG
        "MIR:00000002",  ' ChEBI
        "MIR:00000552"   ' SEED
    }

        ' =========================================================================
        ' 数据库列映射
        ' =========================================================================

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>
        ''' 化合物的 InChI 标识符。
        ''' 唯一标识化学结构的国际标准标识符。
        ''' </summary>
        Public Property InChI As String

        ''' <summary>
        ''' 化合物的 InChI Key。
        ''' InChI 的哈希表示，用于快速查找。
        ''' </summary>
        Public Property InChIKey As String

        ''' <summary>
        ''' 化合物的 SMILES 描述符。
        ''' </summary>
        Public Property Smiles As String

        ''' <summary>
        ''' 化合物的常用名称。
        ''' </summary>
        Public Property CommonName As String

        ''' <summary>
        ''' 化合物的分子量（g/mol）。
        ''' </summary>
        Public Property MolecularWeight As Double?

        ''' <summary>
        ''' 原子袋（原子组成字典），以 Pickle 格式存储。
        ''' 等价于 Python atom_bag = Column(PickleType)。
        ''' 在 VB.NET 中使用 MinimalPickleUnpickler.Unpickle 反序列化。
        ''' </summary>
        Public Property AtomBagBase64 As String

        ''' <summary>
        ''' 解离常数列表，以 Pickle 格式存储。
        ''' 等价于 Python dissociation_constants = Column(PickleType)。
        ''' </summary>
        Public Property DissociationConstantsBase64 As String

        ''' <summary>
        ''' 基团贡献向量，以 Pickle 格式存储。
        ''' 等价于 Python group_vector = Column(PickleType)。
        ''' </summary>
        Public Property GroupVectorBase64 As String

        ''' <summary>
        ''' 标准 Gibbs 自由能 (kJ/mol)。
        ''' </summary>
        Public Property StandardDg As Double?

        ''' <summary>
        ''' 标准 Gibbs 自由能的不确定度 (kJ/mol)。
        ''' </summary>
        Public Property StandardDgUncertainty As Double?

        ''' <summary>
        ''' 标准生成 Gibbs 自由能 (kJ/mol)。
        ''' </summary>
        Public Property StandardDgFormation As Double?

        ''' <summary>
        ''' 标准生成 Gibbs 自由能的不确定度 (kJ/mol)。
        ''' </summary>
        Public Property StandardDgFormationUncertainty As Double?

        ''' <summary>
        ''' 标准生成焓 (kJ/mol)。
        ''' </summary>
        Public Property StandardDhFormation As Double?

        ' =========================================================================
        ' 关联关系
        ' =========================================================================

        ''' <summary>
        ''' 化合物的标识符列表。
        ''' 等价于 Python identifiers = relationship(CompoundIdentifier, ..., cascade="all, delete-orphan")。
        ''' </summary>
        Public Property Identifiers As List(Of CompoundIdentifier) = New List(Of CompoundIdentifier)()

        ''' <summary>
        ''' 化合物的微物种列表。
        ''' 等价于 Python microspecies = relationship(CompoundMicrospecies, ..., cascade="all, delete-orphan")。
        ''' </summary>
        Public Property Microspecies As List(Of CompoundMicrospecies) = New List(Of CompoundMicrospecies)()

        ''' <summary>
        ''' 化合物的镁离子解离常数列表。
        ''' 等价于 Python magnesium_dissociation_constants = relationship(MagnesiumDissociationConstant, ..., cascade="all, delete-orphan")。
        ''' </summary>
        Public Property MagnesiumDissociationConstants As List(Of MagnesiumDissociationConstant) = New List(Of MagnesiumDissociationConstant)()

        ' =========================================================================
        ' 运行时计算属性
        ' =========================================================================

        ''' <summary>
        ''' 获取原子袋字典（从 Base64 编码的 Pickle 数据反序列化）。
        ''' 等价于 Python 中的 atom_bag 属性（PickleType 自动反序列化）。
        ''' 使用 MinimalPickleUnpickler.Unpickle 进行反序列化。
        ''' </summary>
        Public ReadOnly Property AtomBag As Dictionary(Of String, Integer)
            Get
                If String.IsNullOrEmpty(AtomBagBase64) Then Return Nothing
                Try
                    Dim obj As Object = MinimalPickleUnpickler.Unpickle(AtomBagBase64)
                    If TypeOf obj Is Dictionary(Of String, Integer) Then
                        Return DirectCast(obj, Dictionary(Of String, Integer))
                    End If
                    Return Nothing
                Catch
                    Return Nothing
                End Try
            End Get
        End Property

        ''' <summary>
        ''' 获取解离常数列表（从 Base64 编码的 Pickle 数据反序列化）。
        ''' 等价于 Python 中的 dissociation_constants 属性。
        ''' </summary>
        Public ReadOnly Property DissociationConstants As List(Of Double)
            Get
                If String.IsNullOrEmpty(DissociationConstantsBase64) Then Return Nothing
                Try
                    Dim obj As Object = MinimalPickleUnpickler.Unpickle(DissociationConstantsBase64)
                    If TypeOf obj Is List(Of Double) Then
                        Return DirectCast(obj, List(Of Double))
                    End If
                    Return Nothing
                Catch
                    Return Nothing
                End Try
            End Get
        End Property

        ''' <summary>
        ''' 获取基团贡献向量（从 Base64 编码的 Pickle 数据反序列化）。
        ''' 等价于 Python 中的 group_vector 属性。
        ''' </summary>
        Public ReadOnly Property GroupVector As Double()
            Get
                If String.IsNullOrEmpty(GroupVectorBase64) Then Return Nothing
                Try
                    Dim obj As Object = MinimalPickleUnpickler.Unpickle(GroupVectorBase64)
                    If TypeOf obj Is Double() Then
                        Return DirectCast(obj, Double())
                    End If
                    Return Nothing
                Catch
                    Return Nothing
                End Try
            End Get
        End Property

        ' =========================================================================
        ' 方法
        ' =========================================================================

        ''' <summary>
        ''' 返回对象的字符串表示。
        ''' 等价于 Python __repr__。
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Compound(id={Id}, common_name={CommonName})"
        End Function

        ''' <summary>
        ''' 判断此化合物是否为质子（H+）。
        ''' 等价于 Python is_proton(self)。
        ''' 通过 InChI Key 判断。
        ''' </summary>
        Public Function IsProton() As Boolean
            Return Me.InChIKey = EquilibratorConstants.PROTON_INCHI_KEY
        End Function

        ''' <summary>
        ''' 判断此化合物是否为水（H2O）。
        ''' 等价于 Python is_water(self)。
        ''' 通过 InChI Key 判断。
        ''' </summary>
        Public Function IsWater() As Boolean
            Return Me.InChIKey = EquilibratorConstants.WATER_INCHI_KEY
        End Function

        ''' <summary>
        ''' 获取此化合物在指定条件下的变换标准 Gibbs 自由能。
        ''' 等价于 Python standard_dg_formation(pH, pMg, ionic_strength_M, T_in_K)。
        ''' 
        ''' 该方法计算化合物在给定 pH、pMg、离子强度和温度条件下的
        ''' 变换标准生成 Gibbs 自由能。计算基于微物种的 Boltzmann 加权平均。
        ''' </summary>
        ''' <param name="pH">pH 值</param>
        ''' <param name="pMg">pMg 值</param>
        ''' <param name="ionic_strength_M">离子强度（摩尔/升）</param>
        ''' <param name="T_in_K">温度（开尔文）</param>
        ''' <returns>变换标准生成 Gibbs 自由能 (kJ/mol)，如果数据不足则返回 Nothing</returns>
        Public Function StandardDgFormationTransformed(pH As Double, pMg As Double,
                                                     ionic_strength_M As Double,
                                                     T_in_K As Double) As Double?
            If Me.Microspecies Is Nothing OrElse Me.Microspecies.Count = 0 Then
                Return Nothing
            End If

            ' 计算每个微物种的变换 ddg_over_rt
            Dim ddg_over_rt(Me.Microspecies.Count - 1) As Double
            For i As Integer = 0 To Me.Microspecies.Count - 1
                ddg_over_rt(i) = Me.Microspecies(i).TransformedDdgOverRt(
                pH, pMg, ionic_strength_M, T_in_K)
            Next

            ' 使用 log-sum-exp 计算变换 Gibbs 自由能
            Dim result As Object = Numpy.LogSumExp(ddg_over_rt)
            Dim total_ddg_over_rt As Double = CDbl(result)

            ' 计算变换标准生成 Gibbs 自由能
            Dim RT As Double = ThermodynamicConstants.default_R_in_kJ_per_mol_per_K * T_in_K
            Dim formation As Double? = Me.StandardDgFormation
            If formation.HasValue Then
                Return formation.Value + total_ddg_over_rt * RT
            Else
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' 获取此化合物的 pKa 值列表。
        ''' 等价于 Python pka(self, T_in_K)。
        ''' 
        ''' pKa 值从解离常数计算得到。解离常数存储在数据库中，
        ''' 通过 Pickle 反序列化获取。
        ''' </summary>
        ''' <param name="T_in_K">温度（开尔文），默认 298.15 K</param>
        ''' <returns>pKa 值列表，如果无数据则返回空列表</returns>
        Public Function Pka(Optional T_in_K As Double = 298.15) As List(Of Double)
            Dim dissConsts As List(Of Double) = Me.DissociationConstants
            If dissConsts Is Nothing Then
                Return New List(Of Double)()
            End If

            Dim pkaValues As New List(Of Double)()
            For Each dc As Double In dissConsts
                pkaValues.Add(-Math.Log10(dc))
            Next
            Return pkaValues
        End Function

        ''' <summary>
        ''' 标识符排序键。
        ''' 等价于 Python _identifier_sorting_key(identifier)。
        ''' 根据 ORDER_OF_REGISTRIES 中的优先级对标识符进行排序。
        ''' </summary>
        Private Shared Function IdentifierSortingKey(identifier As CompoundIdentifier) As (Integer, Integer)
            Try
                Dim priority As Integer = Array.IndexOf(ORDER_OF_REGISTRIES, identifier.Registry.Identifier)
                If priority < 0 Then
                    Return (Integer.MaxValue, Integer.MaxValue)
                End If

                ' 尝试从访问号中提取数字值
                Dim numbers As MatchCollection = Regex.Matches(identifier.Accession, "\d+")
                If numbers.Count > 0 Then
                    Return (priority, Integer.Parse(numbers(0).Value))
                Else
                    ' 如果没有数字，使用访问号字符串长度
                    Return (priority, identifier.Accession.Length)
                End If
            Catch
                Return (Integer.MaxValue, Integer.MaxValue)
            End Try
        End Function

        ''' <summary>
        ''' 获取此化合物的最佳访问号。
        ''' 等价于 Python get_accession(self)。
        ''' 根据 ORDER_OF_REGISTRIES 优先级选择最佳标识符。
        ''' </summary>
        ''' <returns>格式为 "namespace:accession" 的字符串，如果无标识符则返回 Nothing</returns>
        Public Function GetAccession() As String
            Try
                Dim bestId As CompoundIdentifier = Nothing
                Dim bestKey As (Integer, Integer) = (Integer.MaxValue, Integer.MaxValue)

                For Each id As CompoundIdentifier In Me.Identifiers
                    Dim key As (Integer, Integer) = IdentifierSortingKey(id)
                    If key.Item1 < bestKey.Item1 OrElse
                   (key.Item1 = bestKey.Item1 AndAlso key.Item2 < bestKey.Item2) Then
                        bestKey = key
                        bestId = id
                    End If
                Next

                If bestId IsNot Nothing AndAlso bestId.Registry IsNot Nothing Then
                    Return bestId.Registry.Namespace & ":" & bestId.Accession
                End If
                Return Nothing
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' 获取化合物的分子式字符串。
        ''' 等价于 Python @property formula。
        ''' 从原子袋（atom_bag）中生成分子式字符串。
        ''' </summary>
        Public ReadOnly Property Formula As String
            Get
                Dim bag As Dictionary(Of String, Integer) = AtomBag
                If bag Is Nothing Then Return Nothing

                Dim parts As New List(Of String)()
                For Each kvp In From item In bag Order By item.Key
                    If kvp.Value > 0 AndAlso kvp.Key <> "e-" Then
                        If kvp.Value = 1 Then
                            parts.Add(kvp.Key)
                        Else
                            parts.Add($"{kvp.Key}{kvp.Value}")
                        End If
                    End If
                Next
                Return String.Join("", parts)
            End Get
        End Property

        ''' <summary>
        ''' 检查此化合物是否可以进行 Legendre 变换。
        ''' 等价于 Python can_be_transformed()。
        ''' 即是否已被 ChemAxon 分析并填充了微物种数据。
        ''' </summary>
        ''' <returns>如果化合物可以进行变换则返回 True</returns>
        Public Function CanBeTransformed() As Boolean
            Return Microspecies IsNot Nothing AndAlso
               Microspecies.Count > 0 AndAlso
               Not Microspecies.Any(Function(ms) ms Is Nothing)
        End Function

        ''' <summary>
        ''' 获取所有微物种的变换 ddg_over_rt 值。
        ''' 等价于 Python _get_ms_ddg_over_rt(p_h, ionic_strength, temperature, p_mg)。
        ''' </summary>
        Private Function GetMsDdgOverRt(pH As Quantity, ionicStrength As Quantity,
                                     temperature As Quantity,
                                     Optional pMg As Quantity? = Nothing) As Double()
            If Not CanBeTransformed() Then
                Throw New MissingDissociationConstantsException(
                $"{Me} 尚未被 ChemAxon 分析。")
            End If

            If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

            Dim pHVal As Double = pH.MAs("")
            Dim pMgVal As Double = pMg.Value.MAs("")
            Dim ionicStrengthM As Double = ionicStrength.MAs("M")
            Dim TInK As Double = temperature.MAs("K")

            Dim result(Microspecies.Count - 1) As Double
            For i As Integer = 0 To Microspecies.Count - 1
                result(i) = -Microspecies(i).TransformedDdgOverRt(
                pHVal, pMgVal, ionicStrengthM, TInK)
            Next
            Return result
        End Function

        ''' <summary>
        ''' 计算化合物的 Legendre 变换。
        ''' 等价于 Python transform(p_h, ionic_strength, temperature, p_mg)。
        '''
        ''' 计算化合物在指定 pH、离子强度、温度和 pMg 条件下的
        ''' 变换相对 Gibbs 自由能。使用 logaddexp 避免浮点溢出。
        ''' </summary>
        ''' <param name="pH">pH 值</param>
        ''' <param name="ionicStrength">离子强度</param>
        ''' <param name="temperature">温度</param>
        ''' <param name="pMg">pMg 值（可选，默认 14.0）</param>
        ''' <returns>变换后的相对 Gibbs 自由能（Quantity）</returns>
        Public Function Transform(pH As Quantity, ionicStrength As Quantity,
                               temperature As Quantity,
                               Optional pMg As Quantity? = Nothing) As Quantity
            If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

            Dim ddgOverRt As Double() = GetMsDdgOverRt(pH, ionicStrength, temperature, pMg)

            ' 使用 logaddexp 逐步归并，等价于 Python reduce(np.logaddexp, ...)
            Dim result As Double = ddgOverRt(0)
            For i As Integer = 1 To ddgOverRt.Length - 1
                result = Numpy.LogAddExp(result, ddgOverRt(i))
            Next

            Return -ThermodynamicConstants.R * temperature * New Quantity(result, "")
        End Function

        ''' <summary>
        ''' 计算化合物的 ΔG' 对 pH 的灵敏度。
        ''' 等价于 Python sensitivity_to_p_h(p_h, ionic_strength, temperature, p_mg)。
        '''
        ''' 通过对 Legendre 变换关于 pH 求导，得到 pH 响应的线性近似斜率。
        ''' 推导过程：
        '''   T = -ln(sum[exp(-g_i)]) = -lse[-g_i]
        '''   dT/dpH = -sum[(-dg_i/dpH) * exp(-g_i)] / sum[exp(-g_i)]
        '''          = exp(lse[-g_i + ln(dg_i/dpH)] - lse[-g_i])
        ''' 其中 dg_i/dpH = N_H_i * ln(10)
        ''' </summary>
        ''' <param name="pH">pH 值</param>
        ''' <param name="ionicStrength">离子强度</param>
        ''' <param name="temperature">温度</param>
        ''' <param name="pMg">pMg 值（可选）</param>
        ''' <returns>ΔG' 对 pH 的导数（Quantity）</returns>
        Public Function SensitivityToPH(pH As Quantity, ionicStrength As Quantity,
                                     temperature As Quantity,
                                     Optional pMg As Quantity? = Nothing) As Quantity
            If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

            Dim msDdgOverRt As Double() = GetMsDdgOverRt(pH, ionicStrength, temperature, pMg)

            ' 权重 = N_H_i * ln(10)
            Dim weights(msDdgOverRt.Length - 1) As Double
            For i As Integer = 0 To Microspecies.Count - 1
                weights(i) = Microspecies(i).NumberProtons * ThermodynamicConstants.LOG10
            Next

            ' 未加权和
            Dim unweightedSum As Double = CDbl(Numpy.LogSumExp(msDdgOverRt))
            ' 加权和
            Dim weightedSum As Double = CDbl(Numpy.LogSumExp(msDdgOverRt, weights))

            Return ThermodynamicConstants.R * temperature * New Quantity(Math.Exp(weightedSum - unweightedSum), "")
        End Function

        ''' <summary>
        ''' 计算化合物的 ΔG' 对离子强度的灵敏度。
        ''' 等价于 Python sensitivity_to_I(p_h, ionic_strength, temperature, p_mg)。
        '''
        ''' 推导过程类似 sensitivity_to_p_h：
        '''   dT/dI = exp(lse[-g_i + ln(dg_i/dI)] - lse[-g_i])
        ''' 其中 dg_i/dI = dDH/dI * (z_i^2 - N_H_i - 4*N_Mg_i)
        ''' 由于括号内表达式可能为负，需要使用 return_sign=True。
        ''' </summary>
        ''' <param name="pH">pH 值</param>
        ''' <param name="ionicStrength">离子强度</param>
        ''' <param name="temperature">温度</param>
        ''' <param name="pMg">pMg 值（可选）</param>
        ''' <returns>ΔG' 对离子强度的导数（Quantity）</returns>
        Public Function SensitivityToI(pH As Quantity, ionicStrength As Quantity,
                                    temperature As Quantity,
                                    Optional pMg As Quantity? = Nothing) As Quantity
            If pMg Is Nothing Then pMg = ThermodynamicConstants.default_pMg_Quantity

            Dim msDdgOverRt As Double() = GetMsDdgOverRt(pH, ionicStrength, temperature, pMg)

            ' 权重 = (z_i^2 - N_H_i - 4*N_Mg_i) * dDH/dI
            Dim dhDeriv As Double = ThermodynamicConstants.DebyeHueckelDerivative(
            ionicStrength.MAs("M"), temperature.MAs("K"))
            Dim weights(msDdgOverRt.Length - 1) As Double
            For i As Integer = 0 To Microspecies.Count - 1
                weights(i) = (Microspecies(i).Charge ^ 2 -
                          Microspecies(i).NumberProtons -
                          4 * Microspecies(i).NumberMagnesiums) * dhDeriv
            Next

            ' 未加权和
            Dim unweightedSum As Double = CDbl(Numpy.LogSumExp(msDdgOverRt))
            ' 加权和（带符号）
            Dim resultArr As Double() = CType(Numpy.LogSumExp(msDdgOverRt, weights, returnSign:=True), Double())
            Dim weightedSum As Double = resultArr(0)
            Dim sign As Double = resultArr(1)

            Return -sign * New Quantity(Math.Exp(weightedSum - unweightedSum), "") * New Quantity(1.0, "kJ/mol/molar")
        End Function

        ''' <summary>
        ''' 获取此化合物的常用名称。
        ''' 等价于 Python get_common_name()。
        ''' 从标识符列表中选择 "Synonyms" 注册表中 ID 最小的同义词。
        ''' </summary>
        ''' <returns>化合物名称，如果无同义词则返回 Nothing</returns>
        Public Function GetCommonName() As String
            Dim bestPriority As Double = Double.PositiveInfinity
            Dim synonym As String = Nothing

            For Each identifier In Identifiers
                If identifier.Registry IsNot Nothing AndAlso
               identifier.Registry.Name = "Synonyms" Then
                    Dim priority As Double = identifier.Id
                    If priority < bestPriority Then
                        synonym = identifier.Accession.Split("|"c)(0)
                        bestPriority = priority
                    End If
                End If
            Next

            Return synonym
        End Function

        ''' <summary>
        ''' 比较两个化合物是否相等（基于 ID）。
        ''' </summary>
        Public Overrides Function Equals(obj As Object) As Boolean
            If TypeOf obj Is Compound Then
                Return Me.Id = DirectCast(obj, Compound).Id
            End If
            Return False
        End Function

        ''' <summary>获取哈希码</summary>
        Public Overrides Function GetHashCode() As Integer
            Return Me.Id.GetHashCode()
        End Function

        ''' <summary>比较运算符</summary>
        Public Shared Operator =(a As Compound, b As Compound) As Boolean
            If a Is Nothing AndAlso b Is Nothing Then Return True
            If a Is Nothing OrElse b Is Nothing Then Return False
            Return a.Id = b.Id
        End Operator

        ''' <summary>不等运算符</summary>
        Public Shared Operator <>(a As Compound, b As Compound) As Boolean
            Return Not (a = b)
        End Operator

        ''' <summary>小于运算符（基于 ID）</summary>
        Public Shared Operator <(a As Compound, b As Compound) As Boolean
            If a Is Nothing Then Return b IsNot Nothing
            If b Is Nothing Then Return False
            Return a.Id < b.Id
        End Operator

        ''' <summary>大于运算符（基于 ID）</summary>
        Public Shared Operator >(a As Compound, b As Compound) As Boolean
            If a Is Nothing Then Return False
            If b Is Nothing Then Return a IsNot Nothing
            Return a.Id > b.Id
        End Operator

    End Class
End Namespace