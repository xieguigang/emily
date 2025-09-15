
Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Legend
Imports Microsoft.VisualBasic.Data.ChartPlots.Plot3D.Device
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.d3js.scale
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing3D

#If NET48 Then
Imports Brushes = System.Drawing.Brushes
Imports DashStyle = System.Drawing.Drawing2D.DashStyle
#Else
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
Imports DashStyle = Microsoft.VisualBasic.Imaging.DashStyle
#End If

Public Class AtomModel : Inherits ShapePoint

    Public Property IsResidue As Boolean
    Public Property fontSize As Double

    Public Overrides Sub Draw(g As IGraphics, rect As GraphicsRegion, scaleX As LinearScale, scaleY As LinearScale)
        Dim praw As PointF = GetPosition(rect.Size)
        Dim pscale As New PointF(scaleX(praw.X) - Size.Width / 2, scaleY(praw.Y) - Size.Height / 2)
        Dim font As Font = Me.font(g.Dpi)

        Call g.DrawLegendShape(pscale, Size, Style, Fill)

        If IsResidue Then
            With TextLocation(g, rect, scaleX, scaleY)
                Call DrawText(g, .X, .Y)
            End With
        End If
    End Sub

    Public Function TextLocation(g As IGraphics, rect As GraphicsRegion, scaleX As LinearScale, scaleY As LinearScale) As PointF
        Dim praw As PointF = GetPosition(rect.Size)
        Dim pscale As New PointF(scaleX(praw.X) - Size.Width / 2, scaleY(praw.Y) - Size.Height / 2)
        Dim labelSize As SizeF = TextSize(g)
        Dim lx = (Size.Width - labelSize.Width) / 2 + pscale.X
        Dim ly = (Size.Height - labelSize.Height) / 2 + pscale.Y

        Return New PointF(lx, ly)
    End Function

    Public Function TextSize(g As IGraphics) As SizeF
        Return g.MeasureString(Label, font(g.Dpi))
    End Function

    Public Function font(dpi As Integer) As Font
        Return New Font(FontFace.CambriaMath, FontFace.PointSizeScale(fontSize, dpi))
    End Function

    Public Sub DrawText(g As IGraphics, lx As Single, ly As Single)
        Call g.DrawString(Label, font(g.Dpi), Brushes.Black, New PointF(lx, ly))
    End Sub

    Public Overrides Function Transform(camera As Camera) As Element3D
        Return New AtomModel With {
            .Fill = Fill,
            .fontSize = fontSize,
            .IsResidue = IsResidue,
            .Label = Label,
            .Location = camera.Project(camera.Rotate(Location)),
            .Size = Size,
            .Style = Style
        }
    End Function

End Class