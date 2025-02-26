Imports Emily.gromacs

Public Module test_file

    Sub Main()
        Dim rtp = rtpParser.readRtp("E:\emily\data\amber14sb.ff\aminoacids.rtp")
        Dim filter As New pdbFilter(rtp)
        Dim pdb = "F:\complex.1_backup.pdb".ReadAllLines
        Dim filter_pdb = filter.filter(pdb).ToArray

        Call filter_pdb.SaveTo("F:\complex.1.pdb")

        Pause()
    End Sub
End Module
