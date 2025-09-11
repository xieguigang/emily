Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class WriteListFile
        Public Const SHIFT_MARGIN As Integer = 2

        Private atomNumber As Integer = 0

        Friend halfwayPoint As Single = 0.0F

        Friend shiftBack As Single() = New Single(1) {}

        Friend nDummy As Integer = 0

        Private okField As Boolean = False

        Public Sub New(fileName As String, ensemble As Ensemble)
            Dim first = True
            Dim program = ensemble.Program
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            Using out = New PrintStream(fileName)
                If program = Ensemble.LIGPLOT Then
                    out.format("List of protein-ligand interactions" & vbLf & "-----------------------------------" & vbLf, New Object(-1) {})
                    out.format(vbLf, New Object(-1) {})
                    out.format(vbLf, New Object(-1) {})
                Else
                    out.format("List of atom-atom interactions across protein-protein interface" & vbLf & "---------------------------------------------------------------" & vbLf, New Object(-1) {})
                End If
                For i = 0 To pdbEntryList.Count - 1
                    Dim pdb = pdbEntryList(i)
                    writePDBInfo(out, pdb, program)
                    For iBondType = 1 To 5
                        If iBondType <> 3 Then
                            writeBonds(out, pdb, iBondType)
                        End If
                    Next
                Next
            End Using
        End Sub

        Private Sub writeBonds(out As PrintStream, pdb As PDBEntry, iBondType As Integer)
            Dim first = True
            Dim bondList As List(Of Bond) = pdb.BondList
            Dim iBond = 0
            For i = 0 To bondList.Count - 1
                Dim bond = bondList(i)
                Dim type = bond.Type
                If type = iBondType Then
                    If first Then
                        out.format(vbLf, New Object(-1) {})
                        out.format(vbLf, New Object(-1) {})
                        If type = 1 Then
                            out.format("Hydrogen bonds" & vbLf, New Object(-1) {})
                            out.format("--------------" & vbLf, New Object(-1) {})
                        ElseIf type = 2 Then
                            out.format("Non-bonded contacts" & vbLf, New Object(-1) {})
                            out.format("-------------------" & vbLf, New Object(-1) {})
                        ElseIf type = 4 Then
                            out.format("Salt bridges" & vbLf, New Object(-1) {})
                            out.format("------------" & vbLf, New Object(-1) {})
                        ElseIf type = 5 Then
                            out.format("Disulphides" & vbLf, New Object(-1) {})
                            out.format("-----------" & vbLf, New Object(-1) {})
                        End If
                        out.format(vbLf, New Object(-1) {})
                        out.format("       <----- A T O M   1 ----->", New Object(-1) {})
                        out.format("       <----- A T O M   2 ----->" & vbLf, New Object(-1) {})
                        out.format(vbLf, New Object(-1) {})
                        out.format("       Atom Atom Res  Res       ", New Object(-1) {})
                        out.format("       Atom Atom Res  Res" & vbLf, New Object(-1) {})
                        out.format("        no  name name  no  Chain", New Object(-1) {})
                        out.format("        no  name name  no  Chain  Distance" & vbLf, New Object(-1) {})
                        first = False
                    End If
                    iBond += 1
                    Dim atom1 = bond.getAtom(0)
                    Dim atom2 = bond.getAtom(1)
                    Dim residue1 = atom1.Residue
                    Dim residue2 = atom2.Residue
                    out.format("%5d %5d %s %s %s   %c ", New Object() {Convert.ToInt32(iBond), Convert.ToInt32(atom1.AtomNumber), atom1.AtomName, residue1.ResName, residue1.ResNum, Convert.ToChar(residue1.Chain)})
                    out.format("   --- ", New Object(-1) {})
                    out.format("%5d %s %s %s   %c ", New Object() {Convert.ToInt32(atom2.AtomNumber), atom2.AtomName, residue2.ResName, residue2.ResNum, Convert.ToChar(residue2.Chain)})
                    Dim bondLength = bond.Length
                    out.format(" %8.3f" & vbLf, New Object() {Convert.ToSingle(bondLength)})
                End If
            Next
        End Sub

        Private Sub writePDBInfo(out As PrintStream, pdb As PDBEntry, program As Integer)
            Dim pdbCode = pdb.PDBCode
            If Not ReferenceEquals(pdbCode, Nothing) AndAlso Not pdbCode.Equals("") Then
                out.format(vbLf, New Object(-1) {})
                out.format(vbLf, New Object(-1) {})
                out.format("PDB code: " & pdbCode & vbLf, New Object(-1) {})
                out.format("==============" & vbLf, New Object(-1) {})
            End If
        End Sub

        Public Overridable ReadOnly Property OK As Boolean
            Get
                Return okField
            End Get
        End Property
    End Class

End Namespace
