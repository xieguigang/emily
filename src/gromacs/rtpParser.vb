Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Language

Namespace gromacs

    Public Module rtpParser

        Public Function readRtp(file As String) As rtp
            Return New rtp With {
                .residues = file _
                    .IterateAllLines _
                    .read_residues _
                    .ToArray
            }
        End Function

        ReadOnly rtp_types As Index(Of String) = {"bondedtypes", "atoms", "bonds", "impropers"}

        <Extension>
        Private Iterator Function read_residues(data As IEnumerable(Of String)) As IEnumerable(Of rtpResidue)
            Dim parser As String = Nothing
            Dim residue As rtpResidue = Nothing
            Dim atoms As New List(Of atom)
            Dim bonds As New List(Of bond)
            Dim impropers As New List(Of improper)

            ' removes comment text via regexp
            ' ;.+
            For Each line As String In From str As String
                                       In data
                                       Select str.StringReplace(";.+", "")

                If line.IsPattern("\[.+\]") Then
                    parser = line.GetStackValue("[", "]").Trim

                    If Not parser Like rtp_types Then
                        ' new residue
                        If Not residue Is Nothing Then
                            residue.atoms = atoms.PopAll
                            residue.bonds = bonds.PopAll
                            residue.impropers = impropers.PopAll

                            Yield residue
                        End If

                        residue = New rtpResidue With {
                            .residue = parser
                        }
                    End If

                    Continue For
                End If

                Dim t As String() = Strings.Trim(line).StringSplit("\s+")

                Select Case parser
                    Case "bondedtypes"
                        ' do nothing
                    Case "atoms"
                        Call atoms.Add(New atom With {
                            .name = t(0),
                            .type = t(1),
                            .partial_charge = Val(t(2)),
                            .mass = Val(t(3))
                        })
                    Case "bonds"
                        Call bonds.Add(New bond With {
                            .u = t(0),
                            .v = t(1)
                        })
                    Case "impropers"
                        Call impropers.Add(New improper With {
                            .dihedral_angles = t
                        })
                    Case Else
                        Throw New NotImplementedException(parser)
                End Select
            Next
        End Function
    End Module
End Namespace