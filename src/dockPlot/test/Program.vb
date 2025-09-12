Imports ligplus.file
Imports ligplus.ligplus
Imports ligplus.pdb

Module Program

    Const dock_pdb = "G:\emily\data\8qne.pdb"

    Sub Main(args As String())
        Dim reader As New ReadPDBFile(dock_pdb)

        For Each ligend As HetGroup In reader.PDBEntry.HetGroupList
            Call Console.WriteLine(ligend.ToString)
        Next

        Dim ensemble As New Ensemble(New Properties, 0)

        Call ensemble.addPDBEntry(reader.PDBEntry)

        Pause()
    End Sub
End Module