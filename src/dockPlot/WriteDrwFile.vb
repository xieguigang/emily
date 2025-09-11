Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Friend Class WriteDrwFile
        Public Const SHIFT_MARGIN As Integer = 2

        Private atomNumber As Integer = 0

        Friend halfwayPoint As Single = 0.0F

        Friend shiftBack As Single() = New Single(1) {}

        Friend nDummy As Integer = 0

        Dim ensemble As Ensemble
        Dim straightenDimShift As Boolean

        Public Sub New(ensemble As Ensemble, straightenDimShift As Boolean)
            Me.ensemble = ensemble
            Me.straightenDimShift = straightenDimShift
        End Sub

        Public Sub write(fileName As String)
            Dim first = True
            Dim selected = False
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList

            Using out = New PrintStream(fileName)
                Dim today = Date.Now
                out.format("#--- Created by: LigPlus v.2.3.1" & vbLf, New Object(-1) {})
                out.format("#--- Date:       %s" & vbLf, New Object() {today.ToString()})
                out.format("#V" & vbLf, New Object(-1) {})
                Dim selectedEntry = ensemble.SelectedEntry
                If ensemble.DiagramType = 2 Then
                    If ensemble.OrientationOption = RunExe.PORTRAIT Then
                        out.format("#DP" & vbLf, New Object(-1) {})
                    Else
                        out.format("#DL" & vbLf, New Object(-1) {})
                    End If
                End If
                For i = 0 To pdbEntryList.Count - 1
                    If i = selectedEntry Then
                        selected = True
                    Else
                        selected = False
                    End If
                    Dim pdb = pdbEntryList(i)
                    atomNumber = 0
                    writePDBInfo(out, pdb, selected)
                    If ensemble.Antibody Then
                        If pdb.AntibodyNumberingScheme > -1 AndAlso pdb.AntibodyNumberingScheme < 3 Then
                            out.format("#Y%d" & vbLf, New Object() {Convert.ToInt32(pdb.AntibodyNumberingScheme)})
                        Else
                            out.format("#Y" & vbLf, New Object(-1) {})
                        End If
                    End If
                    writeMolecules(out, ensemble, pdb, First, straightenDimShift)
                    First = False
                    writeBonds(out, pdb)
                    writeTitle(out, ensemble, pdb, straightenDimShift)
                    If ensemble.Antibody AndAlso pdb.AntibodyLoopLabel IsNot Nothing Then
                        writeAntibodyLabels(out, ensemble, pdb, straightenDimShift)
                    End If
                    writeParams(out, pdb)
                    If straightenDimShift Then
                        ensemble.updateMaxMinCoords()
                    End If
                Next
            End Using
        End Sub

        Private Sub writeAntibodyLabels(out As PrintStream, ensemble As Ensemble, pdb As PDBEntry, straightenDimShift As Boolean)
            Dim first = True
            Dim coords = New Single(1) {}
            Dim newCoords = New Single(1) {}
            Dim antibodyLoopLabel As List(Of TextItem) = pdb.AntibodyLoopLabel
            For i = 0 To antibodyLoopLabel.Count - 1
                Dim label = antibodyLoopLabel(i)
                Dim antibodyID = label.AntibodyLoopID
                If first Then
                    out.format("#L" & vbLf, New Object(-1) {})
                    first = False
                End If
                Dim text = label.Text
                coords = label.Coords
                newCoords(0) = coords(0)
                newCoords(1) = coords(1)
                If nDummy > 1 AndAlso (ensemble.OrientationOption = RunExe.LANDSCAPE AndAlso coords(1) < halfwayPoint OrElse ensemble.OrientationOption = RunExe.PORTRAIT AndAlso coords(0) > halfwayPoint) Then
                    newCoords(0) = coords(0) + shiftBack(0)
                    newCoords(1) = coords(1) + shiftBack(1)
                End If
                If straightenDimShift Then
                    label.Coords = newCoords
                End If
                Dim outLine = antibodyID & ":" & text & "    " & format(newCoords(0)) & " " & format(newCoords(1))
                out.format("%s" & vbLf, New Object() {outLine})
            Next
        End Sub

        Private Sub writeAtoms(out As PrintStream, residue As Residue, makeShift As Single(), straightenDimShift As Boolean)
            Dim coords = New Single(1) {}
            Dim newCoords = New Single(1) {}
            out.format("#A" & vbLf, New Object(-1) {})
            Dim atomList As List(Of Atom) = residue.AtomList
            For i = 0 To atomList.Count - 1
                Dim atom = atomList(i)
                atom.AtomNumber = atomNumber
                atomNumber += 1
                Dim atomName = atom.AtomName
                Dim x = atom.getCoord(0) + makeShift(0)
                Dim y = atom.getCoord(1) + makeShift(1)
                If straightenDimShift Then
                    atom.setCoord(0, x)
                    atom.setCoord(1, y)
                End If
                Dim outLine = atomName & " " & format(x) & " " & format(y)
                Dim textItem = atom.AtomLabel
                If textItem IsNot Nothing Then
                    coords = textItem.Coords
                    newCoords(0) = coords(0) + makeShift(0)
                    newCoords(1) = coords(1) + makeShift(1)
                    If straightenDimShift Then
                        textItem.Coords = newCoords
                    End If
                    Dim text = textItem.Text
                    outLine = outLine & " " & format(newCoords(0)) & " " & format(newCoords(1)) & " " & text
                Else
                    outLine = outLine & " " & format(x) & " " & format(y)
                End If
                outLine = outLine & " # " & format(atom.getOriginalCoord(0)) & " " & format(atom.getOriginalCoord(1)) & " " & format(atom.getOriginalCoord(2))
                outLine = outLine & "   [" & atom.AtomNumber.ToString() & "]"
                out.format("%s" & vbLf, New Object() {outLine})
            Next
        End Sub

        Private Sub writeBonds(out As PrintStream, pdb As PDBEntry)
            out.format("#B" & vbLf, New Object(-1) {})
            Dim bondList As List(Of Bond) = pdb.BondList
            For i = 0 To bondList.Count - 1
                Dim bond = bondList(i)
                Dim atomNo1 = bond.getAtom(0).AtomNumber
                Dim atomNo2 = bond.getAtom(1).AtomNumber
                Dim type = bond.Type
                Dim outLine = format(bond.Type) & " " & format(atomNo1) & " " & format(atomNo2)
                If type = 1 Then
                    Dim bondLabel = bond.BondLabel
                    If bondLabel IsNot Nothing Then
                        outLine = outLine & " " & bondLabel.Text
                    End If
                End If
                If type = 0 Then
                    Dim order = bond.BondOrder
                    If order = 1 Then
                        outLine = outLine & " d"
                    ElseIf order = 2 Then
                        outLine = outLine & " t"
                    End If
                End If
                out.format("%s" & vbLf, New Object() {outLine})
            Next
        End Sub

        Private Sub writeMolecules(out As PrintStream, ensemble As Ensemble, pdb As PDBEntry, first As Boolean, straightenDimShift As Boolean)
            Dim coords = New Single(1) {}
            Dim newCoords = New Single(1) {}
            Dim makeShift = New Single(1) {}
            makeShift(1) = 0.0F
            makeShift(0) = 0.0F
            If first Then
                shiftBack(1) = 0.0F
                shiftBack(0) = 0.0F
                nDummy = pdb.NDummy
                If nDummy > 1 Then
                    If ensemble.OrientationOption = RunExe.LANDSCAPE Then
                        shiftBack(0) = pdb.getDummyMolecule(0).getCoordsMax(0) - pdb.getDummyMolecule(1).getCoordsMin(0) + 2.0F
                        shiftBack(1) = pdb.getDummyMolecule(0).getCoordsMin(1) - pdb.getDummyMolecule(1).getCoordsMin(1)
                        halfwayPoint = (pdb.getDummyMolecule(0).getCoordsMin(1) + pdb.getDummyMolecule(1).getCoordsMin(1)) / 2.0F
                    Else
                        shiftBack(0) = pdb.getDummyMolecule(0).getCoordsMin(0) - pdb.getDummyMolecule(1).getCoordsMin(0)
                        shiftBack(1) = pdb.getDummyMolecule(1).getCoordsMax(1) - pdb.getDummyMolecule(0).getCoordsMin(1) + 2.0F
                        halfwayPoint = (pdb.getDummyMolecule(0).getCoordsMin(0) + pdb.getDummyMolecule(1).getCoordsMin(0)) / 2.0F
                    End If
                End If
            End If
            Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
            For i = 0 To moleculeList.Count - 1
                Dim molecule = moleculeList(i)
                If molecule.InterFace = 0 Then
                    If ReferenceEquals(molecule.MoleculeID, Nothing) Then
                        out.format("#O" & vbLf, New Object(-1) {})
                    Else
                        out.format("#O%s" & vbLf, New Object() {molecule.MoleculeID})
                    End If
                ElseIf ReferenceEquals(molecule.MoleculeID, Nothing) Then
                    out.format("#O%d" & vbLf, New Object() {Convert.ToInt32(molecule.InterFace)})
                Else
                    out.format("#O%d%s" & vbLf, New Object() {Convert.ToInt32(molecule.InterFace), molecule.MoleculeID})
                End If
                Dim residueList As List(Of Residue) = molecule.ResidueList
                For j = 0 To residueList.Count - 1
                    Dim residue = residueList(j)
                    If residue.Antigen Then
                        out.format("#R %s ::A" & vbLf, New Object() {residue.ResidueId})
                    ElseIf residue.AntibodyLoopID.Equals("") Then
                        out.format("#R %s" & vbLf, New Object() {residue.ResidueId})
                    Else
                        out.format("#R %s ::%s" & vbLf, New Object() {residue.ResidueId, residue.AntibodyLoopID})
                    End If
                    Dim type = molecule.MoleculeType
                    Dim resName = residue.ResName
                    Dim resNum = residue.ResNum
                    Dim chain = residue.Chain
                    If chain = " "c Then
                        chain = "-"c
                    End If
                    Dim outLine As String = format(type) & resName & resNum & chain.ToString()
                    Dim iSet = 0
                    makeShift(1) = 0.0F
                    makeShift(0) = 0.0F
                    If nDummy > 1 Then
                        If ensemble.OrientationOption = RunExe.LANDSCAPE Then
                            If (residue.getCoordsMin(1) + residue.getCoordsMax(1)) / 2.0F < halfwayPoint Then
                                iSet = 1
                            End If
                        ElseIf (residue.getCoordsMin(0) + residue.getCoordsMax(0)) / 2.0F > halfwayPoint Then
                            iSet = 1
                        End If
                    End If
                    If iSet = 1 Then
                        makeShift(0) = shiftBack(0)
                        makeShift(1) = shiftBack(1)
                    End If
                    Dim textItem = residue.ResidueLabel
                    If textItem IsNot Nothing Then
                        coords = textItem.Coords
                        newCoords(0) = coords(0) + makeShift(0)
                        newCoords(1) = coords(1) + makeShift(1)
                        If straightenDimShift Then
                            textItem.Coords = newCoords
                        End If
                        Dim text = textItem.Text
                        outLine = outLine & "    " & format(newCoords(0)) & " " & format(newCoords(1)) & " " & text
                    End If
                    out.format("%s" & vbLf, New Object() {outLine})
                    writeAtoms(out, residue, makeShift, straightenDimShift)
                    If residue.Nequiv > 0 Then
                        out.format("#Q" & vbLf, New Object(-1) {})
                        For iRes = 0 To residue.Nequiv - 1
                            Dim equivalence = residue.getResidueEquivalence(iRes)
                            Dim equivPDB = CType(equivalence(0), PDBEntry)
                            Dim equivResidue = CType(equivalence(1), Residue)
                            out.format("%d" & vbTab & "%s" & vbLf, New Object() {Convert.ToInt32(equivPDB.PDBId), equivResidue.ResidueId})
                        Next
                    End If
                Next
            Next
        End Sub

        Private Sub writeParams(out As PrintStream, pdb As PDBEntry)
            out.format("#P" & vbLf, New Object(-1) {})
            Dim params = pdb.Params
            If params IsNot Nothing Then
                For Each [property] In params.Keys
                    If [property].IndexOf("_COLOUR", StringComparison.Ordinal) > -1 OrElse [property].IndexOf("_WIDTH", StringComparison.Ordinal) > -1 OrElse [property].IndexOf("_SIZE", StringComparison.Ordinal) > -1 OrElse [property].IndexOf("_RADIUS", StringComparison.Ordinal) > -1 OrElse [property].IndexOf("_STATUS", StringComparison.Ordinal) > -1 OrElse [property].IndexOf("FONT_", StringComparison.Ordinal) > -1 Then
                        out.format("%s=%s" & vbLf, New Object() {[property], params([property])})
                    End If
                Next
            End If
        End Sub

        Private Sub writePDBInfo(out As PrintStream, pdb As PDBEntry, selected As Boolean)
            out.format("#E %d" & vbLf, New Object() {Convert.ToInt32(pdb.PDBId)})
            Dim pdbCode = pdb.PDBCode
            If Not ReferenceEquals(pdbCode, Nothing) AndAlso Not pdbCode.Equals("") Then
                out.format("PDB CODE: %s" & vbLf, New Object() {pdbCode})
            End If
            Dim plotLabel = pdb.PlotLabel.Text
            If Not ReferenceEquals(plotLabel, Nothing) AndAlso Not plotLabel.Equals("") Then
                out.format("LABEL: %s" & vbLf, New Object() {plotLabel})
            End If
            Dim title = pdb.FullTitle
            If Not ReferenceEquals(title, Nothing) AndAlso Not title.Equals("") Then
                out.format("TITLE: %s" & vbLf, New Object() {title})
            End If
            If selected Then
                out.format("SELECTED" & vbLf, New Object(-1) {})
            End If
        End Sub

        Private Sub writeTitle(out As PrintStream, ensemble As Ensemble, pdb As PDBEntry, straightenDimShift As Boolean)
            Dim coords = New Single(1) {}
            Dim newCoords = New Single(1) {}
            Dim pdbTitle = pdb.PDBTitle
            If pdbTitle IsNot Nothing AndAlso Not pdbTitle.Text.Equals("") Then
                out.format("#T" & vbLf, New Object(-1) {})
                Dim text = pdbTitle.Text
                coords = pdbTitle.Coords
                If nDummy > 1 Then
                    If ensemble.OrientationOption = RunExe.LANDSCAPE Then
                        newCoords(0) = coords(0) + shiftBack(0) / 2.0F
                        newCoords(1) = coords(1) + shiftBack(1)
                    Else
                        newCoords(0) = coords(0) + shiftBack(0) / 2.0F
                        newCoords(1) = coords(1)
                    End If
                    If straightenDimShift Then
                        pdbTitle.Coords = newCoords
                    End If
                End If
                coords = pdbTitle.Coords
                Dim outLine = text & "    " & format(coords(0)) & " " & format(coords(1))
                out.format("%s" & vbLf, New Object() {outLine})
            End If
        End Sub

        Private Function format(x As Single) As String
            Return x.ToString("F4")
        End Function
    End Class

End Namespace
