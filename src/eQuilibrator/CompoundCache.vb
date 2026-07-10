Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorApi.Core.Data

Namespace EquilibratorApi.Core

    ''' <summary>
    ''' 化合物缓存。内置质子/水，并可按需从 eQuilibrator CSV 数据源懒加载化合物
    ''' （结构、微物种、基团向量、镁解离常数、标识符）。
    ''' </summary>
    Public Class CompoundCache

        ReadOnly _compounds As New Dictionary(Of String, Compound)(StringComparer.OrdinalIgnoreCase)
        ReadOnly _searchIndex As New Dictionary(Of String, List(Of String))(StringComparer.OrdinalIgnoreCase)

        ''' <summary>化合物 CSV 加载器（可为空）。</summary>
        Private ReadOnly _loader As EquilibratorCsvLoader

        ''' <summary>质子化合物。</summary>
        Public ReadOnly Property Proton As Compound

        ''' <summary>水化合物。</summary>
        Public ReadOnly Property Water As Compound

        ''' <summary>
        ''' 创建化合物缓存。
        ''' </summary>
        ''' <param name="csvDirectory">可选：包含 eQuilibrator CSV 文件的目录。提供后即从 CSV 懒加载化合物。</param>
        Public Sub New(Optional csvDirectory As String = Nothing)
            Proton = New Compound With {
                .Id = "H+",
                .Name = "H+",
                .Charge = 1,
                .ProtonCount = 1,
                .IsProton = True,
                .AtomBag = New Dictionary(Of String, Integer) From {{"H", 1}}
            }
            AddCompound(Proton)

            Water = New Compound With {
                .Id = "H2O",
                .Name = "water",
                .Charge = 0,
                .ProtonCount = 2,
                .MolecularWeight = 18.015,
                .IsWater = True,
                .AtomBag = New Dictionary(Of String, Integer) From {{"H", 2}, {"O", 1}}
            }
            AddCompound(Water)

            If Not String.IsNullOrEmpty(csvDirectory) AndAlso System.IO.Directory.Exists(csvDirectory) Then
                _loader = New EquilibratorCsvLoader(csvDirectory)
            End If
        End Sub

        ''' <summary>添加化合物到缓存并建立检索索引。</summary>
        Public Sub AddCompound(compound As Compound)
            _compounds(compound.Id) = compound
            AddToSearchIndex(compound.Id, compound.Id)
            If Not String.IsNullOrEmpty(compound.Name) Then
                AddToSearchIndex(compound.Name, compound.Id)
            End If
            If Not String.IsNullOrEmpty(compound.InChIKey) Then
                AddToSearchIndex(compound.InChIKey, compound.Id)
            End If
        End Sub

        Private Sub AddToSearchIndex(key As String, compoundId As String)
            Dim normalizedKey = key.ToLowerInvariant()
            Dim list As List(Of String) = Nothing
            If Not _searchIndex.TryGetValue(normalizedKey, list) Then
                list = New List(Of String)()
                _searchIndex(normalizedKey) = list
            End If
            If Not list.Contains(compoundId) Then
                list.Add(compoundId)
            End If
        End Sub

        ''' <summary>按化合物标识获取化合物（内置或 CSV 懒加载）。</summary>
        Public Function GetCompound(compoundId As String) As Compound
            Dim compound As Compound = Nothing
            If _compounds.TryGetValue(compoundId, compound) Then
                Return compound
            End If

            If _loader IsNot Nothing Then
                compound = _loader.GetCompound(compoundId)
                If compound IsNot Nothing Then
                    AddCompound(compound)
                    Return compound
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>按化合物标识获取 PhasedCompound。</summary>
        Public Function GetPhasedCompound(compoundId As String, Optional phase As String = ThermodynamicConstants.DefaultPhase) As PhasedCompound
            Dim compound = GetCompound(compoundId)
            Return If(compound IsNot Nothing, compound.ToPhasedCompound(phase), Nothing)
        End Function

        ''' <summary>按 accession 检索化合物（CSV 子串匹配）。</summary>
        Public Function SearchCompounds(query As String) As List(Of Compound)
            Dim normalizedQuery = query.ToLowerInvariant()
            Dim results = New List(Of Compound)()

            Dim exactMatches As List(Of String) = Nothing
            If _searchIndex.TryGetValue(normalizedQuery, exactMatches) Then
                results.AddRange(exactMatches.Select(AddressOf Me.GetCompound).Where(Function(c) c IsNot Nothing))
            End If

            If _loader IsNot Nothing Then
                For Each acc In _loader.SearchAccessions(query).Take(50)
                    Dim c = GetCompound(acc)
                    If c IsNot Nothing AndAlso Not results.Any(Function(x) x.Id = c.Id) Then
                        results.Add(c)
                    End If
                Next
            End If

            Return results.DistinctBy(Function(c) c.Id).ToList()
        End Function

        ''' <summary>判断化合物是否为质子。</summary>
        Public Function IsProton(compoundId As String) As Boolean
            Return String.Equals(compoundId, "H+", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>判断化合物是否为水。</summary>
        Public Function IsWater(compoundId As String) As Boolean
            Return String.Equals(compoundId, "H2O", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>按 accession 获取 CSV 主键（找不到返回 Nothing）。</summary>
        Public Function GetCompoundIdByAccession(accession As String) As Integer?
            If _loader IsNot Nothing Then Return _loader.GetCompoundIdByAccession(accession)
            Return Nothing
        End Function
    End Class
End Namespace
