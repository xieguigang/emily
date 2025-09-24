Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Plot3D.Device
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.d3js.scale
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D

Public Class BondConnectionModel : Inherits Line

    Public Property type As Integer = 1

    Public Sub New(a As PointF3D, b As PointF3D)
        MyBase.New(a, b)
    End Sub

    Public Overrides Function Transform(camera As Camera) As Element3D
        Dim list = camera.Project(camera.Rotate({Me.A, Me.B})).ToArray
        Dim a = list(0)
        Dim b = list(1)
        Dim norm As New BondConnectionModel(a, b) With {
            .Stroke = Stroke,
            .type = type
        }
        Call norm.__init()
        Return norm
    End Function

    Public Overrides Sub Draw(g As IGraphics, rect As Drawing2D.GraphicsRegion, scaleX As LinearScale, scaleY As LinearScale)
        Dim size As Size = rect.Size
        Dim p1 As PointF = A.PointXY(size)
        Dim p2 As PointF = B.PointXY(size)

        p1 = New PointF(scaleX(p1.X), scaleY(p1.Y))
        p2 = New PointF(scaleX(p2.X), scaleY(p2.Y))

        Call DrawBond(g, p1, p2, Stroke, type)
    End Sub

    ''' <summary>
    ''' 绘制化学键
    ''' </summary>
    ''' <param name="g">绘图图面</param>
    ''' <param name="a">起始点</param>
    ''' <param name="b">结束点</param>
    ''' <param name="pen">绘制线条的画笔</param>
    ''' <param name="bondType">化学键类型（1=单键, 2=双键, 3=三键）</param>
    ''' <remarks>根据化学键类型绘制单条线或平行线</remarks>
    Public Shared Sub DrawBond(g As IGraphics, a As PointF, b As PointF, pen As Pen, bondType As Integer)
        ' 基础单键始终绘制
        g.DrawLine(pen, a, b)

        ' 如果需要绘制平行线（双键或三键）
        If bondType > 1 Then
            ' 计算从点A到点B的主向量
            Dim dx As Single = b.X - a.X
            Dim dy As Single = b.Y - a.Y

            ' 计算主向量的长度
            Dim length As Single = CSng(Math.Sqrt(dx * dx + dy * dy))
            Dim width As Single = pen.Width / (bondType + 1)

            pen = New Pen(pen.Color, width) With {
                .DashStyle = pen.DashStyle
            }

            ' 如果长度为零，则两点重合，无法计算垂直方向，直接返回
            If length = 0 Then Return

            ' 计算垂直于AB线的单位法向量（用于确定平行线的偏移方向）
            ' 法向量的分量通过交换dx, dy并取负其中一个，再归一化得到
            Dim nx As Single = -dy / length
            Dim ny As Single = dx / length

            ' 根据键型计算平行线的偏移量和条数
            Dim lineSpacing As Single = pen.Width * 2.5F ' 平行线间距，可根据画笔宽度调整
            Dim linesToDraw As Integer = bondType - 1 ' 需要额外绘制的平行线数量

            ' 绘制平行线
            For i As Integer = 1 To linesToDraw
                ' 计算当前平行线的偏移量
                ' 对于双键，偏移一条线；对于三键，偏移两条线（中线两侧各一）
                Dim offsetX As Single = nx * lineSpacing * (i - 0.5F * (bondType - 1))
                Dim offsetY As Single = ny * lineSpacing * (i - 0.5F * (bondType - 1))

                ' 计算平行线的起点和终点
                Dim a1 As New PointF(a.X + offsetX, a.Y + offsetY)
                Dim b1 As New PointF(b.X + offsetX, b.Y + offsetY)

                ' 绘制平行线
                g.DrawLine(pen, a1, b1)
            Next
        End If
    End Sub
End Class
