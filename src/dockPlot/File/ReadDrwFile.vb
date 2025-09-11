Imports System.IO
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text.Parser

Namespace file

    Public Class ReadDrwFile
        Public Enum LineType
            ANTIBODY_LABEL
            ANTIBODY_NUMBERING_SCHEME
            ATOM_DATA
            BOND_DATA
            COLOUR_DATA
            COLOURMAP_DATA
            DIMPLOT_FLAG
            EQUIV_RESIDUES
            MOLECULE_DATA
            NEW_PDB_ENTRY
            PLOT_PARAMS
            PLOT_TITLE
            RES_DATA
            SIZE_DATA
            SAVED_FILE
            NOT_WANTED
        End Enum

        Private antibodyField As Boolean = False

        Private savedFile As Boolean = False

        Private antibodyNumberingScheme As Integer = 3

        Private [interFace] As Integer = 0

        Private iPDB As Integer = 0

        Private nAtoms As Integer = 0

        Private nPDB As Integer = 0

        Private readFrom As Integer

        Private selectedEntry As Integer = -1

        Private fullResNum As Integer = 0

        Private lightChain As Char

        Private heavyChain As Char

        Private isAntigen As Boolean = False

        Private antigenChain As Char

        Private fullLightChain As String

        Private fullHeavyChain As String

        Private fullAntigenChain As String

        Private ranLigfit As Boolean = False

        Private orientation As Integer = RunExe.LANDSCAPE

        Private programField As Integer = ensemble.LIGPLOT

        Private antibodyLoopID As String = Nothing

        Private errorMessageField As String = Nothing

        Private fullChain As String = " "

        Private fullResName As String = Nothing

        Private insCode As String = " "

        Private coords As Single()

        Private ensemble As Ensemble = Nothing

        Private molecule As Molecule = Nothing

        Private pdb As PDBEntry = Nothing

        Private params As Properties

        Private residue As Residue = Nothing

        Private paramFileName As String = Nothing

        Private atomList As System.Collections.Generic.List(Of Atom) = Nothing

        Public Sub New(ensemble As Ensemble, fileName As String, pdbCode As String, antibody As Boolean, antibodyNumberingScheme As Integer, fullHeavyChain As String, fullLightChain As String, fullAntigenChain As String, orientation As Integer, ranLigfit As Boolean, frame As LigPlusFrame, plotArea As PlotArea)
            Dim lType = LineType.NOT_WANTED
            Dim dimShiftX = 0.0F
            Dim dimShiftY = 0.0F
            iPDB = -1
            antibodyField = antibody
            Me.antibodyNumberingScheme = antibodyNumberingScheme
            Me.orientation = orientation
            Me.ranLigfit = ranLigfit
            Me.fullLightChain = fullLightChain
            Me.fullHeavyChain = fullHeavyChain
            Me.fullAntigenChain = fullAntigenChain
            ensemble.OrientationOption = orientation
            params = ensemble.DefaultParams
            paramFileName = RunExe.stripExtension(fileName) & ".prm"
            init(ensemble, pdbCode)
            nPDB = 0
            Me.ensemble = ensemble
            selectedEntry = -1
            Dim inputStream As StreamReader = Nothing
            Try
                inputStream = New StreamReader(fileName)
                Dim inputLine As Value(Of String) = ""

                While Not (inputLine = inputStream.ReadLine()) Is Nothing
                    Dim line_str As String = inputLine
                    Dim lineLength = line_str.Length
                    If lineLength > 1 AndAlso line_str(0) = "#"c Then
                        lType = getLineType(line_str)
                        Continue While
                    End If
                    If lineLength > 8 AndAlso line_str.Substring(0, 7).Equals("ERROR: ") Then
                        errorMessageField = line_str.Substring(7)
                        Continue While
                    End If
                    If lineLength > 0 AndAlso lType <> LineType.NOT_WANTED Then
                        interpretLine(line_str, lType)
                    End If
                End While

            Finally
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
            If pdb IsNot Nothing AndAlso nAtoms = 0 Then
                Dim entryNo = ensemble.getEntryNo(pdb)
                If entryNo > -1 Then
                    ensemble.SelectedEntry = entryNo
                    ensemble.deleteSelectedPDBEntry()
                End If
                pdb = Nothing
            ElseIf selectedEntry > -1 Then
                ensemble.SelectedEntry = selectedEntry
            End If
            If pdb Is Nothing Then
                Return
            End If
            pdb.flagShortestHphobics()
            If programField = ensemble.DIMPLOT OrElse antibodyField Then
                ensemble.updateMaxMinCoords()
                If antibodyField Then
                    If Not savedFile Then
                        pdb.createAntibodyLoopListLabel(Me.orientation)
                    End If
                End If
            End If
            If pdb IsNot Nothing Then
                Dim moleculeList = pdb.MoleculeList
                Console.WriteLine("Number of molecules in list: " & moleculeList.Count.ToString())
                Dim residueList = pdb.ResidueList
                Console.WriteLine("Number of residues in list: " & residueList.Count.ToString())
                Dim atomList = pdb.AtomList
                Console.WriteLine("Number of atoms in list: " & atomList.Count.ToString())
                Dim bondList = pdb.BondList
                Console.WriteLine("Number of bonds in list: " & bondList.Count.ToString())
            End If
        End Sub

        Private Sub init(ensemble As Ensemble, pdbCode As String)
            coords = New Single(2) {}
            Dim params As Properties = ensemble.DefaultParams
            Dim instance = ensemble.checkPDBCode(pdbCode, pdb)
            iPDB += 1
            pdb = New PDBEntry(params, pdbCode, instance, ensemble)
            ensemble.addPDBEntry(pdb)
            If antibodyField Then
                pdb.AntibodyNumberingScheme = antibodyNumberingScheme
            End If
            nAtoms = 0
            readFrom = 1
        End Sub

        Private Function formatResidueName(resName As String, resNum As String, chain As Char) As String
            Dim tempName = resName.Substring(1)
            tempName = tempName.ToLower()
            Dim tempNum As String = resNum.Trim()
            Dim [string] As String = resName(0).ToString() & tempName & tempNum
            If chain <> " "c AndAlso chain <> "-"c Then
                [string] = [string] & "(" & chain.ToString() & ")"
            End If
            Return [string]
        End Function

        Private Function getLineType(inputLine As String) As LineType
            Dim lType = LineType.NOT_WANTED
            Dim code = inputLine(1)
            If code = "P"c Then
                lType = LineType.PLOT_PARAMS
            ElseIf code = "E"c Then
                lType = LineType.NEW_PDB_ENTRY
                nPDB += 1
                If nPDB > 1 Then
                    init(ensemble, "ligplot" & nPDB.ToString())
                End If
                If inputLine.Length > 3 Then
                    Dim PDBId = Integer.Parse(inputLine.Substring(3))
                    pdb.PDBId = PDBId
                End If
            ElseIf code = "L"c Then
                lType = LineType.ANTIBODY_LABEL
            ElseIf code = "M"c Then
                lType = LineType.COLOURMAP_DATA
            ElseIf code = "D"c Then
                lType = LineType.DIMPLOT_FLAG
                pdb.ReadFrom = 2
                readFrom = 2
                programField = ensemble.DIMPLOT
                If ranLigfit OrElse savedFile Then
                    If inputLine.Length > 2 AndAlso inputLine(2) = "P"c Then
                        orientation = RunExe.PORTRAIT
                    ElseIf inputLine.Length > 2 AndAlso inputLine(2) = "L"c Then
                        orientation = RunExe.LANDSCAPE
                    End If
                    ensemble.OrientationOption = orientation
                End If
                ensemble.DiagramType = readFrom
                Dim pdbParams = pdb.Params
                pdbParams("PROGRAM") = "DIMPLOT"
            ElseIf code = "Y"c Then
                lType = LineType.ANTIBODY_NUMBERING_SCHEME
                ensemble.Antibody = True
                If inputLine.Length > 2 Then
                    antibodyNumberingScheme = Integer.Parse(inputLine.Substring(2))
                    antibodyField = True
                End If
                pdb.AntibodyNumberingScheme = antibodyNumberingScheme
            ElseIf code = "C"c Then
                lType = LineType.COLOUR_DATA
            ElseIf code = "S"c Then
                lType = LineType.SIZE_DATA
            ElseIf code = "V"c Then
                lType = LineType.SAVED_FILE
                savedFile = True
            ElseIf code = "O"c Then
                lType = LineType.MOLECULE_DATA
                If inputLine.Length > 2 AndAlso (inputLine(2) = "1"c OrElse inputLine(2) = "2"c) Then
                    Dim numberString As String = "" & inputLine(2).ToString()
                    [interFace] = Integer.Parse(numberString)
                Else
                    [interFace] = 0
                End If
                Dim moleculeID = "none"
                If inputLine.Length > 3 Then
                    If inputLine(2) = "M"c Then
                        moleculeID = inputLine.Substring(2)
                    ElseIf inputLine(3) = "M"c Then
                        moleculeID = inputLine.Substring(3)
                    End If
                End If
                If inputLine.Equals("#Odummy") Then
                    moleculeID = "dummy"
                End If
                molecule = pdb.addMolecule(" "c, " ", moleculeID)
                molecule.InterFace = [interFace]
            ElseIf code = "R"c Then
                lType = LineType.RES_DATA
                If inputLine.Contains(" ::") Then
                    Dim split = inputLine.StringSplit("::", True)
                    If split(1).Equals("A") Then
                        antibodyLoopID = Nothing
                        isAntigen = True
                    Else
                        antibodyLoopID = split(1)
                        isAntigen = False
                    End If
                Else
                    antibodyLoopID = Nothing
                    isAntigen = False
                End If
            ElseIf code = "Q"c Then
                lType = LineType.EQUIV_RESIDUES
            ElseIf code = "A"c Then
                lType = LineType.ATOM_DATA
            ElseIf code = "B"c Then
                lType = LineType.BOND_DATA
                atomList = pdb.AtomList
            ElseIf code = "T"c Then
                lType = LineType.PLOT_TITLE
            End If
            Return lType
        End Function

        Private Sub interpretLine(inputLine As String, lType As LineType)
            Select Case lType
                Case LineType.PLOT_PARAMS
                    newPlotParams(inputLine)
                Case LineType.NEW_PDB_ENTRY
                    newPDBEntry(inputLine)
                Case LineType.RES_DATA
                    newResidue(inputLine)
                Case LineType.EQUIV_RESIDUES
                    equivalentResidues(inputLine)
                Case LineType.ATOM_DATA
                    newAtom(inputLine)
                Case LineType.BOND_DATA
                    newBond(inputLine)
                Case LineType.PLOT_TITLE
                    plotTitle(inputLine)
                Case LineType.ANTIBODY_LABEL
                    antibodyLabel(inputLine)
            End Select
        End Sub

        Private Sub antibodyLabel(inputLine As String)
            Dim first = True
            If inputLine(2) <> ":"c Then
                Return
            End If
            Dim antibodyLoopID = inputLine.Substring(0, 2)
            Dim newLine = inputLine.Substring(3)
            Dim token As Scanner = New Scanner(newLine)
            Dim nTokens = 0
            While token.HasNext()
                Dim numberString As String = token.Next()
                nTokens += 1
            End While
            If nTokens > 2 Then
                token = New Scanner(newLine)
                Dim label = ""
                For iToken = 0 To nTokens - 2 - 1
                    If iToken > 0 Then
                        label = label & " "
                    End If
                    label = label & token.Next()
                Next
                Dim numberString As String = token.Next()
                Dim f As Single? = Convert.ToSingle(Single.Parse(numberString))
                Dim x = f.Value
                numberString = token.Next()
                f = Convert.ToSingle(Single.Parse(numberString))
                Dim y = f.Value
                Dim textType = 14
                Dim textItem As TextItem = New TextItem(pdb, label, textType, x, y)
                pdb.addAntibodyLoopLabel(textItem)
                textItem.AntibodyLoopID = antibodyLoopID
            End If
        End Sub

        Private Function copyDummyMolecule(firstDummyMolecule As Molecule, dimShiftX As Single, dimShiftY As Single) As Molecule
            molecule = pdb.addMolecule("X"c, "X", "dummy")
            Dim coords = New Single(4) {}
            Dim xCoord = New Single(1) {}
            Dim yCoord = New Single(1) {}
            Dim inputLine = "7DUM   2 X"
            newResidue(inputLine)
            xCoord(0) = firstDummyMolecule.getCoordsMin(0) + dimShiftX
            yCoord(0) = firstDummyMolecule.getCoordsMin(1) + dimShiftY
            xCoord(1) = firstDummyMolecule.getCoordsMax(0) + dimShiftX
            yCoord(1) = firstDummyMolecule.getCoordsMax(1) + dimShiftY
            inputLine = " C1   " & xCoord(0).ToString() & " " & yCoord(0).ToString()
            newAtom(inputLine)
            Dim atomNumber1 = nAtoms - 1
            inputLine = " C2   " & xCoord(1).ToString() & " " & yCoord(1).ToString()
            newAtom(inputLine)
            Dim atomNumber2 = nAtoms - 1
            atomList = pdb.AtomList
            inputLine = "4 " & atomNumber1.ToString() & " " & atomNumber2.ToString()
            newBond(inputLine)
            Return molecule
        End Function

        Private Function createDummyMolecule() As Molecule
            [interFace] = 0
            molecule = pdb.addMolecule("X"c, "X", "dummy")
            Dim coords = New Single(4) {}
            Dim xCoord = New Single(1) {}
            Dim yCoord = New Single(1) {}
            Dim inputLine = "7DUM   1 X"
            newResidue(inputLine)
            coords = pdb.calcInterfaceLine()
            xCoord(0) = coords(1)
            yCoord(0) = coords(2)
            xCoord(1) = coords(3)
            yCoord(1) = coords(4)
            inputLine = " C1   " & xCoord(0).ToString() & " " & yCoord(0).ToString()
            newAtom(inputLine)
            Dim atomNumber1 = nAtoms - 1
            inputLine = " C2   " & xCoord(1).ToString() & " " & yCoord(1).ToString()
            newAtom(inputLine)
            Dim atomNumber2 = nAtoms - 1
            atomList = pdb.AtomList
            inputLine = "4 " & atomNumber1.ToString() & " " & atomNumber2.ToString()
            newBond(inputLine)
            Return molecule
        End Function

        Private Sub equivalentResidues(inputLine As String)
            Dim equivPDB As PDBEntry = Nothing
            Dim equivResidue As Residue = Nothing
            Dim token As Scanner = New Scanner(inputLine)
            token.UseDelimiter(vbTab)
            Dim itoken = 0

            While token.HasNext() = True
                Dim PDBId As Integer
                Dim tokenString As String = token.Next()
                Select Case itoken
                    Case 0
                        PDBId = Integer.Parse(tokenString)
                        equivPDB = ensemble.identifyPDBEntry(PDBId)
                    Case 1
                        If equivPDB IsNot Nothing Then
                            equivResidue = equivPDB.identifyResidue(tokenString)
                        End If
                End Select
                If residue IsNot Nothing AndAlso equivResidue IsNot Nothing Then
                    residue.storeResidueEquivalence(equivPDB, equivResidue)
                    equivResidue.storeResidueEquivalence(pdb, residue)
                End If

                itoken += 1
            End While
        End Sub

        Private Sub newAtom(inputLine As String)
            Dim atHet = "A"c
            Dim atomCoords = New Single(2) {}
            Dim labelCoords = New Single(1) {}
            Dim originalCoords = New Single(2) {}
            Dim bValue = 0.0F, occupancy = 0.0F
            Dim element = "  "
            Dim i As Integer
            For i = 0 To 2
                atomCoords(i) = 0.0F
            Next
            labelCoords(1) = 0.0F
            labelCoords(0) = 0.0F
            For i = 0 To 2
                originalCoords(i) = 0.0F
            Next
            Dim haveLabel = False
            Dim atomName = inputLine.Substring(0, 4)
            Dim line = inputLine.Substring(4)
            Dim alteredName = ""
            Dim startOrigCoords = False
            Dim iCoord = 0
            Dim token As Scanner = New Scanner(line)
            Dim itoken = 0

            While token.HasNext() = True
                Dim f As Single?
                Dim numberString As String = token.Next()
                Select Case itoken
                    Case 0
                        f = Convert.ToSingle(Single.Parse(numberString))
                        atomCoords(0) = f.Value
                    Case 1
                        f = Convert.ToSingle(Single.Parse(numberString))
                        atomCoords(1) = f.Value
                    Case 2
                        f = Convert.ToSingle(Single.Parse(numberString))
                        labelCoords(0) = f.Value
                    Case 3
                        f = Convert.ToSingle(Single.Parse(numberString))
                        labelCoords(1) = f.Value
                        If labelCoords(0) <> 0.0F AndAlso labelCoords(0) <> 0.0F Then
                            haveLabel = True
                        End If

                    Case Else
                        If Not startOrigCoords Then
                            If numberString.Equals("#") Then
                                startOrigCoords = True
                                Exit Select
                            End If
                            If alteredName.Length > 0 Then
                                alteredName = alteredName & " "
                            End If
                            alteredName = alteredName & numberString
                            Exit Select
                        End If
                        If iCoord < 3 Then
                            originalCoords(iCoord) = Single.Parse(numberString)
                            iCoord += 1
                        End If
                End Select

                itoken += 1
            End While
            nAtoms += 1
            Dim atom = pdb.addAtom(atHet, nAtoms, atomName, atomCoords, bValue, occupancy, residue, element, originalCoords)
            Dim atomLabel As String = atomName.Trim()
            If alteredName.Length > 0 Then
                atomLabel = alteredName
            End If
            Dim textType = -1
            If haveLabel Then
                If molecule.MoleculeType = 1 Then
                    textType = 7
                ElseIf molecule.MoleculeType = 2 Then
                    textType = 8
                ElseIf molecule.MoleculeType = 4 Then
                    textType = 4
                    If programField = RunExe.DIMPLOT Then
                        textType = 24
                    End If
                End If
                If molecule.InterFace = 1 Then
                    textType = 17
                ElseIf molecule.InterFace = 2 Then
                    textType = 18
                End If
            End If
            If textType <> -1 AndAlso textType <> 4 AndAlso textType <> 24 Then
                Dim textItem As TextItem = New TextItem(pdb, atomLabel, textType, labelCoords(0), labelCoords(1))
                atom.AtomLabel = textItem
                molecule.addTextItem(textItem)
                If residue IsNot Nothing Then
                    Dim antibodyLoopID = residue.AntibodyLoopID
                    If Not antibodyLoopID.Equals("") Then
                        textItem.AntibodyLoopID = antibodyLoopID
                    End If
                    If residue.Antigen Then
                        textItem.setIsAntigen()
                    End If
                End If
                textItem.MoleculeID = molecule.MoleculeID
            End If
        End Sub

        Private Sub newBond(inputLine As String)
            Dim atom = New Atom(1) {}
            Dim bond As Bond = Nothing
            Dim order = 0
            Dim type = -1
            atom(1) = Nothing
            atom(0) = Nothing
            Dim token As Scanner = New Scanner(inputLine)
            If token.HasNext() Then
                Dim numberString As String = token.Next()
                type = Integer.Parse(numberString)
            Else
                Return
            End If
            For i = 0 To 1
                If token.HasNext() Then
                    Dim numberString As String = token.Next()
                    Dim atomNumber = Integer.Parse(numberString)
                    If atomNumber > -1 AndAlso atomNumber < atomList.Count Then
                        atom(i) = atomList(atomNumber)
                    End If
                Else
                    Return
                End If
            Next
            If atom(0) IsNot Nothing AndAlso atom(1) IsNot Nothing Then
                bond = pdb.addBond(atom, type)
                If type = 6 OrElse type = 0 AndAlso atom(0).Molecule.Equals(atom(1).Molecule) Then
                    Dim molecule = atom(0).Molecule
                    molecule.addBond(bond)
                End If
            Else
                Return
            End If
            Dim bondLabel = ""
            While token.HasNext()
                If bondLabel.Length > 0 Then
                    bondLabel = bondLabel & " "
                End If
                bondLabel = bondLabel & token.Next()
            End While
            If bondLabel.Length > 0 Then
                If type = 1 OrElse type = 3 Then
                    Dim xc As Single = (atom(0).X + atom(1).X) / 2.0R
                    Dim yc As Single = (atom(0).Y + atom(1).Y) / 2.0R
                    Dim textType = 10
                    Dim textItem As TextItem = New TextItem(pdb, bondLabel, textType, xc, yc)
                    bond.BondLabel = textItem
                    molecule.addTextItem(textItem)
                Else
                    Dim bondOrder = bondLabel(0)
                    If bondOrder = "s"c Then
                        order = 0
                    ElseIf bondOrder = "d"c Then
                        order = 1
                    ElseIf bondOrder = "t"c Then
                        order = 2
                    Else
                        order = 0
                    End If
                    bond.BondOrder = order
                End If
            End If
        End Sub

        Private Sub newPDBEntry(inputLine As String)
            Dim lineLength = inputLine.Length
            pdb.ReadFrom = readFrom
            If antibodyField Then
                pdb.AntibodyNumberingScheme = antibodyNumberingScheme
            End If
            If lineLength > 10 AndAlso inputLine.Substring(0, 8).Equals("PDB CODE") Then
                Dim pdbCode = inputLine.Substring(10)
                Console.WriteLine("Found PDB code: " & pdbCode)
                pdb.PDBCode = pdbCode
                Dim instance = ensemble.checkPDBCode(pdbCode, pdb)
                pdb.Instance = instance
            ElseIf lineLength > 7 AndAlso inputLine.Substring(0, 5).Equals("LABEL") Then
                Dim plotLabel = inputLine.Substring(7)
                Console.WriteLine("Label: " & plotLabel)
                pdb.createPlotLabel(plotLabel)
            ElseIf inputLine.Equals("SELECTED") Then
                Dim entryNo = ensemble.getEntryNo(pdb)
                If entryNo > -1 Then
                    selectedEntry = entryNo
                End If
            End If
        End Sub

        Private Sub newResidue(inputLine As String)
            Dim chain = " "c
            Dim moleculeType = 0
            Dim resName As String = Nothing
            Dim resNum As String = Nothing
            Dim haveLabel = False
            Dim labelX = 0.0F
            Dim labelY = 0.0F
            Dim alteredName = ""
            Dim lineLength = inputLine.Length
            If lineLength <= 8 Then
                Return
            End If
            Dim type = inputLine(0)
            Dim numberString As String = type.ToString() & ""
            moleculeType = Integer.Parse(numberString)
            resName = inputLine.Substring(1, 3)
            fullResName = resName
            If lineLength = 8 Then
                resNum = inputLine.Substring(4, 4) & " "
                insCode = " "
                Dim len = resNum.Length
                If len = 5 AndAlso resNum(4) <> " "c Then
                    insCode = resNum(4).ToString() & ""
                    Dim tmp = resNum.Substring(0, 3) & " "
                    resNum = tmp
                End If
                chain = " "c
            ElseIf lineLength = 9 Then
                resNum = inputLine.Substring(4, 5)
                chain = " "c
            Else
                resNum = inputLine.Substring(4, 5)
                chain = inputLine(9)
                If chain = "-"c Then
                    chain = " "c
                End If
            End If
            fullChain = chain.ToString() & ""
            fullResNum = Integer.Parse(resNum.Trim())
            If lineLength > 15 Then
                numberString = inputLine.Substring(11)
                Dim token As Scanner = New Scanner(numberString)
                Dim itoken = 0

                While token.HasNext() = True
                    Dim f As Single?
                    Dim ch As Char
                    numberString = token.Next()
                    Select Case itoken
                        Case 0
                            f = Convert.ToSingle(Single.Parse(numberString))
                            labelX = f.Value
                        Case 1
                            f = Convert.ToSingle(Single.Parse(numberString))
                            labelY = f.Value
                            haveLabel = True
                        Case Else
                            ch = numberString(0)
                            If ch = "#"c Then
                                ch = numberString(1)
                                If ch = "c"c Then
                                    fullChain = numberString.Substring(2)
                                End If
                                Exit Select
                            End If
                            If alteredName.Length > 0 Then
                                alteredName = alteredName & " "
                            End If
                            alteredName = alteredName & numberString
                    End Select

                    itoken += 1
                End While
            End If
            residue = pdb.addResidue(resName, resNum, chain, fullResName, fullResNum, insCode, fullChain, moleculeType)
            If molecule IsNot Nothing Then
                molecule.addResidue(residue)
                molecule.MoleculeType = moleculeType
            End If
            If antibodyField Then
                If Not ReferenceEquals(antibodyLoopID, Nothing) Then
                    residue.AntibodyLoopID = antibodyLoopID
                ElseIf fullChain.Equals(fullHeavyChain) OrElse fullChain.Equals(fullLightChain) Then
                    Dim loopID = ligplus.Params.getAntibodyLoopID(antibodyNumberingScheme, fullChain, fullResNum, fullHeavyChain, fullLightChain)
                    residue.AntibodyLoopID = loopID
                ElseIf fullChain.Equals(fullAntigenChain) OrElse isAntigen Then
                    residue.setAntigen()
                End If
            End If
            If haveLabel Then
                Dim residueLabel = formatResidueName(resName, resNum, chain)
                If alteredName.Length > 0 Then
                    residueLabel = alteredName
                End If
                Dim textType = -1
                If moleculeType = 1 Then
                    textType = 1
                    If molecule.InterFace = 1 Then
                        textType = 21
                    ElseIf molecule.InterFace = 2 Then
                        textType = 22
                    End If
                ElseIf moleculeType = 2 Then
                    textType = 2
                    If molecule.InterFace = 1 Then
                        textType = 21
                    ElseIf molecule.InterFace = 2 Then
                        textType = 22
                    End If
                ElseIf moleculeType = 3 Then
                    textType = 5
                    If molecule.InterFace = 1 Then
                        textType = 19
                    ElseIf molecule.InterFace = 2 Then
                        textType = 20
                    End If
                ElseIf moleculeType = 4 Then
                    textType = 4
                    If programField = RunExe.DIMPLOT Then
                        textType = 24
                    End If
                End If
                If textType <> -1 Then
                    Dim textItem As TextItem = New TextItem(pdb, residueLabel, textType, labelX, labelY)
                    residue.ResidueLabel = textItem
                    molecule.addTextItem(textItem)
                    Dim antibodyLoopID = residue.AntibodyLoopID
                    If Not antibodyLoopID.Equals("") Then
                        textItem.AntibodyLoopID = antibodyLoopID
                    End If
                    If residue.Antigen Then
                        textItem.setIsAntigen()
                    End If
                    textItem.MoleculeID = molecule.MoleculeID
                End If
            End If
        End Sub

        Private Sub newPlotParams(inputLine As String)
            Dim ePos = inputLine.IndexOf("="c)
            If ePos > -1 Then
                Dim parameter = inputLine.Substring(0, ePos)
                Dim value = inputLine.Substring(ePos + 1)
                Dim params = pdb.Params
                params(parameter) = value
            End If
        End Sub

        Private Sub plotTitle(inputLine As String)
            Dim token As Scanner = New Scanner(inputLine)
            Dim nTokens = 0
            While token.HasNext()
                Dim numberString As String = token.Next()
                nTokens += 1
            End While
            If nTokens > 2 Then
                token = New Scanner(inputLine)
                Dim title = ""
                For iToken = 0 To nTokens - 2 - 1
                    If iToken > 0 Then
                        title = title & " "
                    End If
                    title = title & token.Next()
                Next
                Dim numberString As String = token.Next()
                Dim f As Single? = Convert.ToSingle(Single.Parse(numberString))
                Dim x = f.Value
                numberString = token.Next()
                f = Convert.ToSingle(Single.Parse(numberString))
                Dim y = f.Value
                If title.Equals("tmp") OrElse title.Equals("ligplus") Then
                    title = pdb.PDBCode
                    Dim instance = pdb.CodeInstance + 1
                    If instance > 1 Then
                        title = title & "_" & instance.ToString()
                    End If
                End If
                Dim textType = 0
                If programField = RunExe.DIMPLOT Then
                    textType = 23
                End If
                Dim textItem As TextItem = New TextItem(pdb, title, textType, x, y)
                pdb.PDBTitle = textItem
            End If
        End Sub

        Public Overridable ReadOnly Property Antibody As Boolean
            Get
                Return antibodyField
            End Get
        End Property

        Public Overridable ReadOnly Property ErrorMessage As String
            Get
                Return errorMessageField
            End Get
        End Property

        Public Overridable ReadOnly Property LatestPDB As PDBEntry
            Get
                Return pdb
            End Get
        End Property

        Public Overridable ReadOnly Property OrientationOption As Integer
            Get
                Return orientation
            End Get
        End Property

        Public Overridable ReadOnly Property Program As Integer
            Get
                Return programField
            End Get
        End Property
    End Class

End Namespace
