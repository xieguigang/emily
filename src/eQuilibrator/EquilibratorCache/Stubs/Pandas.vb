''' <summary>
''' Pandas DataFrame 的 VB.NET 占位实现。
''' 提供最基本的数据表操作能力。
''' 由于不引入第三方 NuGet 包，此处使用 DataTable 来模拟。
''' </summary>
Imports System.Data

Public Class DataFrame

    ''' <summary>内部数据表</summary>
    Private _table As DataTable

    ''' <summary>默认构造函数</summary>
    Public Sub New()
        _table = New DataTable()
    End Sub

    ''' <summary>从 DataTable 构造</summary>
    Public Sub New(dt As DataTable)
        _table = dt
    End Sub

    ''' <summary>获取行数</summary>
    Public ReadOnly Property RowCount As Integer
        Get
            Return _table.Rows.Count
        End Get
    End Property

    ''' <summary>获取列数</summary>
    Public ReadOnly Property ColumnCount As Integer
        Get
            Return _table.Columns.Count
        End Get
    End Property

    ''' <summary>获取或设置指定行列的值</summary>
    Default Public Property Item(rowIndex As Integer, colIndex As Integer) As Object
        Get
            Return _table.Rows(rowIndex)(colIndex)
        End Get
        Set(value As Object)
            _table.Rows(rowIndex)(colIndex) = value
        End Set
    End Property

    ''' <summary>获取或设置指定行和列名的值</summary>
    Default Public Property Item(rowIndex As Integer, colName As String) As Object
        Get
            Return _table.Rows(rowIndex)(colName)
        End Get
        Set(value As Object)
            _table.Rows(rowIndex)(colName) = value
        End Set
    End Property

    ''' <summary>
    ''' 创建一个空的 Series（一维数据列）。
    ''' 等价于 pd.Series(index=..., dtype=float)。
    ''' </summary>
    Public Shared Function CreateSeries(index As Object(), dtype As Type) As Series
        Return New Series(index, dtype)
    End Function

    ''' <summary>
    ''' 沿列方向拼接多个 Series 为 DataFrame。
    ''' 等价于 pd.concat(seriesList, axis=1).fillna(0.0)。
    ''' </summary>
    Public Shared Function ConcatAndFill(seriesList As List(Of Series), fillValue As Double) As DataFrame
        ' 占位实现
        Return New DataFrame()
    End Function

    ''' <summary>获取内部 DataTable</summary>
    Public ReadOnly Property InnerTable As DataTable
        Get
            Return _table
        End Get
    End Property

End Class

''' <summary>
''' Pandas Series 的 VB.NET 占位实现。
''' 提供一维带索引的数据序列。
''' </summary>
Public Class Series

    ''' <summary>索引列表</summary>
    Private _index As Object()

    ''' <summary>值列表</summary>
    Private _values As Double()

    ''' <summary>构造函数</summary>
    Public Sub New(index As Object(), dtype As Type)
        _index = index
        _values = New Double(index.Length - 1) {}
    End Sub

    ''' <summary>获取或设置指定索引位置的值</summary>
    Default Public Property Item(idx As Integer) As Double
        Get
            Return _values(idx)
        End Get
        Set(value As Double)
            _values(idx) = value
        End Set
    End Property

    ''' <summary>获取或设置指定索引键的值</summary>
    Default Public Property Item(key As Object) As Double
        Get
            For i As Integer = 0 To _index.Length - 1
                If _index(i).Equals(key) Then Return _values(i)
            Next
            Return 0.0
        End Get
        Set(value As Double)
            For i As Integer = 0 To _index.Length - 1
                If _index(i).Equals(key) Then
                    _values(i) = value
                    Return
                End If
            Next
        End Set
    End Property

    ''' <summary>索引数量</summary>
    Public ReadOnly Property Length As Integer
        Get
            Return _index.Length
        End Get
    End Property

End Class

''' <summary>
''' Pandas 工厂方法的占位模块。
''' </summary>
Public Module PandasFactory

    ''' <summary>等价于 pd.Series(index=..., dtype=float)</summary>
    Public Function CreateSeries(index As Object(), dtype As Type) As Series
        Return New Series(index, dtype)
    End Function

    ''' <summary>等价于 pd.concat(seriesList, axis=1).fillna(fillValue)</summary>
    Public Function ConcatAndFill(seriesList As List(Of Series), fillValue As Double) As DataFrame
        Return DataFrame.ConcatAndFill(seriesList, fillValue)
    End Function

End Module
