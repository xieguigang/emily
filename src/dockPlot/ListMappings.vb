Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class ListMappings
        Public Const RASMOL As Integer = 0

        Public Const PYMOL As Integer = 1

        Public Shared MAX_MESSAGES As Integer = 10

        Public nMessages As Integer = 0

        Public messages As String() = New String(MAX_MESSAGES - 1) {}

        Public Sub New(ensemble As Ensemble, out As PrintStream, viewer As Integer)
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            For iPDB = 0 To pdbEntryList.Count - 1
                Dim pdb = pdbEntryList(iPDB)
                Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
                For iMol = 0 To moleculeList.Count - 1
                    Dim molecule = moleculeList(iMol)
                    Dim moleculeType = molecule.MoleculeType
                    Dim residueList As List(Of Residue) = molecule.ResidueList
                    For j = 0 To residueList.Count - 1
                        Dim residue = residueList(j)
                        If nMessages < MAX_MESSAGES Then
                            If residue.ResidueLabel IsNot Nothing Then
                                Dim residueLabel = residue.ResidueLabel.Text
                                Dim openBracket = residueLabel.IndexOf("("c)
                                Dim closeBracket = residueLabel.IndexOf(")"c)
                                If openBracket > -1 AndAlso closeBracket > -1 AndAlso closeBracket > openBracket Then
                                    Dim fullChain = residueLabel.Substring(openBracket + 1, closeBracket - (openBracket + 1))
                                    Dim idLen = fullChain.Length
                                    If idLen > 1 Then
                                        Dim message As String = "Chain " & fullChain & " mapped to " & residue.Chain.ToString()
                                        addToMessages(message)
                                    End If
                                End If
                            End If
                        End If
                        If moleculeType = 1 AndAlso nMessages < MAX_MESSAGES Then
                            Dim pdbResName As String = residue.ResName.Trim() & residue.ResNum.Trim() & "(" & residue.Chain.ToString() & ")"
                            Dim residueLabel As String = Nothing
                            If residue.ResidueLabel IsNot Nothing Then
                                residueLabel = residue.ResidueLabel.Text
                            End If
                            Dim pdbLen = pdbResName.Length
                            If Not ReferenceEquals(residueLabel, Nothing) Then
                                Dim labelLen = residueLabel.Length
                                If pdbLen <> labelLen Then
                                    Dim message = "Residue " & residueLabel & " mapped to " & pdbResName
                                    addToMessages(message)
                                End If
                            End If
                        End If
                    Next
                Next
            Next
            If nMessages > 0 Then
                If viewer = 0 Then
                    writeRasMol(out)
                Else
                    writePyMOL(out)
                End If
            End If
        End Sub

        Private Sub addToMessages(message As String)
            If nMessages >= MAX_MESSAGES Then
                Return
            End If
            If nMessages = MAX_MESSAGES - 1 Then
                message = "+ more"
            End If
            Dim found = False
            Dim i = 0

            While i < nMessages AndAlso Not found
                If message.Equals(messages(i)) Then
                    found = True
                End If

                i += 1
            End While
            If Not found Then
                messages(nMessages) = message
                nMessages += 1
            End If
        End Sub

        Private Sub writeRasMol(out As PrintStream)
            out.format("echo "" """ & vbLf, New Object(-1) {})
            out.format("echo ""* Mappings in the PDB structure:""" & vbLf, New Object(-1) {})
            For i = 0 To nMessages - 1
                out.format("echo ""    %s""" & vbLf, New Object() {messages(i)})
            Next
            out.format("echo "" """ & vbLf, New Object(-1) {})
        End Sub

        Private Sub writePyMOL(out As PrintStream)
            out.format("log -->" & vbLf, New Object(-1) {})
            out.format("log --> * Mappings in the PDB structure:" & vbLf, New Object(-1) {})
            For i = 0 To nMessages - 1
                out.format("log -->     %s" & vbLf, New Object() {messages(i)})
            Next
            out.WriteLine("log -->" & vbLf, New Object(-1) {})
        End Sub
    End Class

End Namespace
