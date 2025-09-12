Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports SMRUCC.genomics.Data.RCSB.PDB

Public Class Ligand2DPlot : Inherits Plot

    ReadOnly pdb As PDB

    Sub New(pdb As PDB, theme As Theme)
        Call MyBase.New(theme)
        Me.pdb = pdb
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)

    End Sub
End Class
