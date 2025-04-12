Imports System.Drawing
Imports Emily.gromacs
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports PDB_canvas
Imports PDB_canvas.StructModels
Imports SMRUCC.genomics.Data.RCSB.PDB

Public Module test_file

    Sub Main()
        Call Microsoft.VisualBasic.Drawing.SkiaDriver.Register()
        Call testModelRender()
        Call testDrawer()
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
        Dim camera As New Camera(gfx, New Point3D(45, 45, 45))

        Call PainterAlgorithm.SurfacePainter(gfx, camera, ribbonMeshes, drawPath:=True)
        Call gfx.Flush()
        Call gfx.Save("Z:/test_model_render.pdf")

        Pause()
    End Sub

    Sub testDrawer()
        Dim pdb As PDB = PDB.Load("G:\emily\data\8qne.pdb")
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
