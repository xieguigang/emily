''' <summary>
''' 异常定义模块。
''' 等价于 Python equilibrator_cache/exceptions.py。
''' 定义了反应式解析异常和缺失解离常数异常。
''' </summary>

''' <summary>
''' 表示反应式无法正确解析的异常。
''' 等价于 Python ParseException。
''' </summary>
Public Class ParseException
    Inherits Exception

    ''' <summary>默认构造函数</summary>
    Public Sub New()
        MyBase.New("反应式无法正确解析。")
    End Sub

    ''' <summary>带消息的构造函数</summary>
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    ''' <summary>带消息和内部异常的构造函数</summary>
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

End Class

''' <summary>
''' 表示化合物缺失 pKa 列表的异常。
''' 等价于 Python MissingDissociationConstantsException。
''' </summary>
Public Class MissingDissociationConstantsException
    Inherits Exception

    ''' <summary>默认构造函数</summary>
    Public Sub New()
        MyBase.New("化合物缺失解离常数列表。")
    End Sub

    ''' <summary>带消息的构造函数</summary>
    Public Sub New(message As String)
        MyBase.New(message)
    End Sub

    ''' <summary>带消息和内部异常的构造函数</summary>
    Public Sub New(message As String, innerException As Exception)
        MyBase.New(message, innerException)
    End Sub

End Class
