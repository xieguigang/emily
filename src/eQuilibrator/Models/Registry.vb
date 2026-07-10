Imports System.Text.RegularExpressions

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' 化合物注册表模型（MIRIAM 注册表，如 KEGG、BiGG、ChEBI 等）。
    ''' 等价于 Python equilibrator_cache/models/registry.py。
    ''' </summary>
    Public Class Registry

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>MIRIAM 标识符（如 "MIR:00000567"）。</summary>
        Public Property Identifier As String

        ''' <summary>命名空间名称（如 "kegg.compound"）。</summary>
        Public Property [Namespace] As String

        ''' <summary>可读名称（如 "KEGG Compound"）。</summary>
        Public Property Name As String

        ''' <summary>访问号的正则表达式模式。</summary>
        Public Property Pattern As String

        ''' <summary>注册表主页 URL。</summary>
        Public Property HomePage As String

        Private _compiledPattern As Regex

        Private Shared ReadOnly IdentifierPattern As New Regex("^MIR:\d{8}$")

        Public Sub New()
            _compiledPattern = Nothing
        End Sub

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

        ''' <summary>从数据库加载后初始化编译后的正则表达式。</summary>
        Public Sub InitOnLoad()
            If Not String.IsNullOrEmpty(Pattern) Then
                _compiledPattern = New Regex(Pattern)
            End If
        End Sub

        ''' <summary>验证 MIRIAM 标识符格式。</summary>
        Public Function ValidateIdentifier(identifier As String) As String
            If IdentifierPattern.IsMatch(identifier) = False Then
                Throw New ArgumentException(
                    $"注册表的标识符 '{identifier}' 不符合官方模式 '^MIR:\d{{8}}$'。")
            End If
            Return identifier
        End Function

        ''' <summary>验证访问号是否匹配编译后的模式。</summary>
        Public Function IsValidAccession(accession As String) As Boolean
            If _compiledPattern Is Nothing Then Return False
            Return _compiledPattern.IsMatch(accession)
        End Function

        Public Overrides Function ToString() As String
            Return $"Registry(namespace={[Namespace]})"
        End Function
    End Class
End Namespace
