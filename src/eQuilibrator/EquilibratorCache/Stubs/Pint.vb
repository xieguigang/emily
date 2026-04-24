''' <summary>
''' Pint 物理单位库的 VB.NET 占位实现。
''' 提供带单位的物理量（Quantity）的简单封装。
''' 由于不引入第三方 NuGet 包，此处仅实现最小功能集。
''' </summary>
Public Class UnitRegistry

    ''' <summary>单位注册表的单例</summary>
    Public Shared ReadOnly Instance As New UnitRegistry()

    ''' <summary>
    ''' 创建一个带单位的物理量。
    ''' </summary>
    Public Function CreateQuantity(value As Double, unit As String) As Quantity
        Return New Quantity(value, unit)
    End Function

    ''' <summary>
    ''' 检查参数单位是否符合要求（占位实现，始终返回 True）。
    ''' 等价于 pint 的 ureg.check() 装饰器。
    ''' </summary>
    Public Function Check(ParamArray dimensionality() As String) As Boolean
        ' 占位实现：不进行实际单位检查
        Return True
    End Function

End Class

''' <summary>
''' 带单位的物理量。
''' 等价于 pint.UnitRegistry.Quantity。
''' </summary>
Public Class Quantity

    ''' <summary>数值</summary>
    Public Property Value As Double

    ''' <summary>单位字符串</summary>
    Public Property Unit As String

    ''' <summary>默认构造函数</summary>
    Public Sub New()
        Value = 0.0
        Unit = ""
    End Sub

    ''' <summary>构造带单位的物理量</summary>
    Public Sub New(val As Double, unitStr As String)
        Value = val
        Unit = unitStr
    End Sub

    ''' <summary>
    ''' 将当前量转换为指定单位并返回数值。
    ''' 等价于 pint 的 Quantity.m_as(unit)。
    ''' </summary>
    Public Function MAs(targetUnit As String) As Double
        ' 占位实现：简单返回当前值
        ' 实际实现中应进行单位换算
        Select Case targetUnit
            Case "K", "kJ/mol/K", "M", ""
                Return Value
            Case Else
                Return Value
        End Select
    End Function

    ''' <summary>加法运算</summary>
    Public Shared Operator +(a As Quantity, b As Quantity) As Quantity
        Return New Quantity(a.Value + b.Value, a.Unit)
    End Operator

    ''' <summary>减法运算</summary>
    Public Shared Operator -(a As Quantity, b As Quantity) As Quantity
        Return New Quantity(a.Value - b.Value, a.Unit)
    End Operator

    ''' <summary>乘法运算（Quantity * Double）</summary>
    Public Shared Operator *(q As Quantity, d As Double) As Quantity
        Return New Quantity(q.Value * d, q.Unit)
    End Operator

    ''' <summary>乘法运算（Double * Quantity）</summary>
    Public Shared Operator *(d As Double, q As Quantity) As Quantity
        Return New Quantity(d * q.Value, q.Unit)
    End Operator

    ''' <summary>乘法运算（Quantity * Quantity）</summary>
    Public Shared Operator *(a As Quantity, b As Quantity) As Quantity
        Return New Quantity(a.Value * b.Value, CombineUnits(a.Unit, b.Unit))
    End Operator

    ''' <summary>取反运算</summary>
    Public Shared Operator -(q As Quantity) As Quantity
        Return New Quantity(-q.Value, q.Unit)
    End Operator

    ''' <summary>除法运算（Quantity / Double）</summary>
    Public Shared Operator /(q As Quantity, d As Double) As Quantity
        Return New Quantity(q.Value / d, q.Unit)
    End Operator

    ''' <summary>除法运算（Quantity / Quantity）</summary>
    Public Shared Operator /(a As Quantity, b As Quantity) As Quantity
        Return New Quantity(a.Value / b.Value, CombineUnits(a.Unit, b.Unit, divide:=True))
    End Operator

    ''' <summary>字符串表示</summary>
    Public Overrides Function ToString() As String
        Return $"{Value} {Unit}"
    End Function

    ''' <summary>组合两个单位字符串</summary>
    Private Shared Function CombineUnits(u1 As String, u2 As String, Optional divide As Boolean = False) As String
        If String.IsNullOrEmpty(u1) Then Return u2
        If String.IsNullOrEmpty(u2) Then Return u1
        If divide Then
            Return $"{u1} / {u2}"
        Else
            Return $"{u1} * {u2}"
        End If
    End Function

End Class

''' <summary>
''' Pint 单位系统的快捷工厂方法。
''' 等价于 Python 中的 Q_ = ureg.Quantity。
''' </summary>
Public Module PintFactory

    ''' <summary>全局单位注册表</summary>
    Public ReadOnly ureg As UnitRegistry = UnitRegistry.Instance

    ''' <summary>
    ''' 创建带单位的物理量。
    ''' 等价于 Q_(value, unit)。
    ''' </summary>
    Public Function Q_(value As Double, unit As String) As Quantity
        Return New Quantity(value, unit)
    End Function

    ''' <summary>
    ''' 创建带单位的物理量（仅指定数值，单位为空）。
    ''' </summary>
    Public Function Q_(unit As String) As Quantity
        Return New Quantity(0.0, unit)
    End Function

End Module
