Imports Microsoft.VisualBasic.ComponentModel.Collection

Namespace gromacs

    ''' <summary>
    ''' removes the un-expected atoms from the pdb file by matches rtp file atoms
    ''' </summary>
    Public Class pdbFilter

        ReadOnly residue_atoms As New Dictionary(Of String, Index(Of String))

        Sub New(rtp As rtp)
            For Each res As rtpResidue In rtp.residues
                Call residue_atoms.Add(res.residue, res.atoms.Select(Function(a) a.name).Indexing)
            Next
        End Sub

        Public Iterator Function filter(pdb As IEnumerable(Of String)) As IEnumerable(Of String)
            Dim atoms As Index(Of String)

            For Each line As String In pdb
                If InStr(line, "ATOM ") = 1 Then
                    ' 0      1     2   3   
                    ' ATOM   2354  HA  GLU C  38      60.152 124.822 143.752  0     0 1.90          0.00
                    Dim t = line.StringSplit("\s+")
                    Dim atom = t(2)
                    Dim res = t(3)

                    If residue_atoms.ContainsKey(res) Then
                        atoms = residue_atoms(res)

                        If Not atom Like atoms Then
                            Call $"Atom '{atom}' in residue '{res}' was not found in rtp entry '{res}' with {atoms.Count} atoms while sorting atoms.".Warning
                            Call VBDebugger.EchoLine(line)
                            Continue For
                        End If
                    End If
                End If

                Yield line
            Next
        End Function

    End Class
End Namespace