''' <summary>
''' Zenodo 数据下载和缓存模块。
''' 等价于 Python equilibrator_cache/zenodo.py。
''' 负责从 Zenodo 下载化合物缓存数据库文件并管理本地缓存。
''' </summary>
Public Module Zenodo

    ' =========================================================================
    ' 常量
    ' =========================================================================

    ''' <summary>Zenodo 记录的基础 URL</summary>
    Public ReadOnly ZENODO_BASE_URL As String = "https://zenodo.org/record"

    ''' <summary>默认的 Zenodo 记录 ID</summary>
    Public ReadOnly DEFAULT_RECORD_ID As String = "5789186"

    ''' <summary>默认的数据库文件名</summary>
    Public ReadOnly DEFAULT_CACHE_FILENAME As String = "compounds.sqlite"

    ' =========================================================================
    ' 方法
    ' =========================================================================

    ''' <summary>
    ''' 获取化合物缓存数据库的本地路径。
    ''' 等价于 Python get_cache_path()。
    ''' 
    ''' 如果本地缓存不存在，则从 Zenodo 下载数据库文件。
    ''' 使用 appdirs 确定平台特定的缓存目录。
    ''' </summary>
    ''' <param name="recordId">Zenodo 记录 ID</param>
    ''' <param name="filename">数据库文件名</param>
    ''' <returns>本地缓存文件的完整路径</returns>
    Public Function GetCachePath(Optional recordId As String = Nothing,
                                  Optional filename As String = Nothing) As String
        If recordId Is Nothing Then recordId = DEFAULT_RECORD_ID
        If filename Is Nothing Then filename = DEFAULT_CACHE_FILENAME

        ' 使用平台特定的缓存目录
        Dim cacheDir As String = AppDirsStub.UserCacheDir("equilibrator_cache")
        Dim cachePath As String = System.IO.Path.Combine(cacheDir, recordId, filename)

        ' 如果文件不存在，需要下载
        If Not System.IO.File.Exists(cachePath) Then
            DownloadCacheFromZenodo(recordId, filename, cachePath)
        End If

        Return cachePath
    End Function

    ''' <summary>
    ''' 从 Zenodo 下载化合物缓存数据库。
    ''' 等价于 Python 中使用 pooch 的下载逻辑。
    ''' </summary>
    ''' <param name="recordId">Zenodo 记录 ID</param>
    ''' <param name="filename">数据库文件名</param>
    ''' <param name="targetPath">目标保存路径</param>
    Public Sub DownloadCacheFromZenodo(recordId As String, filename As String, targetPath As String)
        ' 确保目标目录存在
        Dim targetDir As String = System.IO.Path.GetDirectoryName(targetPath)
        If Not System.IO.Directory.Exists(targetDir) Then
            System.IO.Directory.CreateDirectory(targetDir)
        End If

        ' 构建下载 URL
        Dim downloadUrl As String = $"{ZENODO_BASE_URL}/{recordId}/files/{filename}"

        ' 占位实现：使用 .NET 的 WebClient 下载文件
        ' 实际使用时，可以使用 System.Net.WebClient 或 HttpClient
        Try
            Using client As New System.Net.WebClient()
                client.DownloadFile(downloadUrl, targetPath)
            End Using
        Catch ex As Exception
            Throw New Exception(
                $"无法从 Zenodo 下载化合物缓存数据库: {downloadUrl}。错误: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' 获取 Zenodo 文件的 URL。
    ''' </summary>
    Public Function GetZenodoFileUrl(recordId As String, filename As String) As String
        Return $"{ZENODO_BASE_URL}/{recordId}/files/{filename}"
    End Function

    ''' <summary>
    ''' 检查本地缓存是否存在。
    ''' </summary>
    Public Function CacheExists(Optional recordId As String = Nothing,
                                 Optional filename As String = Nothing) As Boolean
        If recordId Is Nothing Then recordId = DEFAULT_RECORD_ID
        If filename Is Nothing Then filename = DEFAULT_CACHE_FILENAME

        Dim cacheDir As String = AppDirsStub.UserCacheDir("equilibrator_cache")
        Dim cachePath As String = System.IO.Path.Combine(cacheDir, recordId, filename)

        Return System.IO.File.Exists(cachePath)
    End Function

End Module
