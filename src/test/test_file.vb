Imports System.Drawing
Imports Emily.gromacs
Imports PDB_canvas
Imports SMRUCC.genomics.Data.RCSB.PDB

Public Module test_file

    Sub Main()
        Call Microsoft.VisualBasic.Drawing.SkiaDriver.Register()
        Call testDrawer()
    End Sub

    Sub testDrawer()
        Dim pdb As PDB = PDB.Load("G:\emily\data\XC_1184.pdb")
        Dim img = DrawingPDB.MolDrawing(pdb, "3000,3000")

        Call img.Save("G:\emily\data\XC_1184.png")
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
