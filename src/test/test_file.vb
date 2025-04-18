Imports System.Drawing
Imports Emily.gromacs
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
Imports Microsoft.VisualBasic.Data.ChartPlots.Plot3D
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Linq
Imports PDB_canvas
Imports PDB_canvas.StructModels
Imports SMRUCC.genomics.Data.RCSB.PDB

Public Module test_file

    Sub Main()
        Call Microsoft.VisualBasic.Drawing.SkiaDriver.Register()
        Call testMetadata()
        ' Call testModelRender()
        Call testDrawer()
    End Sub

    Sub testMetadata()
        Dim pdb As PDB = PDB.Load("G:\pdb2ki6.ent")
        Dim pdb2 As PDB = PDB.Load("G:\emily\data\XC_1184.pdb")

        Pause()
    End Sub

    Sub testModelRender()
        Dim generator As New Sheet({
            New Point3D With {.X = 0, .Y = 0, .Z = 0},
            New Point3D With {.X = 1, .Y = 2, .Z = 3},
            New Point3D With {.X = 3, .Y = 1, .Z = 2}
        }, Color.Red)
        Dim ribbonMeshes = generator.GenerateSheetRibbonModel(
                                                       thickness:=0.5,
                                                       width:=1.0,
                                                       segments:=100)

        Dim gfx As New PdfGraphics(2000, 2000)
        Dim camera As New Camera(gfx, New Point3D(45, 45, 45), viewDistance:=1)

        Call PainterAlgorithm.SurfacePainter(gfx, camera, ribbonMeshes, drawPath:=True)
        Call gfx.Flush()
        Call gfx.Save("Z:/test_model_render.pdf")

        Dim colors = Designer.GetColors("paper", ribbonMeshes.Count + 1)
        Dim createData As Func(Of Surface, Integer, Serial3D) =
            Function(a, i)
                Return New Serial3D With {
            .Color = colors(i),
            .PointSize = 10,
            .Shape = Microsoft.VisualBasic.Data.ChartPlots.Graphic.Legend.LegendStyles.Circle,
            .Title = $"mesh_{i + 1}",
            .Points = a.vertices.Select(Function(v) New NamedValue(Of Point3D)("", v)).ToArray
        }
            End Function


        Dim scatter = ribbonMeshes.Select(createData).ToArray

        Call scatter.Plot(camera, showHull:=False, showLegend:=False, driver:=Microsoft.VisualBasic.Imaging.Driver.Drivers.PDF).Save("Z:/scatter_visual.pdf")

        Pause()
    End Sub

    Sub testDrawer()
        Dim pdb As PDB = PDB.Load("Z:\pdb2ku2.ent")
        Dim img = DrawingPDB.MolDrawing(pdb, "3000,3000")

        Call img.Save("G:\emily\data\8qne_pdb.png")
    End Sub

    Sub testFilter()
        Dim rtp = rtpParser.readRtp("E:\emily\data\amber14sb.ff\aminoacids.rtp")
        Dim filter As New pdbFilter(rtp)
        Dim pdb = "F:\complex.1_backup.pdb".ReadAllLines
        Dim filter_pdb = filter.filter(pdb).ToArray

        Call filter_pdb.SaveTo("F:\complex.1.pdb")

        Pause()
    End Sub
End Module
