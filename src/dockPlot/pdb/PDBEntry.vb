Imports ligplus.ligplus
Imports ligplus.models
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Imaging

Namespace pdb

    Public Class PDBEntry
        Public Const COVALENT_DIST As Single = 2.0F

        Public Const COVALENT_DIST_SQRD As Single = 4.0F

        Private Const MIN_DNA_LENGTH As Integer = 2

        Public Const INTERACT_DIST As Single = 4.0F

        Private Const MIN_PROTEIN_LENGTH As Integer = 10

        Public Const PDB_FILE As Integer = 0

        Public Const LIGPLOT_DRW_FILE As Integer = 1

        Public Const DIMPLOT_DRW_FILE As Integer = 2

        Private Const X As Integer = 0

        Private Const Y As Integer = 1

        Private Const Z As Integer = 2

        Public Const MAX_NDUMMY As Integer = 2

        Private Const MIN As Integer = 0

        Private Const MAX As Integer = 1

        Private Const MAX_ITEM_LIST As Integer = 15

        Public Shared nextPDBId As Integer = 0

        Private splitShiftOn As Boolean = False

        Private fromMmcifField As Boolean = False

        Private selectedChain As Char()

        Private PDBId_Conflict As Integer = 0

        Private instanceField As Integer = 0

        Private nAtomsField As Integer = 0

        Private nBonds As Integer = 0

        Private nConectField As Integer = 0

        Private nCoords As Integer = 0

        Private nDummyField As Integer = 0

        Private nMoleculesField As Integer = 0

        Private nProtProtField As Integer = 0

        Private nResiduesField As Integer = 0

        Private readFromField As Integer = 0

        Private moleculeTypeCount As Integer()

        Private antibodyNumberingSchemeField As Integer = -1

        Private dummyMin As Single()() = {New Single(1) {}, New Single(1) {}}

        Private dummyMax As Single()() = {New Single(1) {}, New Single(1) {}}

        Private splitShift As Single() = New Single(2) {}

        Private splitScreenOffset As Single() = New Single(2) {}

        Private coordsAccum As Single() = New Single(2) {}

        Private coordsCentre As Single() = New Single(2) {}

        Private coordsMin As Single() = New Single(2) {}

        Private coordsMax As Single() = New Single(2) {}

        Private ensemble As Ensemble = Nothing

        Private dummyMolecule As Molecule() = New Molecule(1) {}

        Private selectedMolecule As Molecule()

        Private originalPDBField As PDBEntry = Nothing



        Private FirstLigandResidue_Conflict As Residue = Nothing


        Private LastLigandResidue_Conflict As Residue = Nothing

        Private scale As Scale = Nothing

        Private domainDefinition As String()

        Private moleculeChains As String()

        Private selectedFullChain As String()

        Private pdbCodeField As String = Nothing

        Private fullTitleField As String = Nothing

        Private pdbTitleField As TextItem = Nothing

        Private plotLabelField As TextItem = Nothing

        Private antibodyLoopLabelField As List(Of TextItem) = Nothing

        Private atomListField As List(Of Atom)

        Private bondListField As List(Of Bond)

        Private conectListField As List(Of Conect)

        Private hetGroupListField As List(Of HetGroup)

        Private hhbnnbListField As List(Of String)

        Private moleculeListField As List(Of Molecule)

        Private residueListField As List(Of Residue)

        Private protProtPairsList As List(Of Molecule)()

        Public Sub New()
            init()
            pdbCodeField = Nothing
        End Sub

        Public Sub New(defaultParams As Properties, pdbCode As String, instance As Integer, ensemble As Ensemble)
            init()
            pdbCodeField = pdbCode
            instanceField = instance
            Me.ensemble = ensemble
            Params = New Properties
            For Each paramKey As String In defaultParams.Keys
                Dim value = defaultParams(paramKey)
                If Not ReferenceEquals(value, Nothing) Then
                    Params(paramKey) = value
                End If
            Next
            Dim plotName As String = formPlotName()
            createPlotLabel(plotName)
        End Sub

        Public Overridable Property PDBTitle As TextItem
            Get
                Return pdbTitleField
            End Get
            Set(value As TextItem)
                pdbTitleField = value
            End Set
        End Property

        Public Overridable Function getOffset(i As Integer) As Single
            Dim offset = scale.getOffset(i) + splitScreenOffset(i)
            If ensemble.SplitShiftOn Then
                offset += splitShift(i)
            End If
            Return offset
        End Function

        Public Overridable Function getSplitScreenOffset(i As Integer) As Single
            Return splitScreenOffset(i)
        End Function

        Public Overridable Function getSplitShift(i As Integer) As Single
            Return splitShift(i)
        End Function

        Public Overridable Sub createAntibodyLoopListLabel(orientation As Integer)
            Dim nLoops = 0
            Dim textType = 14
            Dim haveLoop = New Boolean(5) {}
            Dim loopCoords = RectangularArray.Matrix(Of Single)(6, 2)
            antibodyLoopLabelField = New List(Of TextItem)()
            Dim coordInfo As coordInfo = calcLoopLabelCoords(orientation)
            loopCoords = coordInfo.loopCoords
            haveLoop = coordInfo.haveLoop
            For [loop] = 0 To 5
                If haveLoop([loop]) Then
                    nLoops += 1
                    Dim x = loopCoords([loop])(0)
                    Dim y = loopCoords([loop])(1)
                    Dim label As TextItem = New TextItem(Me, Params.antibodyLoopName([loop]), textType, x, y)
                    antibodyLoopLabelField.Add(label)
                    label.AntibodyLoopID = Params.antibodyLoopID([loop])
                End If
            Next
            If nLoops > 0 Then
                Dim coords = pdbTitleField.Coords
                If orientation = RunExe.LANDSCAPE Then
                    coords(1) = coords(1) - 2.0F
                Else
                    coords(0) = coords(0) + 4.0F
                End If
                pdbTitleField.Coords = coords
            End If
        End Sub

        Private Sub createDummy(coords As Single())
            Dim atom = New Atom(1) {}
            Dim originalCoords = New Single(2) {}
            Dim bValue = 0.0F, occupancy = 0.0F
            Dim element = "  "
            originalCoords(2) = 0.0F
            originalCoords(1) = 0.0F
            originalCoords(0) = 0.0F
            Dim molecule = addMolecule("X"c, "X", "dummy")
            Dim dummyResidue = addResidue("DUM", "   1 ", "X"c, "DUM", 1, " ", "X", 7)
            molecule.addResidue(dummyResidue)
            molecule.MoleculeType = 7
            Dim atomCoords = New Single(2) {}
            atomCoords(0) = coords(1)
            atomCoords(1) = coords(2)
            atomCoords(2) = 0.0F
            atom(0) = addAtom("H"c, nAtomsField, " C1 ", atomCoords, bValue, occupancy, dummyResidue, element, originalCoords)
            atomCoords(0) = coords(3)
            atomCoords(1) = coords(4)
            atom(1) = addAtom("H"c, nAtomsField, " C2 ", atomCoords, bValue, occupancy, dummyResidue, element, originalCoords)
            Dim bond = addBond(atom, 6)
            molecule.updateMaxMinCoords()
        End Sub

        Public Overridable Sub createDummyMolecules(nDummyWanted As Integer, iPDB As Integer)
            If nDummyField > nDummyWanted Then
                For iDummy = nDummyWanted To nDummyField - 1
                    Dim dummy = dummyMolecule(iDummy)
                    For i = 0 To moleculeListField.Count - 1
                        Dim molecule = moleculeListField(i)
                        If molecule Is dummy Then
                            molecule.deleteMolecule(True)
                            moleculeListField.RemoveAt(i)
                        End If
                    Next
                    nDummyField = nDummyWanted
                Next
            ElseIf nDummyField < nDummyWanted Then
                Dim coords As Single()
                If iPDB > 0 Then
                    coords = ensemble.DummyCoords
                Else
                    coords = calcInterfaceLine()
                End If
                For iDummy = nDummyField To nDummyWanted - 1
                    createDummy(coords)
                Next
            End If
        End Sub

        Public Overridable Sub createPlotLabel(plotName As String)
            Dim textType = 12
            Dim x = 0.0F
            Dim y = 0.0F
            plotLabelField = New TextItem(Me, plotName, textType, x, y)
        End Sub

        Public Overridable Sub displayDummies()
            Console.WriteLine("### PDB entry " & instanceField.ToString() & ". Number of dummies " & nDummyField.ToString())
            For iDummy = 0 To nDummyField - 1
                Dim dummy = dummyMolecule(iDummy)
                Console.WriteLine("        Dummy " & iDummy.ToString() & ".  Coords " & dummy.getCoordsMin(0).ToString() & ", " & dummy.getCoordsMin(1).ToString() & " -> " & dummy.getCoordsMax(0).ToString() & ", " & dummy.getCoordsMax(1).ToString())
            Next
        End Sub

        Public Overridable Function findAtom(atomName As String, resName As String, resNum As String, chain As Char) As Atom
            Dim foundAtom As Atom = Nothing
            Dim i = 0

            While i < atomListField.Count AndAlso foundAtom Is Nothing
                Dim atom As Atom = atomListField(i)
                If atom.AtomName.Equals(atomName) Then
                    Dim residue = atom.Residue
                    If residue.Chain = chain AndAlso residue.ResNum.Equals(resNum) AndAlso residue.ResName.Equals(resName) Then
                        foundAtom = atom
                    End If
                End If

                i += 1
            End While
            Return foundAtom
        End Function

        Public Overridable Function findFullAtom(fullAtomName As String, fullResName As String, fullResNum As Integer, fullInsCode As String, fullChain As String) As Atom
            Dim foundAtom As Atom = Nothing
            Dim i = 0

            While i < atomListField.Count AndAlso foundAtom Is Nothing
                Dim atom As Atom = atomListField(i)
                If atom.FullAtomName.Equals(fullAtomName) Then
                    Dim residue = atom.Residue
                    If residue.FullChain.Equals(fullChain) AndAlso residue.FullResNum = fullResNum AndAlso residue.FullInsCode.Equals(fullInsCode) AndAlso residue.FullResName.Equals(fullResName) Then
                        foundAtom = atom
                    End If
                End If

                i += 1
            End While
            Return foundAtom
        End Function

        Private Sub init()
            selectedChain = New Char(1) {}
            selectedFullChain = New String(1) {}
            selectedMolecule = New Molecule(1) {}
            Dim i As Integer
            For i = 0 To 1
                selectedChain(i) = " "c
                selectedFullChain(i) = " "
                selectedMolecule(i) = Nothing
            Next
            domainDefinition = New String(1) {}
            For i = 0 To 1
                domainDefinition(i) = ""
            Next
            moleculeChains = New String(7) {}
            moleculeTypeCount = New Integer(7) {}
            For i = 0 To 7
                moleculeChains(i) = ""
                moleculeTypeCount(i) = 0
            Next
            atomListField = New List(Of Atom)()
            bondListField = New List(Of Bond)()
            conectListField = New List(Of Conect)()
            hetGroupListField = New List(Of HetGroup)()
            hhbnnbListField = New List(Of String)()
            moleculeListField = New List(Of Molecule)()
            residueListField = New List(Of Residue)()
            For i = 0 To 2
                coordsMin(i) = 0.0F
                coordsMax(i) = 0.0F
                coordsCentre(i) = 0.0F
                splitScreenOffset(i) = 0.0F
                splitShift(i) = 0.0F
            Next
            nCoords = 0
            nDummyField = 0
            PDBId_Conflict = nextPDBId
            nextPDBId += 1
        End Sub

        Public Overridable Sub addAntibodyLoopLabel(label As TextItem)
            If antibodyLoopLabelField Is Nothing Then
                antibodyLoopLabelField = New List(Of TextItem)()
            End If
            antibodyLoopLabelField.Add(label)
        End Sub

        Public Overridable Function addAtom(atHet As Char, atomNumber As Integer, atomName As String, coords As Single(), bValue As Single, occupancy As Single, residue As Residue, element As String, originalCoords As Single()) As Atom
            Dim atom As Atom = New Atom(Me, atHet, atomNumber, atomName, coords, bValue, occupancy, residue, element, originalCoords)
            atomListField.Add(atom.Object)
            nAtomsField += 1
            Return atom
        End Function

        Public Overridable Function addBond(atom As Atom(), type As Integer) As Bond
            Dim bond As Bond = New Bond(Me, atom, type)
            bondListField.Add(bond.Object)
            nBonds += 1
            Return bond
        End Function

        Public Overridable Function addResidue(resName As String, resNum As String, chain As Char, fullResName As String, fullResNum As Integer, insCode As String, fullChain As String, moleculeType As Integer) As Residue
            Dim residue As Residue = New Residue(Me, resName, resNum, chain, fullResName, fullResNum, insCode, fullChain, ensemble, moleculeType)
            residueListField.Add(residue.Object)
            nResiduesField += 1
            Return residue
        End Function

        Friend Overridable Sub addHetGroup(hetName As String, hetDescription As String)
            Dim hetGroup As HetGroup = New HetGroup(hetName, hetDescription)
            hetGroupListField.Add(hetGroup.Object)
        End Sub

        Public Overridable Sub addHHBNNB(inputLine As String)
            hhbnnbListField.Add(inputLine)
        End Sub

        Public Overridable Function addMolecule(chain As Char, fullChain As String, moleculeID As String) As Molecule
            nMoleculesField += 1
            If moleculeID.Equals("none") Then
                moleculeID = "M" & PDBId_Conflict.ToString() & "_" & nMoleculesField.ToString()
            End If
            Dim molecule As Molecule = New Molecule(chain, fullChain, Me, moleculeID, Params)
            If moleculeID.Equals("dummy") Then
                If nDummyField < 2 Then
                    dummyMolecule(nDummyField) = molecule
                    nDummyField += 1
                End If
            End If
            moleculeListField.Add(molecule.Object)
            Return molecule
        End Function

        Friend Overridable Sub appendHetDescription(hetName As String, description As String)
            Dim hetGroup As HetGroup = Nothing
            Dim i = 0

            While i < hetGroupListField.Count AndAlso hetGroup Is Nothing
                Dim het As HetGroup = hetGroupListField(i)
                If hetName.Equals(het.HetName) Then
                    hetGroup = het
                End If

                i += 1
            End While
            If hetGroup IsNot Nothing Then
                hetGroup.appendHetDescription(description)
            End If
        End Sub

        Public Overridable Sub applyDimShifts(cutPointX As Single, cutPointY As Single, dimShiftX As Single, dimShiftY As Single)
            Dim i As Integer
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If molecule.MoleculeType <> 7 Then
                    molecule.applyDimShifts(cutPointX, cutPointY, dimShiftX, dimShiftY)
                End If
            Next
            If antibodyLoopLabelField IsNot Nothing Then
                For i = 0 To antibodyLoopLabelField.Count - 1
                    Dim label As TextItem = antibodyLoopLabelField(i)
                    Dim arrayOfFloat = label.Coords
                    If arrayOfFloat(0) > cutPointX OrElse arrayOfFloat(1) < cutPointY Then
                        label.applyDimShifts(dimShiftX, dimShiftY)
                    End If
                Next
            End If
            Dim coords = pdbTitleField.Coords
            pdbTitleField.applyDimShifts(dimShiftX / 2.0F, dimShiftY)
        End Sub

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim clickObject As Object = Nothing
            If x < coordsMin(0) OrElse x > coordsMax(0) OrElse y < coordsMin(1) OrElse y > coordsMax(1) Then
                Return clickObject
            End If
            If pdbTitleField IsNot Nothing Then
                clickObject = pdbTitleField.clickCheck(x, y)
            End If
            If antibodyLoopLabelField IsNot Nothing AndAlso clickObject Is Nothing Then
                Dim j = 0

                While j < antibodyLoopLabelField.Count AndAlso clickObject Is Nothing
                    Dim label As TextItem = antibodyLoopLabelField(j)
                    clickObject = label.clickCheck(x, y)
                    j += 1
                End While
            End If
            Dim i As Integer
            i = 0

            While i < moleculeListField.Count AndAlso clickObject Is Nothing
                Dim molecule = moleculeListField(i)
                clickObject = molecule.clickCheck(x, y)
                i += 1
            End While
            i = 0

            While i < bondListField.Count AndAlso clickObject Is Nothing
                Dim bond As Bond = bondListField(i)
                clickObject = bond.clickCheck(x, y)
                i += 1
            End While
            Return clickObject
        End Function

        Public Overridable Function getMoleculeTypeCount(type As Integer) As Integer
            Return moleculeTypeCount(type)
        End Function

        Public Overridable Function getMoleculeChains(type As Integer) As String
            Return moleculeChains(type)
        End Function

        Public Overridable Property PDBId As Integer
            Get
                Return PDBId_Conflict
            End Get
            Set(value As Integer)
                PDBId_Conflict = value
                If value >= nextPDBId Then
                    nextPDBId = value + 1
                End If
            End Set
        End Property

        Friend Overridable Sub addConect(atom1 As Atom, atom2 As Atom)
            Dim conect As Conect = New Conect(atom1, atom2)
            conectListField.Add(conect)
            nConectField += 1
        End Sub

        Public Overridable Function calcLoopLabelCoords(orientation As Integer) As coordInfo
            Dim iCoord As Integer
            Dim titleMinMax As Single
            Dim coordInfo As New coordInfo()
            Dim haveLoop = New Boolean(5) {}
            Dim loopMin = New Single(5) {}
            Dim loopMax = New Single(5) {}
            Dim loopCoords = RectangularArray.Matrix(Of Single)(6, 2)
            Dim overallMinMax = {New Single(1) {}, New Single(1) {}}
            overallMinMax(0)(0) = Single.MaxValue
            overallMinMax(0)(1) = -3.40282347E+38F
            overallMinMax(1)(0) = Single.MaxValue
            overallMinMax(1)(1) = -3.40282347E+38F
            For j = 0 To 5
                loopMin(j) = Single.MaxValue
                loopMax(j) = -3.40282347E+38F
                loopCoords(j)(1) = 0.0F
                loopCoords(j)(0) = 0.0F
                haveLoop(j) = False
            Next
            If orientation = RunExe.LANDSCAPE Then
                iCoord = 0
                titleMinMax = Single.MaxValue
            Else
                iCoord = 1
                titleMinMax = -3.40282347E+38F
            End If
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If orientation = RunExe.LANDSCAPE Then
                    Dim coord = molecule.getCoordsMin(1)
                    If coord < titleMinMax Then
                        titleMinMax = coord
                    End If
                Else
                    Dim coord = molecule.getCoordsMax(0)
                    If coord > titleMinMax Then
                        titleMinMax = coord
                    End If
                End If
                For k = 0 To 1
                    If molecule.getCoordsMin(k) < overallMinMax(k)(0) Then
                        overallMinMax(k)(0) = molecule.getCoordsMin(k)
                    End If
                    If molecule.getCoordsMax(k) > overallMinMax(k)(1) Then
                        overallMinMax(k)(1) = molecule.getCoordsMax(k)
                    End If
                Next
                Dim residue = molecule.FirstResidue
                Dim antibodyLoopID = residue.AntibodyLoopID
                If Not ReferenceEquals(antibodyLoopID, Nothing) Then
                    Dim m = -1
                    Dim n = 0

                    While n < 6 AndAlso m = -1
                        If antibodyLoopID.Equals(Params.antibodyLoopID(n)) Then
                            m = n
                        End If

                        n += 1
                    End While
                    If m > -1 Then
                        haveLoop(m) = True
                        Dim minCoord = molecule.getCoordsMin(iCoord)
                        Dim maxCoord = molecule.getCoordsMax(iCoord)
                        If minCoord < loopMin(m) Then
                            loopMin(m) = minCoord
                        End If
                        If maxCoord > loopMax(m) Then
                            loopMax(m) = maxCoord
                        End If
                    End If
                End If
            Next
            If orientation = RunExe.LANDSCAPE Then
                titleMinMax -= 1
            Else
                titleMinMax += 2.0F
            End If
            For [loop] = 0 To 5
                If haveLoop([loop]) Then
                    If orientation = RunExe.LANDSCAPE Then
                        loopCoords([loop])(0) = (loopMin([loop]) + loopMax([loop])) / 2.0F
                        loopCoords([loop])(1) = titleMinMax
                    Else
                        loopCoords([loop])(0) = titleMinMax
                        loopCoords([loop])(1) = (loopMin([loop]) + loopMax([loop])) / 2.0F
                    End If
                End If
            Next
            coordInfo.loopCoords = loopCoords
            coordInfo.haveLoop = haveLoop
            coordInfo.loopMin = loopMin
            coordInfo.loopMax = loopMax
            coordInfo.overallMinMax = overallMinMax
            Return coordInfo
        End Function

        Public Overridable Function calcLoopCoordRanges(orientation As Integer) As coordInfo
            Dim iCoord As Integer
            Dim coordInfo As New coordInfo
            Dim haveLoop = New Boolean(5) {}
            Dim loopMin = New Single(5) {}
            Dim loopMax = New Single(5) {}
            Dim overallMinMax = {New Single(1) {}, New Single(1) {}}
            overallMinMax(0)(0) = Single.MaxValue
            overallMinMax(0)(1) = Single.MaxValue
            overallMinMax(1)(0) = -3.40282347E+38F
            overallMinMax(1)(1) = -3.40282347E+38F
            For [loop] = 0 To 5
                loopMin([loop]) = Single.MaxValue
                loopMax([loop]) = -3.40282347E+38F
                haveLoop([loop]) = False
            Next
            If orientation = RunExe.LANDSCAPE Then
                iCoord = 0
            Else
                iCoord = 1
            End If
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                For jCoord = 0 To 1
                    If molecule.getCoordsMin(jCoord) < overallMinMax(0)(jCoord) Then
                        overallMinMax(0)(jCoord) = molecule.getCoordsMin(jCoord)
                    End If
                    If molecule.getCoordsMax(jCoord) > overallMinMax(1)(jCoord) Then
                        overallMinMax(1)(jCoord) = molecule.getCoordsMax(jCoord)
                    End If
                Next
                Dim residue = molecule.FirstResidue
                Dim antibodyLoopID = residue.AntibodyLoopID
                If Not ReferenceEquals(antibodyLoopID, Nothing) Then
                    Dim k = -1
                    Dim j = 0

                    While j < 6 AndAlso k = -1
                        If antibodyLoopID.Equals(Params.antibodyLoopID(j)) Then
                            k = j
                        End If

                        j += 1
                    End While
                    If k > -1 Then
                        haveLoop(k) = True
                        Dim minCoord = molecule.getCoordsMin(iCoord)
                        Dim maxCoord = molecule.getCoordsMax(iCoord)
                        If minCoord < loopMin(k) Then
                            loopMin(k) = minCoord
                        End If
                        If maxCoord > loopMax(k) Then
                            loopMax(k) = maxCoord
                        End If
                    End If
                End If
            Next
            coordInfo.haveLoop = haveLoop
            coordInfo.loopMin = loopMin
            coordInfo.loopMax = loopMax
            coordInfo.overallMinMax = overallMinMax
            Return coordInfo
        End Function

        Public Class coordInfo
            Public Property overallMinMax As Single()()
            Public Property loopMax As Single()
            Public Property loopMin As Single()
            Public Property haveLoop As Boolean()
            Public Property loopCoords As Single()()
        End Class

        Private Function checkAttachment(residue As Residue) As Residue
            Dim parentResidue As Residue = Nothing
            Dim fullChain = residue.FullChain
            Dim fullResName = residue.FullResName
            Dim i = 0

            While i < moleculeListField.Count AndAlso parentResidue Is Nothing
                Dim molecule = moleculeListField(i)
                If fullChain.Equals(molecule.FullChain) Then
                    Dim moleculeResidueList As List(Of Residue) = molecule.ResidueList
                    Dim j = 0
                    While j < moleculeResidueList.Count AndAlso parentResidue Is Nothing
                        Dim moleculeResidue = moleculeResidueList(j)
                        If fullResName.Equals(moleculeResidue.FullResName) Then
                            Dim connected = checkLinkingBond(residue, moleculeResidue)
                            If connected Then
                                parentResidue = moleculeResidue
                            End If
                        End If

                        j += 1
                    End While
                End If

                i += 1
            End While
            Return parentResidue
        End Function

        Private Function checkConnected(residue As Residue, lastResidue As Residue) As Boolean
            Dim connected = False
            Dim inRange = checkInRange(residue, lastResidue, 4.0F)
            If inRange Then
                connected = checkLinkingBond(residue, lastResidue)
            End If
            Return connected
        End Function

        Public Overridable Function checkInRange(residue1 As Residue, residue2 As Residue, margin As Single) As Boolean
            Dim inRange = True
            Dim i = 0

            While i < 3 AndAlso inRange = True
                If residue1.getCoordsMin(i) - residue2.getCoordsMax(i) > margin Then
                    inRange = False
                End If
                If residue2.getCoordsMin(i) - residue1.getCoordsMax(i) > margin Then
                    inRange = False
                End If

                i += 1
            End While
            Return inRange
        End Function

        Private Function checkInteracting(residue1 As Residue, residue2 As Residue) As Boolean
            Dim interacting = False
            Dim interactDist = 4.0F
            Dim residueAtomList1 As List(Of Atom) = residue1.AtomList
            Dim residueAtomList2 As List(Of Atom) = residue2.AtomList
            Dim numberString = "3.90"
            Try
                interactDist = Single.Parse(numberString)
            Catch __unusedFormatException1__ As FormatException
                interactDist = 4.0F
            End Try
            Dim interactDistSqrd = interactDist * interactDist
            Dim i = 0

            While i < residueAtomList1.Count AndAlso Not interacting
                Dim atom1 = residueAtomList1(i)
                Dim inRange = True
                If atom1.Element.Equals(" H") Then
                    inRange = False
                End If
                Dim icoord = 0

                While icoord < 3 AndAlso inRange = True
                    If atom1.getCoord(icoord) - residue2.getCoordsMax(icoord) > interactDist Then
                        inRange = False
                    End If
                    If residue2.getCoordsMin(icoord) - atom1.getCoord(icoord) > interactDist Then
                        inRange = False
                    End If

                    icoord += 1
                End While
                If inRange Then
                    Dim j = 0

                    While j < residueAtomList2.Count AndAlso Not interacting
                        Dim atom2 = residueAtomList2(j)
                        Dim distSqrd = 0.0F
                        For k = 0 To 2
                            distSqrd += (atom1.getCoord(k) - atom2.getCoord(k)) * (atom1.getCoord(k) - atom2.getCoord(k))
                        Next
                        If distSqrd < interactDistSqrd Then
                            interacting = True
                        End If
                        If atom2.Element.Equals(" H") Then
                            interacting = False
                        End If

                        j += 1
                    End While
                End If

                i += 1
            End While
            Return interacting
        End Function

        Private Function checkLinkingBond(residue1 As Residue, residue2 As Residue) As Boolean
            Dim haveBond = False
            Dim residueAtomList1 As List(Of Atom) = residue1.AtomList
            Dim residueAtomList2 As List(Of Atom) = residue2.AtomList
            Dim i = 0

            While i < residueAtomList1.Count AndAlso Not haveBond
                Dim atom1 = residueAtomList1(i)
                Dim inRange = True
                If atom1.Element.Equals(" H") Then
                    inRange = False
                End If
                Dim icoord = 0

                While icoord < 3 AndAlso inRange = True
                    If atom1.getCoord(icoord) - residue2.getCoordsMax(icoord) > 4.0F Then
                        inRange = False
                    End If
                    If residue2.getCoordsMin(icoord) - atom1.getCoord(icoord) > 4.0F Then
                        inRange = False
                    End If

                    icoord += 1
                End While
                If inRange Then
                    Dim j = 0

                    While j < residueAtomList2.Count AndAlso Not haveBond
                        Dim atom2 = residueAtomList2(j)
                        Dim distSqrd = 0.0F
                        For k = 0 To 2
                            distSqrd += (atom1.getCoord(k) - atom2.getCoord(k)) * (atom1.getCoord(k) - atom2.getCoord(k))
                        Next
                        If distSqrd < 4.0F Then
                            haveBond = True
                        End If
                        If atom2.Element.Equals(" H") Then
                            haveBond = False
                        End If

                        j += 1
                    End While
                End If

                i += 1
            End While
            Return haveBond
        End Function

        Private Function checkMoleculesInRange(molecule1 As Molecule, molecule2 As Molecule, margin As Single) As Boolean
            Dim inRange = True
            Dim i = 0

            While i < 3 AndAlso inRange = True
                If molecule1.getCoordsMin(i) - molecule2.getCoordsMax(i) > margin Then
                    inRange = False
                End If
                If molecule2.getCoordsMin(i) - molecule1.getCoordsMax(i) > margin Then
                    inRange = False
                End If

                i += 1
            End While
            Return inRange
        End Function

        Private Function checkMoleculesInteracting(molecule1 As Molecule, molecule2 As Molecule, margin As Single) As Boolean
            Dim inRange = False
            Dim interacting = False
            Dim residueList1 As List(Of Residue) = molecule1.ResidueList
            Dim residueList2 As List(Of Residue) = molecule2.ResidueList
            Dim i = 0

            While i < residueList1.Count AndAlso Not interacting
                Dim residue1 = residueList1(i)
                Dim j = 0

                While j < residueList2.Count AndAlso Not interacting
                    Dim residue2 = residueList2(j)
                    inRange = checkInRange(residue1, residue2, margin)
                    If inRange Then
                        interacting = checkInteracting(residue1, residue2)
                    End If

                    j += 1
                End While

                i += 1
            End While
            Return interacting
        End Function

        Private Sub classifyMolecules()
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If molecule.MoleculeType = 0 Then
                    Dim nRes = molecule.NResidues
                    If molecule.AtomRecCount = 0 Then
                        molecule.MoleculeType = 4
                    ElseIf molecule.getResidueTypeCount(1) = 0 AndAlso molecule.getResidueTypeCount(2) = 0 AndAlso molecule.getResidueTypeCount(3) = 0 Then
                        molecule.MoleculeType = 4
                    ElseIf molecule.getResidueTypeCount(1) > 0 OrElse molecule.getResidueTypeCount(2) > 0 Then
                        molecule.MoleculeType = 1
                    ElseIf molecule.getResidueTypeCount(3) > 0 Then
                        molecule.MoleculeType = 3
                    Else
                        molecule.MoleculeType = 4
                    End If
                End If
            Next
        End Sub

        Private Sub fixChainBreaks()
            Dim i = 0
            Dim finished = False
            While Not finished
                Dim molecule = moleculeListField(i)
                Dim firstLength = molecule.NResidues
                Dim done = False
                Dim join = -1
                If molecule.MoleculeType = 1 AndAlso firstLength >= 10 OrElse molecule.MoleculeType = 3 AndAlso firstLength >= 2 Then
                    join = i
                End If
                Dim j = i + 1

                While j < moleculeListField.Count AndAlso Not done AndAlso join < i + 1
                    Dim nextMolecule = moleculeListField(j)
                    If molecule.MoleculeType = nextMolecule.MoleculeType AndAlso molecule.FullChain.Equals(nextMolecule.FullChain) AndAlso (molecule.MoleculeType = 1 OrElse molecule.MoleculeType = 3) Then
                        Dim length = nextMolecule.NResidues
                        If join > -1 OrElse molecule.MoleculeType = 1 AndAlso length >= 10 OrElse molecule.MoleculeType = 3 AndAlso length >= 2 Then
                            join = j
                        End If
                    Else
                        done = True
                    End If

                    j += 1
                End While
                If join > i Then
                    Dim nextMolecule = moleculeListField(i + 1)
                    joinMolecules(molecule, nextMolecule)
                    moleculeListField.RemoveAt(i + 1)
                    i -= 1
                End If
                i += 1
                If i >= moleculeListField.Count Then
                    finished = True
                End If
            End While
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If molecule.MoleculeType = 1 AndAlso molecule.NResidues < 10 OrElse molecule.MoleculeType = 3 AndAlso molecule.NResidues < 2 Then
                    molecule.MoleculeType = 4
                End If
                If molecule.MoleculeType = 1 AndAlso molecule.NAtoms < 2 * molecule.NResidues Then
                    molecule.MoleculeType = 2
                End If
                Dim type = molecule.MoleculeType
                If type > -1 AndAlso type < 8 Then
                    Dim chainStr As String
                    If moleculeTypeCount(type) > 0 AndAlso moleculeTypeCount(type) <= 15 Then
                        moleculeChains(type) = moleculeChains(type) & ", "
                    End If
                    Dim fullChain = molecule.FullChain
                    If fullChain.Equals(" ", StringComparison.OrdinalIgnoreCase) Then
                        chainStr = "[ ]"
                    Else
                        chainStr = "" & fullChain
                    End If
                    If moleculeTypeCount(type) = 15 Then
                        moleculeChains(type) = moleculeChains(type) & "+ more"
                    ElseIf moleculeTypeCount(type) < 15 Then
                        moleculeChains(type) = moleculeChains(type) & chainStr
                    End If
                    moleculeTypeCount(type) = moleculeTypeCount(type) + 1
                    If type = 6 Then
                        moleculeTypeCount(type) = molecule.NResidues
                    End If
                End If
            Next
        End Sub

        Public Overridable Sub fixDuplicates()
            For iRes = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(iRes)
                Dim residueAtomList As List(Of Atom) = residue.AtomList
                For iAtom = 0 To residueAtomList.Count - 1
                    Dim atom1 = residueAtomList(iAtom)
                    Dim haveDuplicates = False
                    Dim jAtom = iAtom + 1
                    While jAtom < residueAtomList.Count AndAlso Not haveDuplicates
                        Dim atom2 = residueAtomList(jAtom)
                        If atom1.AtomName.Equals(atom2.AtomName) Then
                            haveDuplicates = True
                        End If

                        jAtom += 1
                    End While
                    If haveDuplicates Then
                        Dim count = 0
                        Dim atName = atom1.AtomName
                        If atName(2) >= "0"c AndAlso atName(2) <= "9"c Then
                            Dim numberString = atName.Substring(2)
                            If numberString(1) = " "c OrElse numberString(1) < "0"c OrElse numberString(1) > "9"c Then
                                numberString = "" & atName(2).ToString()
                            End If
                            count = Integer.Parse(numberString) - 1
                        End If
                        Console.Write("HAVE DUPLICATES in residue {0} {1} {2} for atom {3}" & vbLf, residue.ResName, residue.ResNum, Convert.ToChar(residue.Chain), atName)
                        For i = iAtom To residueAtomList.Count - 1
                            Dim atom2 = residueAtomList(i)
                            If atom2.AtomName.Equals(atName) Then
                                count += 1
                                Dim newName = atName.Substring(0, 2)
                                If atName(0) = " "c AndAlso atName(2) <> " "c AndAlso (atName(2) < "0"c OrElse atName(2) > "9"c) Then
                                    newName = atName.Substring(1, 2)
                                End If
                                If count < 10 Then
                                    newName = newName & count.ToString() & " "
                                ElseIf count < 100 Then
                                    newName = newName & count.ToString()
                                Else
                                    newName = newName & "XX"
                                End If
                                atom2.AtomName = newName
                            End If
                        Next
                    End If
                Next
            Next
        End Sub

        Public Overridable Sub fixLoopCoords(fixLoop As Integer, coords As Single())
            For [loop] = 0 To antibodyLoopLabelField.Count - 1
                Dim label As TextItem = antibodyLoopLabelField([loop])
                If label.Text.Equals(Params.antibodyLoopName(fixLoop)) Then
                    label.Coords = coords
                End If
            Next
        End Sub

        Public Overridable Sub flagShortestHphobics()
            For i = 0 To bondListField.Count - 1
                Dim bond As Bond = bondListField(i)
                bond.Done = False
                bond.ShortestBond = False
            Next
            For j = 0 To bondListField.Count - 1
                Dim bond As Bond = bondListField(j)
                If (bond.Type = 2 OrElse bond.Type = 1) AndAlso Not bond.Done Then
                    bond.Done = True
                    Dim haveHbond = False
                    If bond.Type = 1 Then
                        haveHbond = True
                    End If
                    Dim molecule1 = New Molecule(1) {}
                    molecule1(0) = bond.Molecule
                    molecule1(1) = bond.OtherMolecule
                    Dim length = bond.Length
                    Dim shortestLength = length
                    Dim shortestBond = bond
                    For k = j + 1 To bondListField.Count - 1
                        Dim otherBond As Bond = bondListField(k)
                        If (otherBond.Type = 2 OrElse otherBond.Type = 1) AndAlso Not otherBond.Done Then
                            Dim molecule2 = New Molecule(1) {}
                            molecule2(0) = otherBond.Molecule
                            molecule2(1) = otherBond.OtherMolecule
                            If molecule1(0) Is molecule2(0) AndAlso molecule1(1) Is molecule2(1) OrElse molecule1(0) Is molecule2(1) AndAlso molecule1(1) Is molecule2(0) Then
                                otherBond.Done = True
                                If otherBond.Type = 1 Then
                                    haveHbond = True
                                End If
                                length = otherBond.Length
                                If length < shortestLength Then
                                    shortestLength = length
                                    shortestBond = otherBond
                                End If
                            End If
                        End If
                    Next
                    If Not haveHbond Then
                        shortestBond.ShortestBond = True
                    End If
                End If
            Next
        End Sub

        Private Function formPlotName() As String
            Dim name = pdbCodeField
            If instanceField > 0 Then
                Dim inst = instanceField + 1
                name = name & "_" & inst.ToString()
            End If
            Return name
        End Function

        Public Overridable ReadOnly Property AntibodyLoopLabel As List(Of TextItem)
            Get
                Return antibodyLoopLabelField
            End Get
        End Property

        Public Overridable Property AntibodyNumberingScheme As Integer
            Get
                Return antibodyNumberingSchemeField
            End Get
            Set(value As Integer)
                antibodyNumberingSchemeField = value
            End Set
        End Property

        Public Overridable Function getAtom(atomNumber As Integer) As Atom
            Dim matchedAtom As Atom = Nothing
            Dim i = 0

            While i < atomListField.Count AndAlso matchedAtom Is Nothing
                Dim atom As Atom = atomListField(i)
                If atom.AtomNumber = atomNumber Then
                    matchedAtom = atom
                End If

                i += 1
            End While
            Return matchedAtom
        End Function

        Public Overridable ReadOnly Property AtomList As List(Of Atom)
            Get
                Return atomListField
            End Get
        End Property

        Public Overridable ReadOnly Property BondList As List(Of Bond)
            Get
                Return bondListField
            End Get
        End Property

        Public Overridable ReadOnly Property CodeInstance As Integer
            Get
                Return instanceField
            End Get
        End Property

        Public Overridable ReadOnly Property ConectList As List(Of Conect)
            Get
                Return conectListField
            End Get
        End Property

        Public Overridable Function getCoordsAccum(i As Integer) As Single
            Return coordsAccum(i)
        End Function

        Public Overridable Function getCoordsMax(i As Integer) As Single
            Return coordsMax(i)
        End Function

        Public Overridable Function getCoordsMin(i As Integer) As Single
            Return coordsMin(i)
        End Function

        Public Overridable Function getDomainDefinition(domain As Integer) As String
            Return domainDefinition(domain)
        End Function

        Friend Overridable Function getDummyMolecule(i As Integer) As Molecule
            Return dummyMolecule(i)
        End Function

        Public Overridable Property FirstLigandResidue As Residue
            Get
                Return FirstLigandResidue_Conflict
            End Get
            Set(value As Residue)
                FirstLigandResidue_Conflict = value
            End Set
        End Property

        Public Overridable ReadOnly Property HetGroupList As List(Of HetGroup)
            Get
                Return hetGroupListField
            End Get
        End Property

        Public Overridable ReadOnly Property HHBNNBList As List(Of String)
            Get
                Return hhbnnbListField
            End Get
        End Property

        Public Overridable Property LastLigandResidue As Residue
            Get
                Return LastLigandResidue_Conflict
            End Get
            Set(value As Residue)
                LastLigandResidue_Conflict = value
            End Set
        End Property

        Public Overridable ReadOnly Property MoleculeList As List(Of Molecule)
            Get
                Return moleculeListField
            End Get
        End Property

        Public Overridable ReadOnly Property NDummy As Integer
            Get
                Return nDummyField
            End Get
        End Property

        Public Overridable ReadOnly Property Nresidues As Integer
            Get
                Return nResiduesField
            End Get
        End Property

        Public Overridable ReadOnly Property Natoms As Integer
            Get
                Return nAtomsField
            End Get
        End Property

        Public Overridable ReadOnly Property Nconect As Integer
            Get
                Return nConectField
            End Get
        End Property

        Friend Overridable ReadOnly Property Nmolecules As Integer
            Get
                Return nMoleculesField
            End Get
        End Property

        Public Overridable ReadOnly Property NProtProt As Integer
            Get
                Return nProtProtField
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property

        Public Overridable ReadOnly Property Params As Properties

        Public Overridable Property PDBCode As String
            Get
                Return pdbCodeField
            End Get
            Set(value As String)
                pdbCodeField = value
            End Set
        End Property

        Public Overridable ReadOnly Property PlotLabel As TextItem
            Get
                Return plotLabelField
            End Get
        End Property

        Public Overridable Function getProtPairsList(i As Integer) As List(Of Molecule)
            Return protProtPairsList(i)
        End Function

        Public Overridable ReadOnly Property ResidueList As List(Of Residue)
            Get
                Return residueListField
            End Get
        End Property

        Public Overridable Function getSelectedChain(ichain As Integer) As Char
            Return selectedChain(ichain)
        End Function

        Public Overridable Function getSelectedMolecule(ichain As Integer) As Molecule
            Return selectedMolecule(ichain)
        End Function

        Public Overridable Property FullTitle As String
            Get
                Return fullTitleField
            End Get
            Set(value As String)
                fullTitleField = value
            End Set
        End Property

        Public Overridable Function haveDummyMolecule() As Boolean
            If nDummyField = 0 Then
                Return False
            End If
            Return True
        End Function

        Public Overridable Sub identifyMolecules()
            Dim lastFirstAtom As Atom = Nothing
            Dim connected = False
            Dim lastChain = Char.MinValue
            Dim molecule As Molecule = Nothing
            Dim lastResidue As Residue = Nothing
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                Dim metal = False
                Dim newMolecule = False
                Dim residueAttachment = False
                Dim water = False
                Dim parentResidue As Residue = Nothing
                Dim chain = residue.Chain
                Dim fullChain = residue.FullChain
                Dim residueAtomList As List(Of Atom) = residue.AtomList
                Dim firstAtom = residueAtomList(0)
                If lastResidue Is Nothing Then
                    newMolecule = True
                ElseIf chain <> lastChain Then
                    newMolecule = True
                ElseIf residue.PostTER AndAlso firstAtom.AtHet <> lastFirstAtom.AtHet Then
                    newMolecule = True
                End If
                If residue.ResidueType = 5 Then
                    water = True
                    If molecule IsNot Nothing AndAlso molecule.MoleculeType = 6 Then
                        newMolecule = False
                    Else
                        newMolecule = True
                    End If
                End If
                If residue.ResidueType = 4 Then
                    metal = True
                    newMolecule = True
                End If
                If molecule IsNot Nothing AndAlso molecule.MoleculeType = 5 Then
                    newMolecule = True
                End If
                If newMolecule AndAlso firstAtom.AtHet = "H"c AndAlso Not water AndAlso Not metal Then
                    parentResidue = checkAttachment(residue)
                    If parentResidue IsNot Nothing Then
                        parentResidue.addAttachment(residue)
                        residue.ResidueType = 6
                        residueAttachment = True
                    End If
                End If
                If Not newMolecule AndAlso Not water Then
                    connected = checkConnected(residue, lastResidue)
                    If Not connected Then
                        newMolecule = True
                    End If
                End If
                If newMolecule Then
                    nMoleculesField += 1
                    Dim moleculeID As String = "M" & PDBId_Conflict.ToString() & "_" & nMoleculesField.ToString()
                    molecule = New Molecule(chain, fullChain, Me, moleculeID, Params)
                    moleculeListField.Add(molecule.Object)
                    If water Then
                        molecule.MoleculeType = 6
                    ElseIf metal Then
                        molecule.MoleculeType = 5
                    End If
                End If
                If molecule IsNot Nothing AndAlso Not residueAttachment Then
                    molecule.addResidue(residue)
                    molecule.updateAtomHetatmCount(firstAtom.AtHet)
                End If
                lastChain = chain
                lastFirstAtom = firstAtom
                lastResidue = residue
            Next
        End Sub

        Private Sub identifyProtProtInteractions()
            Dim interacting = False
            protProtPairsList = New List(Of Molecule)(1) {}
            Dim i As Integer
            For i = 0 To 1
                protProtPairsList(0) = New List(Of Molecule)()
                protProtPairsList(1) = New List(Of Molecule)()
            Next
            nProtProtField = 0
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If Not molecule.Deleted AndAlso molecule.MoleculeType = 1 Then
                    For j = i + 1 To moleculeListField.Count - 1
                        Dim otherMolecule = moleculeListField(j)
                        If otherMolecule IsNot molecule AndAlso Not otherMolecule.Deleted AndAlso otherMolecule.MoleculeType = 1 Then
                            interacting = False
                            Dim inRange = checkMoleculesInRange(molecule, otherMolecule, 4.0F)
                            If inRange Then
                                interacting = checkMoleculesInteracting(molecule, otherMolecule, 4.0F)
                            End If
                            If interacting Then
                                protProtPairsList(0).Add(molecule)
                                protProtPairsList(1).Add(otherMolecule)
                                nProtProtField += 1
                            End If
                        End If
                    Next
                End If
            Next
        End Sub

        Public Overridable Function identifyResidue(residueId As String) As Residue
            Dim matchResidue As Residue = Nothing
            Dim iRes = 0
            While iRes < residueListField.Count AndAlso matchResidue Is Nothing
                Dim residue As Residue = residueListField(iRes)
                If residueId.Equals(residue.ResidueId) Then
                    matchResidue = residue
                End If

                iRes += 1
            End While
            Return matchResidue
        End Function

        Public Overridable Sub interpretData()
            identifyMolecules()
            classifyMolecules()
            fixChainBreaks()
            identifyProtProtInteractions()
        End Sub

        Private Sub initSpokes()
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.initSpokes()
            Next
        End Sub

        Private Sub joinMolecules(molecule As Molecule, nextMolecule As Molecule)
            molecule.updateCounts(nextMolecule)
            nextMolecule.deleteMolecule(True)
        End Sub

        Public Overridable Sub listAntibodyLoops(paintType As Integer, g As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean)
            Dim coords = New Single(1) {}
            If antibodyLoopLabelField Is Nothing Then
                Return
            End If
            For i = 0 To antibodyLoopLabelField.Count - 1
                Dim loopLabel As TextItem = antibodyLoopLabelField(i)
                loopLabel.paintTextItem(paintType, g, psFile, scale, selected)
            Next
        End Sub

        Public Overridable Sub listResidueIDs()
            Console.WriteLine("Number of residues: " & residueListField.Count.ToString())
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                Console.WriteLine("  Residue id =" & residue.ResidueId & "=")
            Next
        End Sub

        Friend Overridable Sub paintPDBEntry(paintType As Integer, g As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean)
            Me.scale = scale
            Dim nonEquivs = False
            If ensemble.getnPDB() > 1 Then
                Dim onOffString = Params("HIGHLIGHT_NONEQUIVS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("ON") Then
                    nonEquivs = True
                End If
            End If
            initSpokes()
            Dim i As Integer
            For i = 0 To bondListField.Count - 1
                Dim bond As Bond = bondListField(i)
                bond.paintBond(paintType, g, psFile, scale, selected, nonEquivs)
            Next
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                molecule.paintMolecule(paintType, g, psFile, scale, selected, nonEquivs)
            Next
            If pdbTitleField IsNot Nothing Then
                pdbTitleField.paintTextItem(paintType, g, psFile, scale, selected)
            End If
            If ensemble.Antibody Then
                listAntibodyLoops(paintType, g, psFile, scale, selected)
            End If
        End Sub

        Public Overridable Sub recomputeAnnotations(first As Boolean)
            Dim coords = New Single(3) {}
            If nDummyField > 0 Then
                For iDummy = 0 To nDummyField - 1
                    If first Then
                    End If
                Next
            End If
            If ensemble.Antibody Then
                updateLoopLabelCoords(first)
            End If
        End Sub

        Public Overridable Sub shiftSecondDummy(dimShiftX As Single, dimShiftY As Single)
            Dim cutPointX = -3.40282347E+38F
            Dim cutPointY = Single.MaxValue
            If nDummyField > 1 Then
                Dim molecule = dummyMolecule(1)
                If molecule IsNot Nothing Then
                    molecule.applyDimShifts(cutPointX, cutPointY, dimShiftX, dimShiftY)
                End If
            End If
        End Sub

        Public Overridable Sub updateLoopLabelCoords(first As Boolean)
            Dim haveLoop = New Boolean(5) {}
            Dim loopCoords = RectangularArray.Matrix(Of Single)(6, 2)
            Dim orientation = ensemble.OrientationOption
            Dim coordInfo As coordInfo = calcLoopLabelCoords(orientation)
            loopCoords = coordInfo.loopCoords
            haveLoop = coordInfo.haveLoop
            If Not first Then
                loopCoords = copyLoopLabelCoords(loopCoords, haveLoop)
            End If
            For [loop] = 0 To 5
                If haveLoop([loop]) Then
                    Dim labelName As String = "Loop " & Params.antibodyLoopID([loop]).ToString()
                    Dim loopLabel As TextItem = Nothing
                    For i = 0 To antibodyLoopLabelField.Count - 1
                        Dim label As TextItem = antibodyLoopLabelField(i)
                        If label.Text.Equals(labelName) Then
                            loopLabel = label
                        End If
                    Next
                    If loopLabel IsNot Nothing Then
                        loopLabel.Coords = loopCoords([loop])
                    End If
                End If
            Next
        End Sub

        Private Function copyLoopLabelCoords(loopCoords As Single()(), haveLoop As Boolean()) As Single()()
            For [loop] = 0 To 5
                If haveLoop([loop]) Then
                    Dim coords = ensemble.getLoopCoords(Params.antibodyLoopName([loop]))
                    If coords(0) > 0.0F Then
                        loopCoords([loop])(0) = coords(1)
                        loopCoords([loop])(1) = coords(2)
                    End If
                End If
            Next
            Return loopCoords
        End Function

        Public Overridable Function calcInterfaceLine() As Single()
            Dim coords = New Single(4) {}
            Dim cMin = New Single(1) {}
            Dim cMax = New Single(1) {}
            Dim nAtoms1 = 0
            Dim nAtoms2 = 0
            Dim orientation = ensemble.OrientationOption
            Dim i As Integer
            For i = 0 To 1
                cMin(i) = Single.MaxValue
                cMax(i) = -3.40282347E+38F
            Next
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                Dim x = atom.getCoord(0)
                Dim y = atom.getCoord(1)
                Dim molecule = atom.Molecule
                If orientation = RunExe.LANDSCAPE Then
                    If x < cMin(0) Then
                        cMin(0) = x
                    End If
                    If x > cMax(0) Then
                        cMax(0) = x
                    End If
                Else
                    If y < cMin(1) Then
                        cMin(1) = y
                    End If
                    If y > cMax(1) Then
                        cMax(1) = y
                    End If
                End If
                If molecule.InterFace = 1 Then
                    If orientation = RunExe.LANDSCAPE Then
                        If y > cMax(1) Then
                            cMax(1) = y
                        End If
                    ElseIf x < cMin(0) Then
                        cMin(0) = x
                    End If
                    nAtoms1 += 1
                End If
                If molecule.InterFace = 2 Then
                    If orientation = RunExe.LANDSCAPE Then
                        If y < cMin(1) Then
                            cMin(1) = y
                        End If
                    ElseIf x > cMax(0) Then
                        cMax(0) = x
                    End If
                    nAtoms2 += 1
                End If
            Next
            If nAtoms1 = 0 Then
                cMax(1) = 0.0F
                cMax(0) = 0.0F
            End If
            If nAtoms2 = 0 Then
                cMin(1) = 0.0F
                cMin(0) = 0.0F
            End If
            Dim xCoord = New Single(1) {}
            Dim yCoord = New Single(1) {}
            If orientation = RunExe.LANDSCAPE Then
                xCoord(0) = cMin(0)
                xCoord(1) = cMax(0)
                yCoord(1) = (cMax(1) + cMin(1)) / 2.0F
                yCoord(0) = (cMax(1) + cMin(1)) / 2.0F
            Else
                xCoord(1) = (cMax(0) + cMin(0)) / 2.0F
                xCoord(0) = (cMax(0) + cMin(0)) / 2.0F
                yCoord(0) = cMin(1)
                yCoord(1) = cMax(1)
            End If
            coords(0) = 1.0F
            coords(1) = xCoord(0)
            coords(2) = yCoord(0)
            coords(3) = xCoord(1)
            coords(4) = yCoord(1)
            Return coords
        End Function

        Public Overridable ReadOnly Property FromMmcif As Boolean
            Get
                Return fromMmcifField
            End Get
        End Property

        Public Overridable Sub removeEquivalences(pdbId As Integer)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.removeEquivalences(pdbId)
            Next
        End Sub

        Public Overridable Sub resetAllResidues()
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.Wanted = False
                residue.Domain = 0
                residue.Molecule.Wanted = False
            Next
        End Sub

        Public Overridable Sub restoreCoords(storePos As Integer)
            Dim i As Integer
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                molecule.restoreCoords(storePos)
            Next
            If antibodyLoopLabelField IsNot Nothing Then
                For i = 0 To antibodyLoopLabelField.Count - 1
                    Dim label As TextItem = antibodyLoopLabelField(i)
                    label.restoreCoords(storePos)
                Next
            End If
            pdbTitleField.restoreCoords(storePos)
        End Sub

        Public Overridable Sub saveCoords(storePos As Integer)
            Dim i As Integer
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                molecule.saveCoords(storePos)
            Next
            If antibodyLoopLabelField IsNot Nothing Then
                For i = 0 To antibodyLoopLabelField.Count - 1
                    Dim label As TextItem = antibodyLoopLabelField(i)
                    label.saveCoords(storePos)
                Next
            End If
            pdbTitleField.saveCoords(storePos)
        End Sub


        Public Overridable Sub setDomainDefinition(domain As Integer, formattedDefinition As String)
            domainDefinition(domain) = formattedDefinition
        End Sub


        Public Overridable Sub setFromMmcif()
            fromMmcifField = True
        End Sub





        Friend Overridable WriteOnly Property ReadFrom As Integer
            Set(value As Integer)
                readFromField = value
            End Set
        End Property

        Public Overridable Sub setSelectedChain(ichain As Integer, chain As Char)
            selectedChain(ichain) = chain
        End Sub

        Public Overridable Sub setSelectedFullChain(ichain As Integer, fullChain As String)
            selectedFullChain(ichain) = fullChain
        End Sub

        Public Overridable Sub setSelectedMolecule(ichain As Integer, molecule As Molecule)
            selectedMolecule(ichain) = molecule
        End Sub

        Public Overridable Sub setSplitScreenOffset(i As Integer, splitWidth As Single)
            splitScreenOffset(i) = splitWidth
        End Sub

        Public Overridable Sub setSplitShift(i As Integer, shift As Single)
            splitShift(i) = shift
        End Sub


        Public Overridable Function updateMaxMinCoords() As Integer
            nCoords = 0
            Dim halfwayPoint = 0.0F
            Dim nAssigned = New Integer(1) {}
            If nDummyField > 0 Then
                For j = 0 To nDummyField - 1
                    dummyMolecule(j).updateMaxMinCoords()
                    dummyMin(j)(1) = Single.MaxValue
                    dummyMin(j)(0) = Single.MaxValue
                    dummyMax(j)(1) = -3.40282347E+38F
                    dummyMax(j)(0) = -3.40282347E+38F
                    nAssigned(j) = 0
                Next
                If nDummyField > 1 Then
                    halfwayPoint = (dummyMolecule(0).getCoordsMin(1) + dummyMolecule(1).getCoordsMin(1)) / 2.0F
                    If ensemble.OrientationOption = RunExe.PORTRAIT Then
                        halfwayPoint = (dummyMolecule(0).getCoordsMin(0) + dummyMolecule(1).getCoordsMin(0)) / 2.0F
                    End If
                End If
            End If
            Dim first = True
            For i = 0 To moleculeListField.Count - 1
                Dim molecule = moleculeListField(i)
                If molecule.MoleculeType <> 7 Then
                    Dim nc As Integer = molecule.updateMaxMinCoords()
                    nCoords += nc
                    Dim minValue = New Single(1) {}
                    Dim maxValue = New Single(1) {}
                    For icoord = 0 To 1
                        Dim cMin = molecule.getCoordsMin(icoord)
                        Dim cMax = molecule.getCoordsMax(icoord)
                        Dim cAccum = molecule.getCoordsAccum(icoord)
                        minValue(icoord) = cMin
                        maxValue(icoord) = cMax
                        If first Then
                            coordsMin(icoord) = cMin
                            coordsMax(icoord) = cMax
                            coordsAccum(icoord) = cAccum
                        Else
                            If cMin < coordsMin(icoord) Then
                                coordsMin(icoord) = cMin
                            End If
                            If cMax > coordsMax(icoord) Then
                                coordsMax(icoord) = cMax
                            End If
                            coordsAccum(icoord) = coordsAccum(icoord) + cAccum
                        End If
                        If nCoords > 0 Then
                            coordsCentre(icoord) = coordsAccum(icoord) / nCoords
                        Else
                            coordsCentre(icoord) = 0.0F
                        End If
                    Next
                    Dim iSet = 0
                    If nDummyField > 1 Then
                        If ensemble.OrientationOption = RunExe.LANDSCAPE AndAlso (minValue(1) + maxValue(1)) / 2.0F < halfwayPoint OrElse ensemble.OrientationOption = RunExe.PORTRAIT AndAlso (minValue(0) + maxValue(0)) / 2.0F > halfwayPoint Then
                            iSet = 1
                        End If
                    End If
                    nAssigned(iSet) = nAssigned(iSet) + 1
                    If first Then
                        dummyMin(iSet)(0) = minValue(0)
                        dummyMin(iSet)(1) = minValue(1)
                        dummyMax(iSet)(0) = maxValue(0)
                        dummyMax(iSet)(1) = maxValue(1)
                    Else
                        If minValue(0) < dummyMin(iSet)(0) Then
                            dummyMin(iSet)(0) = minValue(0)
                        End If
                        If minValue(1) < dummyMin(iSet)(1) Then
                            dummyMin(iSet)(1) = minValue(1)
                        End If
                        If maxValue(0) > dummyMax(iSet)(0) Then
                            dummyMax(iSet)(0) = maxValue(0)
                        End If
                        If maxValue(1) > dummyMax(iSet)(1) Then
                            dummyMax(iSet)(1) = maxValue(1)
                        End If
                    End If
                    first = False
                End If
            Next
            If ensemble.getnPDB() > 1 Then
                For icoord = 0 To 1
                    coordsMin(icoord) = coordsMin(icoord) - 1.0F
                    coordsMax(icoord) = coordsMax(icoord) + 1.0F
                Next
            End If
            For iDummy = 0 To nDummyField - 1
                If nAssigned(iDummy) > 0 Then
                    Dim dummyCoords = dummyMolecule(iDummy).DummyCoords
                    Dim newCoords = New Single(4) {}
                    newCoords(0) = 0.0F
                    If ensemble.OrientationOption = RunExe.LANDSCAPE Then
                        newCoords(1) = dummyMin(iDummy)(0)
                        newCoords(4) = dummyCoords(2)
                        newCoords(2) = dummyCoords(2)
                        newCoords(3) = dummyMax(iDummy)(0)
                    Else
                        newCoords(3) = dummyCoords(1)
                        newCoords(1) = dummyCoords(1)
                        newCoords(2) = dummyMin(iDummy)(1)
                        newCoords(4) = dummyMax(iDummy)(1)
                    End If
                    dummyMolecule(iDummy).updateDummyMoleculeCoords(newCoords)
                End If
            Next
            If pdbTitleField IsNot Nothing AndAlso Not pdbTitleField.Text.Equals("") Then
                addTextItemCoords(pdbTitleField)
            End If
            If antibodyLoopLabelField IsNot Nothing Then
                For j = 0 To antibodyLoopLabelField.Count - 1
                    Dim label As TextItem = antibodyLoopLabelField(j)
                    If label.Visible AndAlso Not label.Text.Equals("") Then
                        addTextItemCoords(label)
                    End If
                Next
            End If
            Return nCoords
        End Function

        Public Overridable WriteOnly Property Instance As Integer
            Set(value As Integer)
                instanceField = value
            End Set
        End Property

        Public Overridable Property OriginalPDB As PDBEntry
            Set(value As PDBEntry)
                originalPDBField = value
            End Set
            Get
                Return originalPDBField
            End Get
        End Property


        Private Sub addTextItemCoords(pdbTitle As TextItem)
            Dim textCoords = pdbTitle.MaxMinCoords
            If textCoords(0) < coordsMin(0) Then
                coordsMin(0) = textCoords(0)
            End If
            If textCoords(1) > coordsMax(0) Then
                coordsMax(0) = textCoords(1)
            End If
            If textCoords(2) < coordsMin(1) Then
                coordsMin(1) = textCoords(2)
            End If
            If textCoords(3) > coordsMax(1) Then
                coordsMax(1) = textCoords(3)
            End If
        End Sub
    End Class

End Namespace
