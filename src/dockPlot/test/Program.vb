Imports ligplus.file

Module Program

    Const dock_pdb = "G:\emily\data\8qne.pdb"

    Sub Main(args As String())
        Dim reader As New ReadPDBFile(dock_pdb)

        Pause()
    End Sub
End Module