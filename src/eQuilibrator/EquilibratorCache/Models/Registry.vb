''' <summary>
''' 化合物注册表模型。
''' 等价于 Python equilibrator_cache/models/registry.py。
''' 表示 MIRIAM 注册表（如 KEGG、BiGG、ChEBI 等），
''' 用于标识化合物的来源命名空间。
''' </summary>
Imports System.Text.RegularExpressions

Namespace Cache

    Public Class Registry

        ' =========================================================================
        ' 数据库列映射
        ' =========================================================================

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>
        ''' 注册表的 MIRIAM 标识符。
        ''' 例如 "MIR:00000567"（MetaNetX）、"MIR:00000578"（KEGG）等。
        ''' </summary>
        Public Property Identifier As String

        ''' <summary>
        ''' 命名空间名称。
        ''' 例如 "metanetx.chemical"、"kegg.compound" 等。
        ''' </summary>
        Public Property [Namespace] As String

        ''' <summary>
        ''' 注册表的可读名称。
        ''' 例如 "MetaNetX Chemical"、"KEGG Compound" 等。
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' 访问号的正则表达式模式。
        ''' 用于验证标识符是否符合注册表的格式要求。
        ''' </summary>
        Public Property Pattern As String

        ''' <summary>
        ''' 注册表主页 URL。
        ''' </summary>
        Public Property HomePage As String

        ' =========================================================================
        ' 运行时属性
        ' =========================================================================

        ''' <summary>
        ''' 编译后的正则表达式对象。
        ''' 等价于 Python 中的 self.compiled_pattern。
        ''' </summary>
        Private _compiledPattern As Regex

        ''' <summary>
        ''' MIRIAM 标识符的验证正则。
        ''' 等价于 Python _identifier_pattern = re.compile(r"^MIR:\d{8}$")。
        ''' </summary>
        Private Shared ReadOnly IdentifierPattern As New Regex("^MIR:\d{8}$")

        ' =========================================================================
        ' 构造函数
        ' =========================================================================

        ''' <summary>默认构造函数</summary>
        Public Sub New()
            _compiledPattern = Nothing
        End Sub

        ''' <summary>
        ''' 带参数的构造函数。
        ''' 等价于 Python __init__ 中的 self.compiled_pattern = re.compile(self.pattern)。
        ''' </summary>
        Public Sub New(identifier As String, ns As String, name As String, pattern As String, homePage As String)
            Me.Identifier = identifier
            Me.Namespace = ns
            Me.Name = name
            Me.Pattern = pattern
            Me.HomePage = homePage
            If Not String.IsNullOrEmpty(pattern) Then
                _compiledPattern = New Regex(pattern)
            End If
        End Sub

        ' =========================================================================
        ' 方法
        ' =========================================================================

        ''' <summary>
        ''' 从数据库加载后初始化编译后的正则表达式。
        ''' 等价于 Python @reconstructor def init_on_load(self)。
        ''' </summary>
        Public Sub InitOnLoad()
            If Not String.IsNullOrEmpty(Pattern) Then
                _compiledPattern = New Regex(Pattern)
            End If
        End Sub

        ''' <summary>
        ''' 验证 MIRIAM 标识符是否符合 "^MIR:\d{8}$" 格式。
        ''' 等价于 Python @validates("identifier") def validate_identifier(self, _, identifier)。
        ''' </summary>
        ''' <param name="identifier">待验证的标识符</param>
        ''' <returns>验证通过的标识符</returns>
        ''' <exception cref="ArgumentException">标识符格式不正确时抛出</exception>
        Public Function ValidateIdentifier(identifier As String) As String
            If IdentifierPattern.IsMatch(identifier) = False Then
                Throw New ArgumentException(
                $"注册表的标识符 '{identifier}' 不符合官方模式 '^MIR:\d{{8}}$'。")
            End If
            Return identifier
        End Function

        ''' <summary>
        ''' 验证访问号是否符合编译后的正则表达式模式。
        ''' 等价于 Python is_valid_accession(self, accession)。
        ''' </summary>
        ''' <param name="accession">待验证的访问号</param>
        ''' <returns>如果匹配则返回 True，否则返回 False</returns>
        Public Function IsValidAccession(accession As String) As Boolean
            If _compiledPattern Is Nothing Then Return False
            Return _compiledPattern.IsMatch(accession)
        End Function

        ''' <summary>
        ''' 返回对象的字符串表示。
        ''' 等价于 Python __repr__。
        ''' </summary>
        Public Overrides Function ToString() As String
            Return $"Registry(namespace={[Namespace]})"
        End Function

    End Class
End Namespace