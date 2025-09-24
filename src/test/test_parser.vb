Imports SMRUCC.genomics.Data
Imports SMRUCC.genomics.Data.RCSB.PDB

Module test_parser

    Sub test_reader()
        Dim pdbqt As PDB = RCSB.PDB.PDB.Load("G:\emily\test\vina\result.pdbqt")
        Dim pdb As PDB = PDB.Load("G:\emily\data\8qne.pdb")
    End Sub
End Module
