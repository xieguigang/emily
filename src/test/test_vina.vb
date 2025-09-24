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
End Module
