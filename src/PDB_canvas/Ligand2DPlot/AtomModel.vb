
Imports System.Drawing
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Legend
Imports Microsoft.VisualBasic.Data.ChartPlots.Plot3D.Device
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.d3js.scale
Imports Microsoft.VisualBasic.Imaging.Drawing2D

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
        Dim font As New Font(FontFace.CambriaMath, FontFace.PointSizeScale(fontSize, g.Dpi))

        Call g.DrawLegendShape(pscale, Size, Style, Fill)

        If IsResidue Then
            Dim labelSize As SizeF = g.MeasureString(Label, font)
            Dim lx = (Size.Width - labelSize.Width) / 2 + pscale.X
            Dim ly = (Size.Height - labelSize.Height) / 2 + pscale.Y

            Call g.DrawString(Label, font, Brushes.Black, New PointF(lx, ly))
        End If
    End Sub

End Class