Imports ligplus.file
Imports ligplus.ligplus
Imports ligplus.pdb

Module Program

    Const dock_pdb = "G:\emily\data\8qne.pdb"

    Sub Main(args As String())
        Dim config As Properties = Properties.Load("G:\emily\src\dockPlot\default.txt")
        Dim app As New LigPlusFrame()

        Call app.OpenPDBFileMenuItemActionPerformed(dock_pdb)

        For Each ligend As HetGroup In LigPlusFrame.pdb.HetGroupList
            Call Console.WriteLine(ligend.ToString)
        Next

        Dim ensemble As New Ensemble(config, 0)

        Call ensemble.addPDBEntry(LigPlusFrame.pdb)

        Pause()
    End Sub
End Module