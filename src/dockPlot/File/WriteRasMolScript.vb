Imports ligplus.ligplus
Imports ligplus.models
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace file

    Friend Class WriteRasMolScript
        Public Shared SCRIPT As Integer = 0

        Public Shared COORDS As Integer = 1

        Public Shared COORDS_COPY As Integer = 2

        Public Shared ABODY_SCRIPT1 As Integer = 3

        Public Shared ABODY_SCRIPT2 As Integer = 4

        Public Shared viewer As Integer = 0

        Private ensemble As Ensemble


        Private antibody As Boolean = False

        Private program As Integer

        Public Sub New(ensemble As Ensemble)
            Me.ensemble = ensemble
            program = ensemble.Program
            antibody = ensemble.Antibody
        End Sub

        Public Sub write(fileName As String)
            Dim atomNumber = 0

            Using out = New PrintStream(fileName)
                writeHeaders(out)
                listObjects(out)

                Dim listMappings As ListMappings = New ListMappings(ensemble, out, viewer)

                If antibody Then
                    atomNumber = writeCoords(out, atomNumber, ABODY_SCRIPT1)
                    writeCoords(out, atomNumber, ABODY_SCRIPT2)
                Else
                    writeCoords(out, atomNumber, SCRIPT)
                End If
                atomNumber = 0
                closeScript(out)
                atomNumber = writeCoords(out, atomNumber, COORDS)
                If antibody Then
                    writeCoords(out, atomNumber, COORDS_COPY)
                End If
            End Using
        End Sub

        Private Sub closeScript(out As PrintStream)
            out.format(vbLf, New Object(-1) {})
            out.format("select all" & vbLf, New Object(-1) {})
            out.format("set picking distance" & vbLf, New Object(-1) {})
            out.format("exit" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
        End Sub

        Private Sub listObjects(out As PrintStream)
            Dim atomNumber = 0
            out.format("echo ""* User-defined sets present in structure:-""" & vbLf, New Object(-1) {})
            out.format("echo "" """ & vbLf, New Object(-1) {})
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            For i = 0 To pdbEntryList.Count - 1
                Dim pdb = pdbEntryList(i)
                out.format("echo ""  prot%02d = protein residues for %s""" & vbLf, New Object() {Convert.ToInt32(i + 1), pdb.PDBCode})
                If program = Ensemble.LIGPLOT Then
                    out.format("echo ""  lig%02d  = ligand for %s""" & vbLf, New Object() {Convert.ToInt32(i + 1), pdb.PDBCode})
                End If
            Next
        End Sub

        Private Function writeCoords(out As PrintStream, atomNumber As Integer, mode As Integer) As Integer
            Dim hydrophobicsList As New List(Of String)
            Dim dummyResNo = 0
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            For iPDB = 0 To pdbEntryList.Count - 1
                Dim firstAtom = atomNumber + 1
                Dim lastAtom = atomNumber + 1
                Dim firstLigandAtom = atomNumber + 1
                Dim lastLigandAtom = atomNumber + 1
                Dim pdb = pdbEntryList(iPDB)
                Dim proteinName = String.Format("prot{0:D2}", New Object() {Convert.ToInt32(iPDB + 1)})
                Dim ligandName = String.Format("lig{0:D2}", New Object() {Convert.ToInt32(iPDB + 1)})
                If mode = COORDS Then
                    out.format("REMARK PDB CODE: %s" & vbLf, New Object() {pdb.PDBCode})
                End If
                Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
                For iMol = 0 To moleculeList.Count - 1
                    Dim molecule = moleculeList(iMol)
                    Dim moleculeType = molecule.MoleculeType
                    Dim firstMoleculeAtom = atomNumber + 1
                    Dim residueList As List(Of Residue) = molecule.ResidueList
                    For j = 0 To residueList.Count - 1
                        Dim residue = residueList(j)
                        Dim wanted = True
                        If residue.ResName.Equals("DUM") Then
                            wanted = False
                        End If
                        Dim atomList As List(Of Atom) = residue.AtomList
                        Dim first = True
                        Dim k = 0

                        While k < atomList.Count AndAlso wanted
                            Dim atom = atomList(k)
                            If atom.InUse Then
                                lastAtom = Threading.Interlocked.Increment(atomNumber)
                                If mode = COORDS OrElse mode = COORDS_COPY Then
                                    Dim element = atom.Element
                                    If element.Length = 1 Then
                                        element = " " & element(0).ToString()
                                    End If
                                    out.format("%s%5d %s %s %c%s   %8.3f%8.3f%8.3f%6.2f%6.2f          %s" & vbLf, New Object() {atom.KeyWord, Convert.ToInt32(atomNumber), atom.AtomName, residue.ResName, Convert.ToChar(residue.Chain), residue.ResNum, Convert.ToSingle(atom.getOriginalCoord(0)), Convert.ToSingle(atom.getOriginalCoord(1)), Convert.ToSingle(atom.getOriginalCoord(2)), Convert.ToSingle(atom.Occupancy), Convert.ToSingle(atom.BValue), element})
                                ElseIf first AndAlso (mode = SCRIPT OrElse mode = ABODY_SCRIPT2) Then
                                    out.format("select atomno = %d" & vbLf, New Object() {Convert.ToInt32(atomNumber)})
                                    If residue.Chain <> " "c Then
                                        out.format("label %%n%%r(%%c)" & vbLf, New Object(-1) {})
                                    Else
                                        out.format("label %%n%%r" & vbLf, New Object(-1) {})
                                    End If
                                    first = False
                                End If
                            End If

                            k += 1
                        End While
                    Next
                    If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 Then
                        out.format("select (atomno >= %d & atomno <= %d)" & vbLf, New Object() {Convert.ToInt32(firstMoleculeAtom), Convert.ToInt32(atomNumber)})
                        If moleculeType = 1 AndAlso mode <> ABODY_SCRIPT1 Then
                            out.format("wireframe 0.5" & vbLf, New Object(-1) {})
                        ElseIf moleculeType = 4 Then
                            out.format("spacefill 0.5" & vbLf, New Object(-1) {})
                            out.format("colour cyan" & vbLf, New Object(-1) {})
                        ElseIf moleculeType = 2 AndAlso firstMoleculeAtom = atomNumber Then
                            out.format("spacefill 0.5" & vbLf, New Object(-1) {})
                            out.format("colour green" & vbLf, New Object(-1) {})
                        ElseIf moleculeType = 3 Then
                            Dim group As String = "(atomno >= " & firstMoleculeAtom.ToString() & " & atomno <= " & atomNumber.ToString() & ")"
                            hydrophobicsList.Add(group)
                        ElseIf mode = ABODY_SCRIPT1 Then
                            out.format("spacefill 0.5" & vbLf, New Object(-1) {})
                        Else
                            out.format("wireframe 0.2" & vbLf, New Object(-1) {})
                        End If
                    ElseIf mode = ABODY_SCRIPT2 Then
                        out.format("select (atomno >= %d & atomno <= %d)" & vbLf, New Object() {Convert.ToInt32(firstMoleculeAtom), Convert.ToInt32(atomNumber)})
                        out.format("wireframe 0.2" & vbLf, New Object(-1) {})
                        If moleculeType = 3 Then
                            out.format("spacefill 0.2" & vbLf, New Object(-1) {})
                        End If
                        Dim colour = molecule.MoleculeColour
                        If Not colour.IsEmpty Then
                            Dim red As Integer = colour.R
                            Dim green As Integer = colour.G
                            Dim blue As Integer = colour.B
                            out.format("colour [%d,%d,%d]" & vbLf, New Object() {Convert.ToInt32(red), Convert.ToInt32(green), Convert.ToInt32(blue)})
                        Else
                            out.format("colour white" & vbLf, New Object(-1) {})
                        End If
                    End If
                    If moleculeType = 1 Then
                        firstLigandAtom = firstMoleculeAtom
                        lastLigandAtom = atomNumber
                    End If
                Next
                Dim bondList As List(Of Bond) = pdb.BondList
                Dim i As Integer
                For i = 0 To bondList.Count - 1
                    Dim bond = bondList(i)
                    Dim type = bond.Type
                    If type = 1 Then
                        dummyResNo += 1
                        Dim resNum = String.Format("{0,4:D} ", New Object() {Convert.ToInt32(dummyResNo)})
                        Dim atom1 = bond.getAtom(0)
                        Dim atom2 = bond.getAtom(1)
                        Dim x = 0.0F
                        Dim y = 0.0F
                        Dim z = 0.0F
                        Dim xMid = 0.0F
                        Dim yMid = 0.0F
                        Dim zMid = 0.0F
                        For iAtom = 0 To 2
                            atomNumber += 1
                            If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 Then
                                If iAtom = 2 Then
                                    out.format("select atomno = %d" & vbLf, New Object() {Convert.ToInt32(atomNumber)})
                                    out.format("label ""%s""" & vbLf, New Object() {bond.BondLabel.Text})
                                End If
                            ElseIf iAtom <> 2 Then
                                Dim atom As Atom
                                If iAtom = 0 Then
                                    atom = atom1
                                Else
                                    atom = atom2
                                End If
                                x = atom.getOriginalCoord(0)
                                y = atom.getOriginalCoord(1)
                                z = atom.getOriginalCoord(2)
                                xMid += x
                                yMid += y
                                zMid += z
                            ElseIf iAtom = 2 Then
                                x = xMid / 2.0F
                                y = yMid / 2.0F
                                z = zMid / 2.0F
                            End If
                            If mode = COORDS Then
                                out.format("HETATM%5d %s %s %c%s   %8.3f%8.3f%8.3f%6.2f%6.2f          %s" & vbLf, New Object() {Convert.ToInt32(atomNumber), " C  ", "DUM", Convert.ToChar("X"c), resNum, Convert.ToSingle(x), Convert.ToSingle(y), Convert.ToSingle(z), Convert.ToDouble(1.0R), Convert.ToDouble(10.0R), "C"})
                            End If
                        Next
                        If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 Then
                            out.format("select (atomno >= %d & atomno <= %d)" & vbLf, New Object() {Convert.ToInt32(atomNumber - 2), Convert.ToInt32(atomNumber)})
                            out.format("wireframe on" & vbLf, New Object(-1) {})
                            out.format("colour cyan" & vbLf, New Object(-1) {})
                        End If
                    End If
                Next
                If mode = SCRIPT Then
                    out.format("define lig%02d (atomno >= %d & atomno <= %d)" & vbLf, New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(firstLigandAtom), Convert.ToInt32(lastLigandAtom)})
                    out.format("define prot%02d (atomno >= %d & atomno <= %d)" & vbLf, New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(firstAtom), Convert.ToInt32(lastAtom)})
                    out.format("define prot%02d (prot%02d & !lig%02d)" & vbLf, New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(iPDB + 1), Convert.ToInt32(iPDB + 1)})
                End If
                If hydrophobicsList.Count > 0 Then
                    For i = 0 To hydrophobicsList.Count - 1
                        Dim range = hydrophobicsList(i)
                        If i = 0 Then
                            out.format("define hydrophobics %s" & vbLf, New Object() {range})
                        Else
                            out.format("define hydrophobics hydrophobics | %s" & vbLf, New Object() {range})
                        End If
                    Next
                    out.format("select hydrophobics" & vbLf, New Object(-1) {})
                    out.format("dots on" & vbLf, New Object(-1) {})
                    If mode = SCRIPT Then
                        out.format("spacefill 0.2" & vbLf, New Object(-1) {})
                    End If
                    out.format("wireframe 0.2" & vbLf, New Object(-1) {})
                End If
            Next
            Return atomNumber
        End Function

        Private Sub writeHeaders(out As PrintStream)
            Dim today = Date.Now
            out.format("#!rasmol -script" & vbLf, New Object(-1) {})
            out.format("# Generated by LigPlus v.2.3.1" & vbLf, New Object(-1) {})
            out.format("# Date: %s" & vbLf, New Object() {today.ToString()})
            out.format("# R A Laskowski, Aug 2009" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format("zap" & vbLf, New Object(-1) {})
            out.format("load inline" & vbLf, New Object(-1) {})
            out.format("background black" & vbLf, New Object(-1) {})
            out.format("set ambient 60" & vbLf, New Object(-1) {})
            out.format("set specular off" & vbLf, New Object(-1) {})
            out.format("slab off" & vbLf, New Object(-1) {})
            out.format("set bonds off" & vbLf, New Object(-1) {})
            out.format("set axes off" & vbLf, New Object(-1) {})
            out.format("set boundingbox off" & vbLf, New Object(-1) {})
            out.format("set unitcell off" & vbLf, New Object(-1) {})
            out.format("set bondmode and" & vbLf, New Object(-1) {})
            out.format("dots off" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format("# Initialise colours" & vbLf, New Object(-1) {})
            out.format("select all" & vbLf, New Object(-1) {})
            out.format("colour bonds none" & vbLf, New Object(-1) {})
            out.format("colour backbone none" & vbLf, New Object(-1) {})
            out.format("colour hbonds none" & vbLf, New Object(-1) {})
            out.format("colour ssbonds none" & vbLf, New Object(-1) {})
            out.format("colour ribbons none" & vbLf, New Object(-1) {})
            out.format("wireframe off" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format("select all" & vbLf, New Object(-1) {})
            out.format("colour cpk" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format("echo ""   +----------+""" & vbLf, New Object(-1) {})
            out.format("echo ""   | LigPlot+ |""" & vbLf, New Object(-1) {})
            out.format("echo ""   +----------+""" & vbLf, New Object(-1) {})
            out.format("echo "" """ & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
        End Sub
    End Class

End Namespace
