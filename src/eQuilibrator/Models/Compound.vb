Imports System.Text.RegularExpressions
Imports System.Collections
Imports Microsoft.VisualBasic.Data.IO.Pickle
Imports eQuilibrator.EquilibratorApi.Core.Constants

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' 统一的生化化合物模型（合并 EquilibratorApi.Core 与 EquilibratorCache 两套重复定义）。
    ''' 包含结构信息、微物种、基团向量（组贡献法）、镁解离常数与标识符，
    ''' 支持从 eQuilibrator CSV 数据源按需加载。
    ''' </summary>
    Public Class Compound

        ' =========================================================================
        ' 身份与结构
        ' =========================================================================

        ''' <summary>用户可见/检索键（accession，如 "kegg:C00002"，或 CSV 主键字符串）。</summary>
        Public Property Id As String = String.Empty

        ''' <summary>CSV 主键（compounds.csv 的 integer id），用于加载 microspecies / Mg 常数 / identifiers。</summary>
        Public Property CsvId As Integer = -1

        ''' <summary>常用名称（CSV 无此列，通常由标识符推导）。</summary>
        Public Property Name As String

        ''' <summary>InChI 标识符。</summary>
        Public Property InChI As String

        ''' <summary>InChI Key。</summary>
        Public Property InChIKey As String

        ''' <summary>SMILES 描述符。</summary>
        Public Property SMILES As String

        ''' <summary>分子量（g/mol）。</summary>
        Public Property MolecularWeight As Double?

        ''' <summary>电荷数。</summary>
        Public Property Charge As Integer

        ''' <summary>质子数。</summary>
        Public Property ProtonCount As Integer

        ''' <summary>原子袋（原子组成字典）。</summary>
        Public Property AtomBag As Dictionary(Of String, Integer)

        ' =========================================================================
        ' 热力学量（近似/占位；CSV 暂无标准 ΔfG°，可由组贡献法估算）
        ' =========================================================================

        ''' <summary>标准生成 Gibbs 自由能（kJ/mol，近似或组贡献估算值）。</summary>
        Public Property StandardFormationEnergy As Double

        ''' <summary>标准 Gibbs 自由能（kJ/mol，框架预留）。</summary>
        Public Property StandardDg As Double?

        ''' <summary>标准生成 Gibbs 自由能（kJ/mol，框架预留，CSV 暂无）。</summary>
        Public Property StandardDgFormation As Double?

        ' =========================================================================
        ' CSV 原始（base64 pickle）字段
        ' =========================================================================

        ''' <summary>原子袋（base64 编码的 pickle）。</summary>
        Public Property AtomBagBase64 As String

        ''' <summary>解离常数（pKa 列表，base64 编码的 pickle）。</summary>
        Public Property DissociationConstantsBase64 As String

        ''' <summary>基团向量（组贡献法，base64 编码的 pickle 的 numpy 数组）。</summary>
        Public Property GroupVectorBase64 As String

        ' =========================================================================
        ' 反序列化后的运行期字段
        ' =========================================================================

        ''' <summary>基团贡献向量（组贡献法）。</summary>
        Public Property GroupVector As Double()

        ''' <summary>解离常数（pKa 列表）。</summary>
        Public Property DissociationConstants As List(Of Double)

        ' =========================================================================
        ' 关联数据
        ' =========================================================================

        ''' <summary>微物种列表（组件贡献法 / Legendre 变换）。</summary>
        Public Property Microspecies As List(Of CompoundMicrospecies) = New List(Of CompoundMicrospecies)()

        ''' <summary>镁离子解离常数列表。</summary>
        Public Property MagnesiumDissociationConstants As List(Of MagnesiumDissociationConstant) = New List(Of MagnesiumDissociationConstant)()

        ''' <summary>标识符列表（KEGG / BiGG / ChEBI 等）。</summary>
        Public Property Identifiers As List(Of CompoundIdentifier) = New List(Of CompoundIdentifier)()

        ' =========================================================================
        ' 特殊化合物标记
        ' =========================================================================

        ''' <summary>是否为质子（H+）。</summary>
        Public Property IsProton As Boolean

        ''' <summary>是否为水（H2O）。</summary>
        Public Property IsWater As Boolean

        ' =========================================================================
        ' 注册表优先级（用于选取最佳 accession）
        ' =========================================================================

        ''' <summary>标识符排序的注册表优先级顺序（最可信 → 最不可信）。</summary>
        Public Shared ReadOnly ORDER_OF_REGISTRIES As String() = {
            "MIR:00000567", "MIR:00000578", "MIR:00000556", "MIR:00000002", "MIR:00000552"
        }

        Sub New()
        End Sub

        Sub New(id As String, csvId As Integer,
                Optional atomBag As Dictionary(Of String, Integer) = Nothing,
                Optional groupVector As Double() = Nothing)
            Me.Id = id
            Me.CsvId = csvId
            Me.AtomBag = atomBag
            Me.GroupVector = groupVector
        End Sub

        ' =========================================================================
        ' pickle 反序列化辅助（替代被删除的 EquilibratorCache 运行期逻辑）
        ' =========================================================================

        ''' <summary>将 base64 pickle（Python dict）反序列化为原子袋字典。</summary>
        ''' <remarks>eQuilibrator 以 pickle 协议 4 存储为 Python dict（如 {"H": 1}），
        ''' 反序列化后由 sciBASIC# 表现为 Dictionary(Of Object, Object)。</remarks>
        Public Shared Function UnpickleAtomBag(base64 As String) As Dictionary(Of String, Integer)
            If String.IsNullOrEmpty(base64) Then Return Nothing
            Try
                Dim obj As Object = MinimalPickleUnpickler.Unpickle(base64)
                If TypeOf obj Is IDictionary Then
                    Dim d = DirectCast(obj, IDictionary)
                    Dim r = New Dictionary(Of String, Integer)()
                    For Each e As DictionaryEntry In d
                        r(CStr(e.Key)) = Convert.ToInt32(e.Value)
                    Next
                    Return r
                End If
            Catch
            End Try
            Return Nothing
        End Function

        ''' <summary>将 base64 pickle（Python list/tuple of int）反序列化为基团向量（Double()）。</summary>
        ''' <remarks>eQuilibrator 以 pickle 协议 4 存储为 Python list（如 [0,1,0,3,...]），
        ''' 反序列化后由 sciBASIC# 表现为 List(Of Object) 或 PythonTuple。</remarks>
        Public Shared Function UnpickleGroupVector(base64 As String) As Double()
            If String.IsNullOrEmpty(base64) Then Return Nothing
            Try
                Dim obj As Object = MinimalPickleUnpickler.Unpickle(base64)
                If TypeOf obj Is IDictionary Then Return Nothing
                If TypeOf obj Is IEnumerable AndAlso Not (TypeOf obj Is String) Then
                    Dim list = New List(Of Double)()
                    For Each e In DirectCast(obj, IEnumerable)
                        list.Add(Convert.ToDouble(e))
                    Next
                    Return list.ToArray()
                End If
            Catch
            End Try
            Return Nothing
        End Function

        ''' <summary>将 base64 pickle（Python list of float）反序列化为解离常数（pKa）列表。</summary>
        Public Shared Function UnpickleDissociationConstants(base64 As String) As List(Of Double)
            If String.IsNullOrEmpty(base64) Then Return Nothing
            Try
                Dim obj As Object = MinimalPickleUnpickler.Unpickle(base64)
                If TypeOf obj Is IDictionary Then Return Nothing
                If TypeOf obj Is IEnumerable AndAlso Not (TypeOf obj Is String) Then
                    Dim list = New List(Of Double)()
                    For Each e In DirectCast(obj, IEnumerable)
                        list.Add(Convert.ToDouble(e))
                    Next
                    Return list
                End If
            Catch
            End Try
            Return Nothing
        End Function

        ' =========================================================================
        ' 方法
        ' =========================================================================

        ''' <summary>创建带相信息的 PhasedCompound。</summary>
        Public Function ToPhasedCompound(Optional phase As String = ThermodynamicConstants.DefaultPhase) As PhasedCompound
            Return New PhasedCompound(Id, phase) With {
                .Name = Name,
                .InChIKey = InChIKey,
                .InChI = InChI,
                .SMILES = SMILES,
                .MolecularWeight = If(MolecularWeight.HasValue, MolecularWeight.Value, 0.0),
                .Charge = Charge,
                .ProtonCount = ProtonCount,
                .AtomBag = If(AtomBag IsNot Nothing, New Dictionary(Of String, Integer)(AtomBag), New Dictionary(Of String, Integer)()),
                .StandardFormationEnergy = StandardFormationEnergy,
                .IsProton = IsProton,
                .IsWater = IsWater
            }
        End Function

        ''' <summary>获取 pKa 值列表（由解离常数计算）。</summary>
        Public Function Pka(Optional T_in_K As Double = 298.15) As List(Of Double)
            If DissociationConstants Is Nothing Then Return New List(Of Double)()
            Dim r = New List(Of Double)()
            For Each dc In DissociationConstants
                r.Add(-Math.Log10(dc))
            Next
            Return r
        End Function

        ''' <summary>化合物是否可进行 Legendre 变换（已填充微物种）。</summary>
        Public Function CanBeTransformed() As Boolean
            Return Microspecies IsNot Nothing AndAlso
                   Microspecies.Count > 0 AndAlso
                   Not Microspecies.Any(Function(ms) ms Is Nothing)
        End Function

        ''' <summary>从原子袋生成分子式字符串。</summary>
        Public ReadOnly Property Formula As String
            Get
                If AtomBag Is Nothing Then Return Nothing
                Dim parts = New List(Of String)()
                For Each kvp In From item In AtomBag Order By item.Key
                    If kvp.Value > 0 AndAlso kvp.Key <> "e-" Then
                        parts.Add(If(kvp.Value = 1, kvp.Key, $"{kvp.Key}{kvp.Value}"))
                    End If
                Next
                Return String.Join("", parts)
            End Get
        End Property

        ''' <summary>获取最佳访问号（按 ORDER_OF_REGISTRIES 优先级）。</summary>
        Public Function GetAccession() As String
            Try
                Dim bestId As CompoundIdentifier = Nothing
                Dim bestKey As (Integer, Integer) = (Integer.MaxValue, Integer.MaxValue)

                For Each ident In Identifiers
                    Dim key = IdentifierSortingKey(ident)
                    If key.Item1 < bestKey.Item1 OrElse
                       (key.Item1 = bestKey.Item1 AndAlso key.Item2 < bestKey.Item2) Then
                        bestKey = key
                        bestId = ident
                    End If
                Next

                If bestId IsNot Nothing AndAlso bestId.Registry IsNot Nothing Then
                    Return bestId.Registry.[Namespace] & ":" & bestId.Accession
                End If
                Return Nothing
            Catch
                Return Nothing
            End Try
        End Function

        ''' <summary>获取化合物的常用名称（Synonyms 注册表中的同义词）。</summary>
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

        Private Shared Function IdentifierSortingKey(identifier As CompoundIdentifier) As (Integer, Integer)
            Try
                Dim priority As Integer = Array.IndexOf(ORDER_OF_REGISTRIES, identifier.Registry?.Identifier)
                If priority < 0 Then Return (Integer.MaxValue, Integer.MaxValue)

                Dim numbers As MatchCollection = Regex.Matches(identifier.Accession, "\d+")
                If numbers.Count > 0 Then
                    Return (priority, Integer.Parse(numbers(0).Value))
                End If
                Return (priority, identifier.Accession.Length)
            Catch
                Return (Integer.MaxValue, Integer.MaxValue)
            End Try
        End Function

        Public Overrides Function ToString() As String
            Return $"Compound(id={Id}, name={Name})"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other = TryCast(obj, Compound)
            Return other IsNot Nothing AndAlso
                   String.Equals(Id, other.Id, StringComparison.OrdinalIgnoreCase)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return If(Id, "").GetHashCode()
        End Function
    End Class
End Namespace
