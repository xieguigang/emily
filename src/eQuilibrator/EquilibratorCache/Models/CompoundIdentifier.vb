Namespace Cache

    ''' <summary>
    ''' 化合物标识符模型。
    ''' 等价于 Python equilibrator_cache/models/compound_identifier.py。
    ''' 表示化合物在特定注册表中的标识符（如 KEGG ID、BiGG ID 等）。
    ''' </summary>
    Public Class CompoundIdentifier

        ' =========================================================================
        ' 数据库列映射
        ' =========================================================================

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>所属化合物的 ID（外键）</summary>
        Public Property CompoundId As Integer

        ''' <summary>所属注册表的 ID（外键）</summary>
        Public Property RegistryId As Integer

        ''' <summary>
        ''' 访问号（标识符值）。
        ''' 例如 "C00031"（KEGG）、"glc__D_c"（BiGG）等。
        ''' </summary>
        Public Property Accession As String

        ' =========================================================================
        ' 关联关系
        ' =========================================================================

        ''' <summary>
        ''' 关联的注册表对象。
        ''' 等价于 Python registry = relationship(Registry, lazy="selectin")。
        ''' </summary>
        Public Property Registry As Registry

        ' =========================================================================
        ' 方法
        ' =========================================================================

        ''' <summary>
        ''' 返回对象的字符串表示。
        ''' 等价于 Python __repr__。
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"CompoundIdentifier(registry={Registry}, accession={Accession})"
        End Function

        ''' <summary>
        ''' 使用注册表验证访问号是否有效。
        ''' 等价于 Python is_valid(self)。
        ''' </summary>
        ''' <returns>如果访问号有效则返回 True，否则返回 False</returns>
        Public Function IsValid() As Boolean
            If Registry Is Nothing Then
                ' logger.Error("No associated registry.")
                Return False
            End If
            If Not Registry.IsValidAccession(Accession) Then
                ' logger.Error($"Identifier '{Accession}' does not match {Registry.Name}'s pattern '{Registry.Pattern}'.")
                Return False
            End If
            Return True
        End Function

    End Class
End Namespace