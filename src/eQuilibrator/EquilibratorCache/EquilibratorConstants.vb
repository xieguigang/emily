''' <summary>
''' equilibrator_cache 主模块。
''' 等价于 Python equilibrator_cache/__init__.py。
''' 定义全局常量和导出接口。
''' </summary>

''' <summary>
''' equilibrator_cache 全局常量。
''' </summary>
Public Module EquilibratorConstants

    ' =========================================================================
    ' 版本信息
    ' =========================================================================

    ''' <summary>版本号</summary>
    Public ReadOnly __version__ As String = "0.5.3"

    ' =========================================================================
    ' 化合物标识常量
    ' =========================================================================

    ''' <summary>
    ''' 质子的 InChI 标识符。
    ''' 等价于 Python PROTON_INCHI。
    ''' </summary>
    Public ReadOnly PROTON_INCHI As String = "InChI=1S/p+1"

    ''' <summary>
    ''' 质子的 InChI Key。
    ''' 等价于 Python PROTON_INCHI_KEY。
    ''' </summary>
    Public ReadOnly PROTON_INCHI_KEY As String = "GPRLSGONYQIRFK-UHFFFAOYSA-N"

    ''' <summary>
    ''' 水的 InChI 标识符。
    ''' 等价于 Python WATER_INCHI。
    ''' </summary>
    Public ReadOnly WATER_INCHI As String = "InChI=1S/H2O/h1H2"

    ''' <summary>
    ''' 水的 InChI Key。
    ''' 等价于 Python WATER_INCHI_KEY。
    ''' </summary>
    Public ReadOnly WATER_INCHI_KEY As String = "XLYOFNOQVPJJNP-UHFFFAOYSA-N"

    ' =========================================================================
    ' 默认参数
    ' =========================================================================

    ''' <summary>
    ''' 默认的 Zenodo 记录 ID。
    ''' </summary>
    Public ReadOnly DEFAULT_ZENODO_RECORD_ID As String = "5789186"

    ''' <summary>
    ''' 默认的缓存数据库文件名。
    ''' </summary>
    Public ReadOnly DEFAULT_CACHE_FILENAME As String = "compounds.sqlite"

End Module
