''' <summary>
''' API 入口模块。
''' 等价于 Python equilibrator_cache/api.py。
''' 提供创建化合物缓存的高级 API 函数。
''' </summary>
Public Module Api

    ''' <summary>
    ''' 创建默认的化合物缓存。
    ''' 等价于 Python default_cache()。
    ''' 
    ''' 此方法从 Zenodo 下载（或使用本地缓存的）化合物数据库，
    ''' 并创建一个 CompoundCache 实例。
    ''' </summary>
    ''' <returns>化合物缓存对象</returns>
    Public Function DefaultCache() As CompoundCache
        Dim cachePath As String = Zenodo.GetCachePath()
        Return New CompoundCache(cachePath)
    End Function

    ''' <summary>
    ''' 从指定路径创建化合物缓存。
    ''' 等价于 Python local_cache(path)。
    ''' </summary>
    ''' <param name="path">SQLite 数据库文件的路径</param>
    ''' <returns>化合物缓存对象</returns>
    Public Function LocalCache(path As String) As CompoundCache
        If Not System.IO.File.Exists(path) Then
            Throw New System.IO.FileNotFoundException(
                $"化合物缓存数据库文件未找到: {path}")
        End If
        Return New CompoundCache(path)
    End Function

    ''' <summary>
    ''' 从 Zenodo 记录创建化合物缓存。
    ''' 等价于 Python zenodo_cache(record_id)。
    ''' </summary>
    ''' <param name="recordId">Zenodo 记录 ID</param>
    ''' <returns>化合物缓存对象</returns>
    Public Function ZenodoCache(Optional recordId As String = Nothing) As CompoundCache
        Dim cachePath As String = Zenodo.GetCachePath(recordId)
        Return New CompoundCache(cachePath)
    End Function

End Module
