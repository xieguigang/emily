Imports Microsoft.VisualBasic.Text
Imports System
Imports System.Collections.Generic
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Friend Class WritePyMOLScript
        Public Shared SCRIPT As Integer = 0

        Public Shared COORDS As Integer = 1

        Public Shared COORDS_COPY As Integer = 2

        Public Shared ABODY_SCRIPT1 As Integer = 3

        Public Shared ABODY_SCRIPT2 As Integer = 4

        Public Shared NATOM_SPLIT As Integer = 100

        Public Shared viewer As Integer = 1

        Private ensemble As Ensemble



        Private antibody As Boolean = False

        Private program As Integer

        Dim coordsFile As String

        Public Sub New(ensemble As Ensemble, coordsFile As String)
            Dim atomNumber = 0

            Me.ensemble = ensemble
            Me.coordsFile = coordsFile
            program = ensemble.Program
            antibody = ensemble.Antibody
            Using out = New PrintStream(coordsFile)
                atomNumber = writeCoords(out, atomNumber, COORDS)
                If antibody Then
                    writeCoords(out, atomNumber, COORDS_COPY)
                End If
            End Using

        End Sub

        Public Sub write(scriptFile As String)
            Dim atomNumber = 0

            Using out = New PrintStream(scriptFile)
                writeHeaders(out, coordsFile)
                If antibody Then
                    atomNumber = 0
                    atomNumber = writeCoords(out, atomNumber, ABODY_SCRIPT1)
                    writeCoords(out, atomNumber, ABODY_SCRIPT2)
                Else
                    atomNumber = 0
                    writeCoords(out, atomNumber, SCRIPT)
                End If
                atomNumber = 0
                writeEndLines(out)

                Dim listMappings As ListMappings = New ListMappings(ensemble, out, viewer)

            End Using
        End Sub

        Private Function writeCoords(out As PrintStream, atomNumber As Integer, mode As Integer) As Integer
            Dim dummyResNo = 0
            Dim nHbond = 0
            Dim haveLigand = False
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            For iPDB = 0 To pdbEntryList.Count - 1
                Dim metalsList As List(Of String) = New List(Of String)()
                Dim hydrophobicsList As List(Of String) = New List(Of String)()
                Dim allList As List(Of String) = New List(Of String)()
                Dim coloursList As List(Of String) = New List(Of String)()
                Dim moleculeColourList As List(Of String) = New List(Of String)()
                Dim nColours = 0
                Dim haveWaters = False
                Dim firstProteinAtom = atomNumber + 1
                Dim lastProteinAtom = atomNumber + 1
                Dim firstLigandAtom = atomNumber + 1
                Dim lastLigandAtom = atomNumber + 1
                Dim pdb = pdbEntryList(iPDB)
                If mode = COORDS Then
                    out.format("REMARK PDB CODE: %s" & vbLf, New Object() {pdb.PDBCode})
                End If
                Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
                For iMol = 0 To moleculeList.Count - 1
                    Dim moleculeColour As String
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
                            atomNumber += 1
                            atom.AtomNumber = atomNumber
                            If mode = COORDS OrElse mode = COORDS_COPY Then
                                Dim element = atom.Element
                                If element.Length = 1 Then
                                    element = " " & element(0).ToString()
                                End If
                                out.format("%s%5d %s %s %c%s   %8.3f%8.3f%8.3f%6.2f%6.2f          %s" & vbLf, New Object() {atom.KeyWord, Convert.ToInt32(atomNumber), atom.AtomName, residue.ResName, Convert.ToChar(residue.Chain), residue.ResNum, Convert.ToSingle(atom.getOriginalCoord(0)), Convert.ToSingle(atom.getOriginalCoord(1)), Convert.ToSingle(atom.getOriginalCoord(2)), Convert.ToSingle(atom.Occupancy), Convert.ToSingle(atom.BValue), element})
                            ElseIf first AndAlso (mode = SCRIPT OrElse mode = ABODY_SCRIPT2) Then
                                If residue.Chain <> " "c Then
                                    out.format("label id %d, ""%%s%%s(%%s)"" %% (resn, resi, chain)" & vbLf, New Object() {Convert.ToInt32(atomNumber)})
                                Else
                                    out.format("label id %d, ""%%s%%s"" %% (resn, resi)" & vbLf, New Object() {Convert.ToInt32(atomNumber)})
                                End If
                                first = False
                            End If

                            k += 1
                        End While
                    Next
                    Dim colour = molecule.MoleculeColour
                    If Not colour.IsEmpty Then
                        Dim red = colour.R / 255.0F
                        Dim green = colour.G / 255.0F
                        Dim blue = colour.B / 255.0F
                        moleculeColour = "[" & red.ToString() & "," & green.ToString() & "," & blue.ToString() & "]"
                    Else
                        moleculeColour = "[ 0.0, 0.0, 0.0 ]"
                    End If
                    Dim iColour = -1
                    If moleculeColourList.Count > 0 Then
                        For iCol = 0 To moleculeColourList.Count - 1
                            Dim mColour = moleculeColourList(iCol)
                            If moleculeColour.Equals(mColour) Then
                                iColour = iCol
                            End If
                        Next
                    End If
                    If iColour = -1 AndAlso (mode = SCRIPT OrElse mode = ABODY_SCRIPT1) Then
                        nColours += 1
                        out.format("set_color col%02d, %s" & vbLf, New Object() {Convert.ToInt32(nColours), moleculeColour})
                        moleculeColourList.Add(moleculeColour)
                        iColour = nColours - 1
                    End If
                    Dim colourNo As String = "" & iColour.ToString()
                    If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 Then
                        If moleculeType = 1 AndAlso mode <> ABODY_SCRIPT1 Then
                            haveLigand = True
                            firstLigandAtom = firstMoleculeAtom
                            lastLigandAtom = atomNumber
                        ElseIf moleculeType = 2 OrElse moleculeType = 3 OrElse mode = ABODY_SCRIPT1 AndAlso moleculeType = 1 Then
                            lastProteinAtom = atomNumber
                        End If
                        If moleculeType = 4 Then
                            haveWaters = True
                        ElseIf moleculeType = 2 AndAlso firstMoleculeAtom = atomNumber Then
                            Dim group As String = "id " & atomNumber.ToString()
                            metalsList.Add(group)
                        End If
                        If moleculeType = 3 OrElse mode = ABODY_SCRIPT1 AndAlso (moleculeType = 2 OrElse moleculeType = 1) Then
                            Dim group As String = "id " & firstMoleculeAtom.ToString()
                            For k = firstMoleculeAtom + 1 To atomNumber + 1 - 1
                                group = group & " or id " & k.ToString()
                            Next
                            If moleculeType = 3 Then
                                hydrophobicsList.Add(group)
                            End If
                            allList.Add(group)
                            coloursList.Add(colourNo)
                        End If
                    ElseIf mode = ABODY_SCRIPT2 Then
                        Dim group As String = "id " & firstMoleculeAtom.ToString()
                        For k = firstMoleculeAtom + 1 To atomNumber + 1 - 1
                            group = group & " or id " & k.ToString()
                        Next
                        allList.Add(group)
                        coloursList.Add(colourNo)
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
                                    out.format("select hatom, (id %d)" & vbLf, New Object() {Convert.ToInt32(atomNumber)})
                                    out.format("colour cyan, hatom" & vbLf, New Object(-1) {})
                                    out.format("label hatom, ""%s""" & vbLf, New Object() {bond.BondLabel.Text})
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
                            nHbond += 1
                            out.format("create hbond%02d, (id %d or id %d or id %d)" & vbLf, New Object() {Convert.ToInt32(nHbond), Convert.ToInt32(atomNumber - 2), Convert.ToInt32(atomNumber - 1), Convert.ToInt32(atomNumber)})
                            out.format("show lines, hbond%02d" & vbLf, New Object() {Convert.ToInt32(nHbond)})
                            out.format("colour cyan, hbond%02d" & vbLf, New Object() {Convert.ToInt32(nHbond)})
                        End If
                    End If
                Next
                If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 OrElse mode = ABODY_SCRIPT2 Then
                    If haveLigand Then
                        Dim ligandName = String.Format("lig{0:D2}", New Object() {Convert.ToInt32(iPDB + 1)})
                        out.format("create %s, (id %d", New Object() {ligandName, Convert.ToInt32(firstLigandAtom)})
                        For j = firstLigandAtom + 1 To lastLigandAtom + 1 - 1
                            out.format(" or id %d", New Object() {Convert.ToInt32(j)})
                        Next
                        out.format(")" & vbLf, New Object(-1) {})
                        out.format("set stick_radius, 0.4, %s" & vbLf, New Object() {ligandName})
                        out.format("show sticks, %s" & vbLf, New Object() {ligandName})
                        out.format("rebuild" & vbLf, New Object(-1) {})
                    End If
                    If mode = SCRIPT OrElse mode = ABODY_SCRIPT1 Then
                        Dim nAtoms = lastProteinAtom - firstProteinAtom + 1
                        Dim nDefs As Integer = CLng(Math.Round(nAtoms / NATOM_SPLIT, MidpointRounding.AwayFromZero)) + 1L
                        Dim iAtom = firstProteinAtom
                        For iDef = 0 To nDefs - 1
                            Dim proteinName = String.Format("prot{0:D2}_{1:D2}", New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(iDef + 1)})
                            out.format("create %s, (id %d", New Object() {proteinName, Convert.ToInt32(iAtom)})
                            Dim j = 1

                            While j < NATOM_SPLIT AndAlso iAtom < lastProteinAtom
                                iAtom += 1
                                out.format(" or id %d", New Object() {Convert.ToInt32(iAtom)})
                                j += 1
                            End While
                            out.format(")" & vbLf, New Object(-1) {})
                            If mode = SCRIPT Then
                                out.format("set stick_radius, 0.2, %s" & vbLf, New Object() {proteinName})
                                out.format("show sticks, %s" & vbLf, New Object() {proteinName})
                            ElseIf mode = ABODY_SCRIPT1 Then
                                out.format("alter %s, vdw=0.5" & vbLf, New Object() {proteinName})
                                out.format("show spheres, %s" & vbLf, New Object() {proteinName})
                            End If
                        Next
                    ElseIf mode = ABODY_SCRIPT2 Then
                        If allList.Count > 0 Then
                            For i = 0 To allList.Count - 1
                                Dim hGroupName = String.Format("hgrp{0:D2}_{1:D2}", New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(i + 1)})
                                Dim range = allList(i)
                                out.format("create %s, ( %s )" & vbLf, New Object() {hGroupName, range})
                                Dim colourNo = coloursList(i)
                                Dim iColour = Integer.Parse(colourNo) + 1
                                out.format("set stick_radius, 0.2, %s" & vbLf, New Object() {hGroupName})
                                out.format("show sticks, %s" & vbLf, New Object() {hGroupName})
                                out.format("color col%02d, %s" & vbLf, New Object() {Convert.ToInt32(iColour), hGroupName})
                                out.format("rebuild" & vbLf, New Object(-1) {})
                            Next
                        End If
                    End If
                    If haveWaters Then
                        Dim waterName = String.Format("waters{0:D2}", New Object() {Convert.ToInt32(iPDB + 1)})
                        out.format("create %s, (resn HOH)" & vbLf, New Object() {waterName})
                        out.format("alter %s, vdw=0.5" & vbLf, New Object() {waterName})
                        out.format("show spheres, %s" & vbLf, New Object() {waterName})
                        out.format("rebuild" & vbLf, New Object(-1) {})
                    End If
                    If metalsList.Count > 0 Then
                        Dim metalsName = String.Format("metals{0:D2}", New Object() {Convert.ToInt32(iPDB + 1)})
                        out.format("create %s, (", New Object() {metalsName})
                        For j = 0 To metalsList.Count - 1
                            Dim range = metalsList(j)
                            If j <> 0 Then
                                out.format(" or %s", New Object() {range})
                            Else
                                out.format("%s", New Object() {range})
                            End If
                        Next
                        out.format(")" & vbLf, New Object(-1) {})
                        out.format("alter %s, vdw=0.5" & vbLf, New Object() {metalsName})
                        out.format("rebuild" & vbLf, New Object(-1) {})
                        out.format("show spheres, %s" & vbLf, New Object() {metalsName})
                    End If
                    If hydrophobicsList.Count > 0 Then
                        Dim nHyd = hydrophobicsList.Count
                        Dim nDefs As Integer = CLng(Math.Round(nHyd / NATOM_SPLIT, MidpointRounding.AwayFromZero)) + 1L
                        For iDef = 0 To nDefs - 1
                            Dim hydrophobicsName = String.Format("hyd{0:D2}_{1:D2}", New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(iDef + 1)})
                            out.format("create %s, (", New Object() {hydrophobicsName})
                            Dim iRange = 0
                            Dim j = 0

                            While j < NATOM_SPLIT AndAlso iRange < hydrophobicsList.Count
                                Dim range = hydrophobicsList(iRange)
                                If j <> 0 Then
                                    out.format(" or %s", New Object() {range})
                                Else
                                    out.format("%s", New Object() {range})
                                End If
                                iRange += 1
                                j += 1
                            End While
                            out.format(")" & vbLf, New Object(-1) {})
                            If mode = SCRIPT Then
                                Dim hydSticksName = String.Format("hydsticks{0:D2}_{1:D2}", New Object() {Convert.ToInt32(iPDB + 1), Convert.ToInt32(iDef + 1)})
                                out.format("create %s, %s" & vbLf, New Object() {hydSticksName, hydrophobicsName})
                                out.format("alter %s, vdw=0.2" & vbLf, New Object() {hydSticksName})
                                out.format("rebuild" & vbLf, New Object(-1) {})
                                out.format("show spheres, %s" & vbLf, New Object() {hydSticksName})
                                out.format("set stick_radius, 0.2, %s" & vbLf, New Object() {hydSticksName})
                                out.format("show sticks, %s" & vbLf, New Object() {hydSticksName})
                                out.format("rebuild" & vbLf, New Object(-1) {})
                                out.format("alter %s, vdw=1.8" & vbLf, New Object() {hydrophobicsName})
                                out.format("rebuild" & vbLf, New Object(-1) {})
                            End If
                            out.format("set transparency=0.6" & vbLf, New Object(-1) {})
                            out.format("show surface, %s" & vbLf, New Object() {hydrophobicsName})
                        Next
                    End If
                End If
            Next
            Return atomNumber
        End Function

        Private Sub writeHeaders(out As PrintStream, coordsFile As String)
            Dim today = Date.Now
            out.format("# Pymol script" & vbLf, New Object(-1) {})
            out.format("# Generated by LigPlus v.2.3.1" & vbLf, New Object(-1) {})
            out.format("# Date: %s" & vbLf, New Object() {today.ToString()})
            out.format("# R A Laskowski, Aug 2009" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
            out.format("delete all" & vbLf, New Object(-1) {})
            out.format("reset" & vbLf, New Object(-1) {})
            out.format("bg_color black" & vbLf, New Object(-1) {})
            out.format("set auto_color, 0" & vbLf, New Object(-1) {})
            out.format("set_color cream, [ 1.0, 1.0, 0.7 ]" & vbLf, New Object(-1) {})
            out.format("set_color lightgrey, [ 0.8, 0.8, 0.8 ]" & vbLf, New Object(-1) {})
            out.format("set_color skyblue, [ 0.7, 0.9, 1.0 ]" & vbLf, New Object(-1) {})
            out.format("set_color cyan, [ 0.0, 1.0, 1.0 ]" & vbLf, New Object(-1) {})
            out.format("set cartoon_ring_mode,1" & vbLf, New Object(-1) {})
            out.format("set dot_mode, 1" & vbLf, New Object(-1) {})
            out.format("set dot_density, 3" & vbLf, New Object(-1) {})
            out.format("util.cbag" & vbLf, New Object(-1) {})
            out.format("load %s" & vbLf, New Object() {coordsFile})
            out.format("colour lightgrey, (elem C)" & vbLf, New Object(-1) {})
            out.format("hide lines, all" & vbLf, New Object(-1) {})
            out.format(vbLf, New Object(-1) {})
        End Sub

        Private Sub writeEndLines(out As PrintStream)
            out.format("zoom all, complete=1, buffer=2" & vbLf, New Object(-1) {})
        End Sub
    End Class

End Namespace
