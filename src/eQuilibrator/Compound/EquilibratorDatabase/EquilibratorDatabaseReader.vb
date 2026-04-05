' ============================================================================
' Equilibrator SQLite 数据库读取器
' 用于从SQLite数据库读取化合物数据并反序列化为对象
' ============================================================================

Imports System.Data
Imports System.Text
Imports Microsoft.VisualBasic.Data.IO.ManagedSqlite.Core

Namespace EquilibratorThermodynamics

    ''' <summary>
    ''' SQLite数据库读取器
    ''' 用于读取equilibrator_api的化合物数据库
    ''' </summary>
    Public Class EquilibratorDatabaseReader : Implements IDisposable

        Private _connection As Sqlite3Database
        Private _disposed As Boolean = False

        ''' <summary>
        ''' 数据库文件路径
        ''' </summary>
        Public ReadOnly Property DatabasePath As String

        ''' <summary>
        ''' 构造函数
        ''' </summary>
        Public Sub New(databasePath As String)
            _DatabasePath = databasePath
            _connection = Sqlite3Database.OpenFile(databasePath, False)
        End Sub

        ''' <summary>
        ''' 读取所有化合物数据
        ''' </summary>
        Public Iterator Function ReadAllCompounds() As IEnumerable(Of Compound)
            'Dim compounds As New List(Of Compound)()

            'Open()

            '' 读取化合物主表
            'Dim compoundDict As New Dictionary(Of Integer, Compound)()
            'Using cmd As New SQLiteCommand("SELECT * FROM compounds", _connection)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim compound As New Compound With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .InChIKey = If(reader.IsDBNull(reader.GetOrdinal("inchi_key")), Nothing, reader.GetString(reader.GetOrdinal("inchi_key"))),
            '                .InChI = If(reader.IsDBNull(reader.GetOrdinal("inchi")), Nothing, reader.GetString(reader.GetOrdinal("inchi"))),
            '                .Smiles = If(reader.IsDBNull(reader.GetOrdinal("smiles")), Nothing, reader.GetString(reader.GetOrdinal("smiles"))),
            '                .Mass = If(reader.IsDBNull(reader.GetOrdinal("mass")), 0.0, reader.GetDouble(reader.GetOrdinal("mass"))),
            '                .Microspecies = New List(Of CompoundMicrospecies)(),
            '                .Identifiers = New List(Of CompoundIdentifier)(),
            '                .MagnesiumDissociationConstants = New List(Of MagnesiumDissociationConstant)()
            '            }

            '            ' 读取BLOB字段
            '            Dim atomBagIdx As Integer = reader.GetOrdinal("atom_bag")
            '            Dim dissocIdx As Integer = reader.GetOrdinal("dissociation_constants")
            '            Dim groupVecIdx As Integer = reader.GetOrdinal("group_vector")

            '            If Not reader.IsDBNull(atomBagIdx) Then
            '                compound.AtomBagRaw = DirectCast(reader(atomBagIdx), Byte())
            '            End If

            '            If Not reader.IsDBNull(dissocIdx) Then
            '                compound.DissociationConstantsRaw = DirectCast(reader(dissocIdx), Byte())
            '            End If

            '            If Not reader.IsDBNull(groupVecIdx) Then
            '                compound.GroupVectorRaw = DirectCast(reader(groupVecIdx), Byte())
            '            End If

            '            ' 解析BLOB数据
            '            If compound.AtomBagRaw IsNot Nothing Then
            '                compound.AtomBag = EquilibratorBlobParser.ParseAtomBag(compound.AtomBagRaw)
            '            End If
            '            If compound.GroupVectorRaw IsNot Nothing Then
            '                compound.GroupVector = EquilibratorBlobParser.ParseGroupVector(compound.GroupVectorRaw)
            '            End If
            '            If compound.DissociationConstantsRaw IsNot Nothing Then
            '                compound.DissociationConstants = EquilibratorBlobParser.ParseDissociationConstants(compound.DissociationConstantsRaw)
            '            End If

            '            compoundDict(compound.Id) = compound
            '        End While
            '    End Using
            'End Using

            '' 读取微物种数据
            'Using cmd As New SQLiteCommand("SELECT * FROM compound_microspecies", _connection)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim ms As New CompoundMicrospecies With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .CompoundId = reader.GetInt32(reader.GetOrdinal("compound_id")),
            '                .Charge = reader.GetInt32(reader.GetOrdinal("charge")),
            '                .NumberProtons = reader.GetInt32(reader.GetOrdinal("number_protons")),
            '                .NumberMagnesiums = reader.GetInt32(reader.GetOrdinal("number_magnesiums")),
            '                .IsMajor = reader.GetBoolean(reader.GetOrdinal("is_major"))
            '            }

            '            Dim ddgIdx As Integer = reader.GetOrdinal("ddg_over_rt")
            '            If Not reader.IsDBNull(ddgIdx) Then
            '                ms.DdGOverRt = reader.GetDouble(ddgIdx)
            '            End If

            '            If compoundDict.ContainsKey(ms.CompoundId) Then
            '                ms.Compound = compoundDict(ms.CompoundId)
            '                compoundDict(ms.CompoundId).Microspecies.Add(ms)
            '            End If
            '        End While
            '    End Using
            'End Using

            '' 读取镁解离常数
            'Using cmd As New SQLiteCommand("SELECT * FROM magnesium_dissociation_constant", _connection)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim mgDiss As New MagnesiumDissociationConstant With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .CompoundId = reader.GetInt32(reader.GetOrdinal("compound_id")),
            '                .NumberProtons = reader.GetInt32(reader.GetOrdinal("number_protons")),
            '                .NumberMagnesiums = reader.GetInt32(reader.GetOrdinal("number_magnesiums")),
            '                .DissociationConstant = reader.GetDouble(reader.GetOrdinal("dissociation_constant"))
            '            }

            '            If compoundDict.ContainsKey(mgDiss.CompoundId) Then
            '                mgDiss.Compound = compoundDict(mgDiss.CompoundId)
            '                compoundDict(mgDiss.CompoundId).MagnesiumDissociationConstants.Add(mgDiss)
            '            End If
            '        End While
            '    End Using
            'End Using

            '' 读取标识符数据
            'Using cmd As New SQLiteCommand("SELECT * FROM compound_identifiers", _connection)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim identifier As New CompoundIdentifier With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .CompoundId = reader.GetInt32(reader.GetOrdinal("compound_id")),
            '                .RegistryId = reader.GetInt32(reader.GetOrdinal("registry_id")),
            '                .Accession = reader.GetString(reader.GetOrdinal("accession"))
            '            }

            '            If compoundDict.ContainsKey(identifier.CompoundId) Then
            '                identifier.Compound = compoundDict(identifier.CompoundId)
            '                compoundDict(identifier.CompoundId).Identifiers.Add(identifier)
            '            End If
            '        End While
            '    End Using
            'End Using

            'compounds.AddRange(compoundDict.Values)
            'Return compounds
        End Function

        ''' <summary>
        ''' 读取注册表数据
        ''' </summary>
        Public Function ReadRegistries() As Dictionary(Of Integer, Registry)
            Dim registries As New Dictionary(Of Integer, Registry)()
            Return registries
        End Function

        ''' <summary>
        ''' 根据ID读取单个化合物
        ''' </summary>
        Public Function ReadCompoundById(compoundId As Integer) As Compound
            'Open()

            'Dim compound As Compound = Nothing

            '' 读取化合物主表
            'Using cmd As New SQLiteCommand("SELECT * FROM compounds WHERE id = @id", _connection)
            '    cmd.Parameters.AddWithValue("@id", compoundId)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        If reader.Read() Then
            '            compound = New Compound With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .InChIKey = If(reader.IsDBNull(reader.GetOrdinal("inchi_key")), Nothing, reader.GetString(reader.GetOrdinal("inchi_key"))),
            '                .InChI = If(reader.IsDBNull(reader.GetOrdinal("inchi")), Nothing, reader.GetString(reader.GetOrdinal("inchi"))),
            '                .Smiles = If(reader.IsDBNull(reader.GetOrdinal("smiles")), Nothing, reader.GetString(reader.GetOrdinal("smiles"))),
            '                .Mass = If(reader.IsDBNull(reader.GetOrdinal("mass")), 0.0, reader.GetDouble(reader.GetOrdinal("mass"))),
            '                .Microspecies = New List(Of CompoundMicrospecies)(),
            '                .Identifiers = New List(Of CompoundIdentifier)(),
            '                .MagnesiumDissociationConstants = New List(Of MagnesiumDissociationConstant)()
            '            }

            '            ' 读取BLOB字段
            '            Dim atomBagIdx As Integer = reader.GetOrdinal("atom_bag")
            '            Dim dissocIdx As Integer = reader.GetOrdinal("dissociation_constants")
            '            Dim groupVecIdx As Integer = reader.GetOrdinal("group_vector")

            '            If Not reader.IsDBNull(atomBagIdx) Then
            '                compound.AtomBagRaw = DirectCast(reader(atomBagIdx), Byte())
            '                compound.AtomBag = EquilibratorBlobParser.ParseAtomBag(compound.AtomBagRaw)
            '            End If

            '            If Not reader.IsDBNull(dissocIdx) Then
            '                compound.DissociationConstantsRaw = DirectCast(reader(dissocIdx), Byte())
            '                compound.DissociationConstants = EquilibratorBlobParser.ParseDissociationConstants(compound.DissociationConstantsRaw)
            '            End If

            '            If Not reader.IsDBNull(groupVecIdx) Then
            '                compound.GroupVectorRaw = DirectCast(reader(groupVecIdx), Byte())
            '                compound.GroupVector = EquilibratorBlobParser.ParseGroupVector(compound.GroupVectorRaw)
            '            End If
            '        End If
            '    End Using
            'End Using

            'If compound Is Nothing Then
            '    Return Nothing
            'End If

            '' 读取微物种数据
            'Using cmd As New SQLiteCommand("SELECT * FROM compound_microspecies WHERE compound_id = @id", _connection)
            '    cmd.Parameters.AddWithValue("@id", compoundId)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim ms As New CompoundMicrospecies With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .CompoundId = compoundId,
            '                .Charge = reader.GetInt32(reader.GetOrdinal("charge")),
            '                .NumberProtons = reader.GetInt32(reader.GetOrdinal("number_protons")),
            '                .NumberMagnesiums = reader.GetInt32(reader.GetOrdinal("number_magnesiums")),
            '                .IsMajor = reader.GetBoolean(reader.GetOrdinal("is_major")),
            '                .Compound = compound
            '            }

            '            Dim ddgIdx As Integer = reader.GetOrdinal("ddg_over_rt")
            '            If Not reader.IsDBNull(ddgIdx) Then
            '                ms.DdGOverRt = reader.GetDouble(ddgIdx)
            '            End If

            '            compound.Microspecies.Add(ms)
            '        End While
            '    End Using
            'End Using

            '' 读取镁解离常数
            'Using cmd As New SQLiteCommand("SELECT * FROM magnesium_dissociation_constant WHERE compound_id = @id", _connection)
            '    cmd.Parameters.AddWithValue("@id", compoundId)
            '    Using reader As SQLiteDataReader = cmd.ExecuteReader()
            '        While reader.Read()
            '            Dim mgDiss As New MagnesiumDissociationConstant With {
            '                .Id = reader.GetInt32(reader.GetOrdinal("id")),
            '                .CompoundId = compoundId,
            '                .NumberProtons = reader.GetInt32(reader.GetOrdinal("number_protons")),
            '                .NumberMagnesiums = reader.GetInt32(reader.GetOrdinal("number_magnesiums")),
            '                .DissociationConstant = reader.GetDouble(reader.GetOrdinal("dissociation_constant")),
            '                .Compound = compound
            '            }

            '            compound.MagnesiumDissociationConstants.Add(mgDiss)
            '        End While
            '    End Using
            'End Using

            ' Return compound
        End Function

        ''' <summary>
        ''' 根据InChI Key读取化合物
        ''' </summary>
        Public Function ReadCompoundByInChIKey(inchiKey As String) As Compound
            'Open()

            'Dim compoundId As Integer = -1

            '' 首先找到化合物ID
            'Using cmd As New SQLiteCommand("SELECT id FROM compounds WHERE inchi_key = @key", _connection)
            '    cmd.Parameters.AddWithValue("@key", inchiKey)
            '    Dim result = cmd.ExecuteScalar()
            '    If result IsNot Nothing Then
            '        compoundId = CInt(result)
            '    End If
            'End Using

            'If compoundId < 0 Then
            '    Return Nothing
            'End If

            'Return ReadCompoundById(compoundId)
        End Function

        ''' <summary>
        ''' 根据SMILES读取化合物
        ''' </summary>
        Public Function ReadCompoundBySmiles(smiles As String) As Compound
            'Open()

            'Dim compoundId As Integer = -1

            'Using cmd As New SQLiteCommand("SELECT id FROM compounds WHERE smiles = @smiles", _connection)
            '    cmd.Parameters.AddWithValue("@smiles", smiles)
            '    Dim result = cmd.ExecuteScalar()
            '    If result IsNot Nothing Then
            '        compoundId = CInt(result)
            '    End If
            'End Using

            'If compoundId < 0 Then
            '    Return Nothing
            'End If

            'Return ReadCompoundById(compoundId)
        End Function

        ''' <summary>
        ''' 根据标识符读取化合物
        ''' </summary>
        Public Function ReadCompoundByIdentifier(accession As String) As Compound
            'Open()

            'Dim compoundId As Integer = -1

            'Using cmd As New SQLiteCommand("SELECT compound_id FROM compound_identifiers WHERE accession = @acc", _connection)
            '    cmd.Parameters.AddWithValue("@acc", accession)
            '    Dim result = cmd.ExecuteScalar()
            '    If result IsNot Nothing Then
            '        compoundId = CInt(result)
            '    End If
            'End Using

            'If compoundId < 0 Then
            '    Return Nothing
            'End If

            'Return ReadCompoundById(compoundId)
        End Function

#Region "IDisposable Support"

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not _disposed Then
                If disposing Then
                    If _connection IsNot Nothing Then
                        _connection.Dispose()
                    End If
                End If
                _disposed = True
            End If
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Namespace
