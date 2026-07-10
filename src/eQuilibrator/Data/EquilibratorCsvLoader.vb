Imports System.IO
Imports System.Text
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorThermodynamics

Namespace EquilibratorApi.Core.Data

    ''' <summary>
    ''' eQuilibrator CSV 数据源加载器。
    ''' 首遍扫描构建「compound_id → 字节偏移」索引，之后按化合物 ID 懒加载并解析对应行，
    ''' 反序列化 base64 编码的 pickle 字段（atom_bag / dissociation_constants / group_vector），
    ''' 避免将超大 CSV 整文件驻留内存。
    ''' </summary>
    Public Class EquilibratorCsvLoader

        ' CSV 文件路径
        Private ReadOnly _compoundsFile As String
        Private ReadOnly _microspeciesFile As String
        Private ReadOnly _mgFile As String
        Private ReadOnly _identifiersFile As String
        Private ReadOnly _registriesFile As String

        ' 索引：compound_id -> 字节偏移
        Private ReadOnly _compoundOffset As New Dictionary(Of Integer, Long)()
        ' 索引：compound_id -> 微物种行偏移列表
        Private ReadOnly _microspeciesOffsets As New Dictionary(Of Integer, List(Of Long))()
        ' 索引：compound_id -> Mg 解离常数行偏移列表
        Private ReadOnly _mgOffsets As New Dictionary(Of Integer, List(Of Long))()
        ' 索引：注册表 id -> Registry
        Private ReadOnly _registries As New Dictionary(Of Integer, Registry)()
        ' 索引：accession（含 "prefix:accession" 与完整 namespace 形式） -> compound_id
        Private ReadOnly _accessionIndex As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        ' 反向：compound_id -> (accession, registry_id) 列表
        Private ReadOnly _accessionsByCompound As New Dictionary(Of Integer, List(Of AccessionRef))()

        Private Structure AccessionRef
            Public Accession As String
            Public RegistryId As Integer
        End Structure

        ''' <summary>
        ''' 使用包含 eQuilibrator CSV 文件的目录初始化加载器（首次访问时构建索引）。
        ''' </summary>
        ''' <param name="csvDirectory">包含 compounds.csv 等文件的目录。</param>
        Public Sub New(csvDirectory As String)
            _compoundsFile = Path.Combine(csvDirectory, "compounds.csv")
            _microspeciesFile = Path.Combine(csvDirectory, "compound_microspecies.csv")
            _mgFile = Path.Combine(csvDirectory, "magnesium_dissociation_constant.csv")
            _identifiersFile = Path.Combine(csvDirectory, "compound_identifiers.csv")
            _registriesFile = Path.Combine(csvDirectory, "registries.csv")

            BuildRegistries()
            BuildCompoundIndex()
            BuildMicrospeciesIndex()
            BuildMagnesiumIndex()
            BuildAccessionIndex()
        End Sub

        ' =====================================================================
        ' 索引构建
        ' =====================================================================

        Private Sub BuildRegistries()
            If Not File.Exists(_registriesFile) Then Return
            ' created_on,updated_on,id,name,namespace,pattern,identifier,url,is_prefixed,access_url
            ForEachDataRow(_registriesFile,
                Sub(fields, offset)
                    If fields.Length < 7 Then Return
                    Dim id = SafeInt(fields(2))
                    Dim reg = New Registry(fields(4), fields(5), fields(3), fields(6), If(fields.Length > 7, fields(7), Nothing))
                    _registries(id) = reg
                End Sub)
        End Sub

        Private Sub BuildCompoundIndex()
            If Not File.Exists(_compoundsFile) Then Return
            ' created_on,updated_on,id,inchi_key,inchi,smiles,mass,atom_bag,dissociation_constants,group_vector
            ForEachDataRow(_compoundsFile,
                Sub(fields, offset)
                    If fields.Length < 3 Then Return
                    Dim id = SafeInt(fields(2))
                    If id >= 0 Then _compoundOffset(id) = offset
                End Sub)
        End Sub

        Private Sub BuildMicrospeciesIndex()
            If Not File.Exists(_microspeciesFile) Then Return
            ' created_on,updated_on,id,compound_id,charge,number_protons,number_magnesiums,is_major,ddg_over_rt
            ForEachDataRow(_microspeciesFile,
                Sub(fields, offset)
                    If fields.Length < 4 Then Return
                    Dim cid = SafeInt(fields(3))
                    If cid < 0 Then Return
                    If Not _microspeciesOffsets.ContainsKey(cid) Then
                        _microspeciesOffsets(cid) = New List(Of Long)()
                    End If
                    _microspeciesOffsets(cid).Add(offset)
                End Sub)
        End Sub

        Private Sub BuildMagnesiumIndex()
            If Not File.Exists(_mgFile) Then Return
            ' created_on,updated_on,id,compound_id,number_protons,number_magnesiums,dissociation_constant
            ForEachDataRow(_mgFile,
                Sub(fields, offset)
                    If fields.Length < 4 Then Return
                    Dim cid = SafeInt(fields(3))
                    If cid < 0 Then Return
                    If Not _mgOffsets.ContainsKey(cid) Then
                        _mgOffsets(cid) = New List(Of Long)()
                    End If
                    _mgOffsets(cid).Add(offset)
                End Sub)
        End Sub

        Private Sub BuildAccessionIndex()
            If Not File.Exists(_identifiersFile) Then Return
            ' created_on,updated_on,id,compound_id,registry_id,accession
            ForEachDataRow(_identifiersFile,
                Sub(fields, offset)
                    If fields.Length < 6 Then Return
                    Dim cid = SafeInt(fields(3))
                    Dim regId = SafeInt(fields(4))
                    Dim accession = fields(5)
                    If cid < 0 OrElse String.IsNullOrEmpty(accession) Then Return

                    If Not _accessionsByCompound.ContainsKey(cid) Then
                        _accessionsByCompound(cid) = New List(Of AccessionRef)()
                    End If
                    _accessionsByCompound(cid).Add(New AccessionRef With {.Accession = accession, .RegistryId = regId})

                    ' 直接 accession
                    AddAccessionKey(accession, cid)
                    ' 短前缀形式（如 "kegg:C00002"）
                    Dim reg = If(_registries.ContainsKey(regId), _registries(regId), Nothing)
                    If reg IsNot Nothing Then
                        Dim prefix = reg.Namespace
                        Dim dot = prefix.IndexOf("."c)
                        If dot >= 0 Then prefix = prefix.Substring(0, dot)
                        AddAccessionKey(prefix & ":" & accession, cid)
                        AddAccessionKey(reg.Namespace & ":" & accession, cid)
                    End If
                End Sub)
        End Sub

        Private Sub AddAccessionKey(key As String, csvId As Integer)
            If Not _accessionIndex.ContainsKey(key) Then
                _accessionIndex(key) = csvId
            End If
        End Sub

        ' =====================================================================
        ' 行读取（字节偏移索引 + CSV 解析）
        ' =====================================================================

        ''' <summary>
        ''' 逐行读取 CSV（流式，低内存），对每一逻辑记录调用回调（含起始字节偏移）。
        ''' 引号感知：仅将「不在引号内」的 LF 视为记录分隔符，从而正确处理带内嵌换行的引用字段。
        ''' </summary>
        Private Shared Sub ForEachDataRow(path As String, callback As Action(Of String(), Long))
            Using fs = New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                Dim buffer(1024 * 1024 - 1) As Byte
                Dim lineBuffer As New MemoryStream()
                Dim lineStart As Long = 0
                Dim bytesRead As Integer
                Dim firstLine As Boolean = True
                Dim inQuotes As Boolean = False

                bytesRead = fs.Read(buffer, 0, buffer.Length)
                While bytesRead > 0
                    For i = 0 To bytesRead - 1
                        Dim b = buffer(i)
                        If b = 34 Then ' 双引号：切换引号状态
                            inQuotes = Not inQuotes
                            lineBuffer.WriteByte(b)
                        ElseIf b = 10 AndAlso Not inQuotes Then ' 引号外的 LF = 记录结束
                            Dim line = Encoding.UTF8.GetString(lineBuffer.ToArray())
                            lineBuffer.SetLength(0)
                            If firstLine Then
                                firstLine = False
                            Else
                                callback(ParseCsvLine(line), lineStart)
                            End If
                            lineStart = fs.Position - bytesRead + (i + 1)
                        ElseIf b = 13 AndAlso Not inQuotes Then
                            ' 忽略引号外的 CR
                        Else
                            lineBuffer.WriteByte(b)
                        End If
                    Next
                    bytesRead = fs.Read(buffer, 0, buffer.Length)
                End While

                If lineBuffer.Length > 0 AndAlso Not firstLine Then
                    callback(ParseCsvLine(Encoding.UTF8.GetString(lineBuffer.ToArray())), lineStart)
                End If
            End Using
        End Sub

        ''' <summary>
        ''' 读取指定字节偏移处的完整逻辑记录并解析为字段（引号感知，支持内嵌换行）。
        ''' </summary>
        Private Function ReadRowAt(path As String, offset As Long) As String()
            Using fs = New FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)
                fs.Seek(offset, SeekOrigin.Begin)
                Dim lineBuffer As New MemoryStream()
                Dim inQuotes As Boolean = False
                Dim buffer(65535) As Byte
                Dim done As Boolean = False
                While Not done
                    Dim n = fs.Read(buffer, 0, buffer.Length)
                    If n <= 0 Then Exit While
                    For i = 0 To n - 1
                        Dim b = buffer(i)
                        If b = 34 Then
                            inQuotes = Not inQuotes
                            lineBuffer.WriteByte(b)
                        ElseIf b = 10 AndAlso Not inQuotes Then
                            done = True
                            Exit For
                        ElseIf b = 13 AndAlso Not inQuotes Then
                            ' 忽略引号外的 CR
                        Else
                            lineBuffer.WriteByte(b)
                        End If
                    Next
                End While
                If lineBuffer.Length = 0 Then Return Nothing
                Return ParseCsvLine(Encoding.UTF8.GetString(lineBuffer.ToArray()))
            End Using
        End Function

        ''' <summary>
        ''' 解析一行 CSV，正确处理双引号包裹字段与转义引号（""）。
        ''' </summary>
        Public Shared Function ParseCsvLine(line As String) As String()
            Dim result As New List(Of String)()
            Dim sb As New StringBuilder()
            Dim i = 0
            Dim inQuotes = False

            While i < line.Length
                Dim c = line(i)
                If inQuotes Then
                    If c = """"c Then
                        If i + 1 < line.Length AndAlso line(i + 1) = """"c Then
                            sb.Append(""""c)
                            i += 2
                            Continue While
                        Else
                            inQuotes = False
                            i += 1
                        End If
                    Else
                        sb.Append(c)
                        i += 1
                    End If
                Else
                    If c = """"c Then
                        inQuotes = True
                        i += 1
                    ElseIf c = ","c Then
                        result.Add(sb.ToString())
                        sb.Clear()
                        i += 1
                    Else
                        sb.Append(c)
                        i += 1
                    End If
                End If
            End While

            result.Add(sb.ToString())
            Return result.ToArray()
        End Function

        Private Shared Function SafeInt(s As String) As Integer
            Dim v As Integer = -1
            If Integer.TryParse(s, v) Then Return v
            Return -1
        End Function

        ' =====================================================================
        ' 化合物解析
        ' =====================================================================

        Private Function ParseCompound(fields As String()) As Compound
            ' created_on,updated_on,id,inchi_key,inchi,smiles,mass,atom_bag,dissociation_constants,group_vector
            Dim c = New Compound()
            c.CsvId = SafeInt(fields(2))
            c.InChIKey = If(fields.Length > 3, fields(3), Nothing)
            c.InChI = If(fields.Length > 4, fields(4), Nothing)
            c.SMILES = If(fields.Length > 5, fields(5), Nothing)
            If fields.Length > 6 AndAlso Not String.IsNullOrEmpty(fields(6)) Then
                Dim m As Double = 0
                If Double.TryParse(fields(6), m) Then c.MolecularWeight = m
            End If
            If fields.Length > 7 Then c.AtomBagBase64 = fields(7)
            If fields.Length > 8 Then c.DissociationConstantsBase64 = fields(8)
            If fields.Length > 9 Then c.GroupVectorBase64 = fields(9)

            c.AtomBag = Compound.UnpickleAtomBag(c.AtomBagBase64)
            c.GroupVector = Compound.UnpickleGroupVector(c.GroupVectorBase64)
            c.DissociationConstants = Compound.UnpickleDissociationConstants(c.DissociationConstantsBase64)

            c.IsProton = String.Equals(c.InChIKey, ThermodynamicConstants.PROTON_INCHI_KEY, StringComparison.OrdinalIgnoreCase)
            c.IsWater = String.Equals(c.InChIKey, ThermodynamicConstants.WATER_INCHI_KEY, StringComparison.OrdinalIgnoreCase)
            Return c
        End Function

        Private Function ParseMicrospecies(fields As String()) As CompoundMicrospecies
            ' created_on,updated_on,id,compound_id,charge,number_protons,number_magnesiums,is_major,ddg_over_rt
            Dim ms = New CompoundMicrospecies()
            ms.Id = SafeInt(fields(2))
            ms.CompoundId = SafeInt(fields(3))
            Dim v As Integer
            If Integer.TryParse(fields(4), v) Then ms.Charge = v
            If Integer.TryParse(fields(5), v) Then ms.NumberProtons = v
            If Integer.TryParse(fields(6), v) Then ms.NumberMagnesiums = v
            ms.IsMajor = (fields(7) = "1" OrElse fields(7) = "True" OrElse fields(7) = "t")
            If fields.Length > 8 AndAlso Not String.IsNullOrEmpty(fields(8)) Then
                Dim d As Double = 0
                If Double.TryParse(fields(8), d) Then ms.DdgOverRt = d
            End If
            Return ms
        End Function

        Private Function ParseMagnesium(fields As String()) As MagnesiumDissociationConstant
            ' created_on,updated_on,id,compound_id,number_protons,number_magnesiums,dissociation_constant
            Dim mg = New MagnesiumDissociationConstant()
            mg.Id = SafeInt(fields(2))
            mg.CompoundId = SafeInt(fields(3))
            Dim v As Integer
            If Integer.TryParse(fields(4), v) Then mg.NumberProtons = v
            If Integer.TryParse(fields(5), v) Then mg.NumberMagnesiums = v
            Dim d As Double = 0
            If Double.TryParse(fields(6), d) Then mg.DissociationConstant = d
            Return mg
        End Function

        ' =====================================================================
        ' 公共查询
        ' =====================================================================

        ''' <summary>将用户提供的化合物标识解析为 CSV 主键。</summary>
        Private Function ResolveCsvId(id As String) As Integer
            If String.IsNullOrEmpty(id) Then Return -1
            Dim direct As Integer = -1
            If Integer.TryParse(id, direct) AndAlso direct >= 0 Then Return direct
            If _accessionIndex.ContainsKey(id) Then Return _accessionIndex(id)

            ' 尝试 "prefix:accession" 形式
            Dim colon = id.IndexOf(":"c)
            If colon > 0 Then
                Dim acc = id.Substring(colon + 1)
                If _accessionIndex.ContainsKey(acc) Then Return _accessionIndex(acc)
            End If
            Return -1
        End Function

        ''' <summary>按化合物标识获取化合物（懒加载并解析 microspecies / Mg / identifiers）。</summary>
        Public Function GetCompound(id As String) As Compound
            Dim csvId = ResolveCsvId(id)
            If csvId < 0 OrElse Not _compoundOffset.ContainsKey(csvId) Then Return Nothing

            Dim fields = ReadRowAt(_compoundsFile, _compoundOffset(csvId))
            If fields Is Nothing OrElse fields.Length < 3 Then Return Nothing

            Dim c = ParseCompound(fields)
            c.Id = id

            ' 微物种
            If _microspeciesOffsets.ContainsKey(csvId) Then
                For Each off In _microspeciesOffsets(csvId)
                    Dim mf = ReadRowAt(_microspeciesFile, off)
                    If mf IsNot Nothing AndAlso mf.Length >= 7 Then
                        c.Microspecies.Add(ParseMicrospecies(mf))
                    End If
                Next
            End If

            ' 镁解离常数
            If _mgOffsets.ContainsKey(csvId) Then
                For Each off In _mgOffsets(csvId)
                    Dim mgf = ReadRowAt(_mgFile, off)
                    If mgf IsNot Nothing AndAlso mgf.Length >= 7 Then
                        c.MagnesiumDissociationConstants.Add(ParseMagnesium(mgf))
                    End If
                Next
            End If

            ' 标识符
            If _accessionsByCompound.ContainsKey(csvId) Then
                For Each ar In _accessionsByCompound(csvId)
                    Dim ci = New CompoundIdentifier() With {
                        .CompoundId = csvId,
                        .RegistryId = ar.RegistryId,
                        .Accession = ar.Accession,
                        .Registry = If(_registries.ContainsKey(ar.RegistryId), _registries(ar.RegistryId), Nothing)
                    }
                    c.Identifiers.Add(ci)
                Next
                ' 使用最佳 accession 作为 Id（若当前 Id 不是易读形式）
                If c.Id = csvId.ToString() Then
                    Dim acc = c.GetAccession()
                    If acc IsNot Nothing Then c.Id = acc
                End If
            End If

            ' 估算标准生成能（近似；CSV 无此列，使用组贡献法占位）
            c.StandardFormationEnergy = EstimateStandardFormationEnergy(c)
            Return c
        End Function

        ''' <summary>按 CSV 主键获取化合物。</summary>
        Public Function GetCompoundById(csvId As Integer) As Compound
            If Not _compoundOffset.ContainsKey(csvId) Then Return Nothing
            Dim c = GetCompound(csvId.ToString())
            Return c
        End Function

        ''' <summary>按 accession 获取 CSV 主键（找不到返回 Nothing）。</summary>
        Public Function GetCompoundIdByAccession(accession As String) As Integer?
            Dim csvId = ResolveCsvId(accession)
            If csvId < 0 Then Return Nothing
            Return csvId
        End Function

        ''' <summary>按子串搜索匹配的 accession（用于按名称/标识检索）。</summary>
        Public Function SearchAccessions(query As String) As List(Of String)
            Dim q = query.ToLowerInvariant()
            Dim results = New List(Of String)()
            For Each kvp In _accessionIndex
                If kvp.Key.ToLowerInvariant().Contains(q) Then
                    results.Add(kvp.Key)
                End If
            Next
            Return results
        End Function

        ''' <summary>
        ''' 估算标准生成吉布斯自由能（组贡献法近似）。
        ''' 使用基团向量乘以内置占位基团能量；若基团向量不可用则回退到原子贡献。
        ''' </summary>
        Private Shared Function EstimateStandardFormationEnergy(c As Compound) As Double
            If c.GroupVector IsNot Nothing AndAlso c.GroupVector.Length > 0 Then
                Dim sum = 0.0
                For i = 0 To c.GroupVector.Length - 1
                    sum += c.GroupVector(i) * GroupContributionParameters.DefaultGroupEnergy(i + 1)
                Next
                Return sum
            End If
            If c.AtomBag IsNot Nothing Then
                Dim sum = 0.0
                Dim atomic As New Dictionary(Of String, Double) From {
                    {"C", 15.0}, {"H", 5.0}, {"O", -140.0}, {"N", 30.0}, {"S", -20.0}, {"P", -250.0}
                }
                For Each kvp In c.AtomBag
                    If atomic.ContainsKey(kvp.Key) Then sum += atomic(kvp.Key) * kvp.Value
                Next
                Return sum
            End If
            Return 0.0
        End Function
    End Class
End Namespace
