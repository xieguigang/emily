''' <summary>
''' 第三方库占位 Stubs 的集合文件。
''' 以下库在 VB.NET 中无直接对应关系，使用空对象加空函数占位。
''' </summary>

' ============================================================================
' Levenshtein 库占位
' Python: import Levenshtein
' 用途: 计算字符串之间的编辑距离（Levenshtein distance）
' ============================================================================
Public Module LevenshteinStub

    ''' <summary>
    ''' 计算两个字符串之间的 Levenshtein 编辑距离。
    ''' 等价于 Levenshtein.distance(s1, s2)。
    ''' 占位实现：使用动态规划算法计算编辑距离。
    ''' </summary>
    Public Function Distance(s1 As String, s2 As String) As Integer
        Dim len1 As Integer = s1.Length
        Dim len2 As Integer = s2.Length
        Dim dp(len1, len2) As Integer

        For i As Integer = 0 To len1
            dp(i, 0) = i
        Next
        For j As Integer = 0 To len2
            dp(0, j) = j
        Next

        For i As Integer = 1 To len1
            For j As Integer = 1 To len2
                Dim cost As Integer = If(s1(i - 1) = s2(j - 1), 0, 1)
                dp(i, j) = Math.Min(Math.Min(dp(i - 1, j) + 1, dp(i, j - 1) + 1), dp(i - 1, j - 1) + cost)
            Next
        Next

        Return dp(len1, len2)
    End Function

End Module

' ============================================================================
' tqdm 库占位
' Python: from tqdm import tqdm
' 用途: 进度条显示
' ============================================================================
Public Module TqdmStub

    ''' <summary>
    ''' 包装可迭代对象以显示进度条。
    ''' 等价于 tqdm(iterable, desc=...)。
    ''' 占位实现：直接返回原始集合，不显示进度条。
    ''' </summary>
    Public Function Tqdm(Of T)(iterable As IEnumerable(Of T), Optional desc As String = "") As IEnumerable(Of T)
        ' 占位实现：不显示进度条，直接返回原始集合
        Return iterable
    End Function

End Module

' ============================================================================
' appdirs 库占位
' Python: import appdirs
' 用途: 获取平台特定的用户数据/缓存目录
' ============================================================================
Public Module AppDirsStub

    ''' <summary>
    ''' 获取用户缓存目录路径。
    ''' 等价于 appdirs.user_cache_dir(appname=...)。
    ''' 占位实现：返回平台特定的缓存目录。
    ''' </summary>
    Public Function UserCacheDir(Optional appname As String = "equilibrator",
                                  Optional appauthor As String = "",
                                  Optional version As String = "",
                                  Optional opinion As Boolean = True) As String
        Dim basePath As String = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
        If Not String.IsNullOrEmpty(appname) Then
            basePath = System.IO.Path.Combine(basePath, appname)
        End If
        If Not String.IsNullOrEmpty(version) Then
            basePath = System.IO.Path.Combine(basePath, version)
        End If
        Return basePath
    End Function

End Module

' ============================================================================
' pooch 库占位
' Python: import pooch
' 用途: 从远程服务器下载和缓存数据文件
' ============================================================================
Public Module PoochStub

    ''' <summary>
    ''' 从远程 URL 下载文件并缓存到本地。
    ''' 等价于 pooch.retrieve(path=..., fname=..., url=..., known_hash=..., progressbar=...)。
    ''' 占位实现：不执行实际下载，返回本地路径。
    ''' </summary>
    Public Function Retrieve(path As String, fname As String, url As String,
                              knownHash As String, Optional progressbar As Boolean = True) As String
        ' 占位实现：仅返回期望的本地文件路径
        Dim fullPath As String = System.IO.Path.Combine(path, fname)

        ' 如果文件已存在，直接返回路径
        If System.IO.File.Exists(fullPath) Then
            Return fullPath
        End If

        ' 占位：不执行实际下载
        ' 实际实现中应使用 System.Net.WebClient 或 HttpClient 下载文件
        Throw New NotImplementedException(
            "PoochStub.Retrieve: 远程文件下载功能未实现。请手动下载文件到: " & fullPath &
            " (URL: " & url & ", MD5: " & knownHash & ")")
    End Function

End Module

' ============================================================================
' cobra 库占位
' Python: import cobra
' 用途: 约束基建模（COBRA）工具箱的 Python 接口
' ============================================================================
Namespace CobraStub

    ''' <summary>
    ''' COBRA 模型的占位类。
    ''' 等价于 cobra.Model。
    ''' </summary>
    Public Class Model
        ''' <summary>模型 ID</summary>
        Public Property Id As String
        ''' <summary>代谢物列表</summary>
        Public Property Metabolites As List(Of Metabolite) = New List(Of Metabolite)()
        ''' <summary>反应列表</summary>
        Public Property Reactions As List(Of Reaction) = New List(Of Reaction)()
    End Class

    ''' <summary>
    ''' COBRA 代谢物的占位类。
    ''' 等价于 cobra.Metabolite。
    ''' </summary>
    Public Class Metabolite
        ''' <summary>代谢物 ID</summary>
        Public Property Id As String
        ''' <summary>代谢物名称</summary>
        Public Property Name As String
        ''' <summary>注释字典</summary>
        Public Property Annotation As Dictionary(Of String, Object) = New Dictionary(Of String, Object)()
        ''' <summary> compartments</summary>
        Public Property Compartment As String = ""
    End Class

    ''' <summary>
    ''' COBRA 反应的占位类。
    ''' 等价于 cobra.Reaction。
    ''' </summary>
    Public Class Reaction
        ''' <summary>反应 ID</summary>
        Public Property Id As String
        ''' <summary>反应名称</summary>
        Public Property Name As String
    End Class

End Namespace
