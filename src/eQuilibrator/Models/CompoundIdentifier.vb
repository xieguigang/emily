Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' 化合物标识符模型。
    ''' 等价于 Python equilibrator_cache/models/compound_identifier.py。
    ''' 表示化合物在特定注册表中的标识符（如 KEGG ID、BiGG ID 等）。
    ''' </summary>
    Public Class CompoundIdentifier

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>所属化合物的 ID（外键）</summary>
        Public Property CompoundId As Integer

        ''' <summary>所属注册表的 ID（外键）</summary>
        Public Property RegistryId As Integer

        ''' <summary>访问号（标识符值，例如 "C00031"、"glc__D_c"）。</summary>
        Public Property Accession As String

        ''' <summary>关联的注册表对象。</summary>
        Public Property Registry As Registry

        Public Overrides Function ToString() As String
            Return $"CompoundIdentifier(registry={Registry}, accession={Accession})"
        End Function

        ''' <summary>使用注册表验证访问号是否有效。</summary>
        Public Function IsValid() As Boolean
            If Registry Is Nothing Then Return False
            If Not Registry.IsValidAccession(Accession) Then Return False
            Return True
        End Function
    End Class
End Namespace
