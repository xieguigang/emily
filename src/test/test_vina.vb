Imports Emily.PDB_Canvas
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Language
Imports SMRUCC.genomics.Data.RCSB.PDB

Module test_vina

    Sub pdb_writer()
        Dim docking As New ComplexGenerator("AAA1", "BioDeep_0000000001")
        Dim i As i32 = 1

        For Each stat As String In docking.CreateComplexList(
            "G:\emily\test\vina\result.pdbqt".ReadAllText,
            "G:\emily\test\vina\protein.pdbqt".ReadAllText
            )

            Call stat.SaveTo($"./{++i}.pdb")
        Next
    End Sub

    Sub test_pdbqt_render()
        Dim pdb As PDB = PDB.Load("G:\emily\src\test\bin\x64\Debug\net8.0\simple.pdbqt")
        Dim theme As New Theme With {.padding = "padding: 10% 10% 10% 10%;"}
        Dim render As New Ligand2DPlot(pdb, pdb.GetLigandReference, theme)
        render.TextNudge = False
        render.CalculateMaxPlainView()
        Dim image = render.Plot("3600,2400").AsGDIImage

        Call image.SaveAs($"./draw_simple_pdbqt.png")

        Pause()
    End Sub
End Module
