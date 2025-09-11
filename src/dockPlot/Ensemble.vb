Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Imaging

Namespace ligplus

    Public Class Ensemble
        Public Const LIST_MARGIN As Integer = 10

        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Private Const CUTPOINT_MARGIN As Single = 2.0F

        Private Const DIMSHIFT_FRACTION As Single = 1.1F

        Private Const MIDPOINT_FRACTION As Single = 0.15F

        Private Const X As Integer = 0

        Private Const Y As Integer = 1

        Private Const MIN As Integer = 0

        Private Const MAX As Integer = 1

        Private splitShiftOnField As Boolean = False

        Private maxWidth As Single = 0.0F

        Private coordsMinField As Single() = New Single(2) {}

        Private coordsMaxField As Single() = New Single(2) {}

        Private nCoords As Integer = 0

        Private nPDBEntries As Integer = 0

        Private selectedEntryField As Integer = -1

        Private antibodyField As Boolean = False

        Private orientation As Integer

        Private programField As Integer = LIGPLOT

        Private nStored As Integer = 0

        Private storePos As Integer = -1

        Private cutPointX As Single = 0.0F

        Private cutPointY As Single = 0.0F

        Private dimShiftX As Single = 0.0F

        Private dimShiftY As Single = 0.0F

        Private clickPDBField As PDBEntry = Nothing

        Private ligplusParams As Properties

        Private pdbEntryListField As List(Of PDBEntry)

        Private plotListLabel As TextItem = Nothing

        Private diagramTypeField As Integer

        Public Sub New(ligplusParams As Properties, diagramType As Integer)
            Me.ligplusParams = ligplusParams
            diagramTypeField = diagramType
            init()
        End Sub

        Public Overridable Function getSplitWidth(splitScreen As Boolean, margin As Single) As Single()
            Dim splitWidth = New Single(1) {}
            splitWidth(1) = 0.0F
            splitWidth(0) = 0.0F
            Dim nPDB = pdbEntryListField.Count
            If nPDB = 0 Then
                Return splitWidth
            End If
            If nPDB < 2 Then
                splitScreen = False
            End If
            splitWidth = getWidthNoSplit(margin)
            If splitScreen Then
                splitWidth = calcLayout(nPDB, splitWidth, margin)
            End If
            Return splitWidth
        End Function

        Public Overridable Function getWidthNoSplit(margin As Single) As Single()
            Dim splitWidth = New Single(1) {}
            Dim noShift = 0.0F
            splitWidth(1) = 0.0F
            splitWidth(0) = 0.0F
            Dim offset = New Single(1) {}
            Dim minCoords = New Single(1) {}
            Dim maxCoords = New Single(1) {}
            For i = 0 To 1
                maxCoords(i) = 0.0F
                minCoords(i) = 0.0F
            Next
            For iPDB = 0 To pdbEntryListField.Count - 1
                offset(1) = 0.0F
                offset(0) = 0.0F
                Dim pdb = pdbEntryListField(iPDB)
                pdb.setSplitScreenOffset(0, noShift)
                pdb.setSplitScreenOffset(1, noShift)
                For iCoord = 0 To 1
                    Dim cMin = pdb.getCoordsMin(iCoord)
                    Dim cMax = pdb.getCoordsMax(iCoord)
                    If iPDB = 0 Then
                        minCoords(iCoord) = cMin
                        maxCoords(iCoord) = cMax
                    Else
                        If cMin < minCoords(iCoord) Then
                            minCoords(iCoord) = cMin
                        End If
                        If cMax > maxCoords(iCoord) Then
                            maxCoords(iCoord) = cMax
                        End If
                    End If
                Next
            Next
            splitWidth(0) = maxCoords(0) - minCoords(0)
            splitWidth(1) = maxCoords(1) - minCoords(1)
            Return splitWidth
        End Function

        Public Overridable Function calcBestSplit(haveLoop As Boolean(), width As Single, height As Single, midPoint As Single, loopMin As Single(), loopMax As Single()) As Single
            Dim bestMidpoint = 0.0F
            Dim minDistance = -1.0F
            For [loop] = 0 To 5
                If haveLoop([loop]) Then
                    Dim distance = Math.Abs(midPoint - loopMin([loop]))
                    If minDistance = -1.0F OrElse distance < minDistance Then
                        minDistance = distance
                        bestMidpoint = loopMin([loop])
                    End If
                    distance = Math.Abs(midPoint - loopMax([loop]))
                    If minDistance = -1.0F OrElse distance < minDistance Then
                        minDistance = distance
                        bestMidpoint = loopMax([loop])
                    End If
                End If
            Next
            If minDistance = -1.0F Then
                bestMidpoint = midPoint
            Else
                Dim distance = Math.Abs(midPoint - bestMidpoint)
                Dim fraction = distance / width
                If fraction > 0.15F Then
                    bestMidpoint = midPoint
                End If
            End If
            Return bestMidpoint
        End Function

        Public Overridable Sub calcDimSplit()
            Dim haveLoop = New Boolean(5) {}
            Dim loopMin = New Single(5) {}
            Dim loopMax = New Single(5) {}
            Dim overallMinMax = {New Single(1) {}, New Single(1) {}}
            Console.WriteLine("In calcDimSplit ...")
            If pdbEntryListField.Count > 1 Then
                fixLabelCoords()
            End If
            overallMinMax(0)(0) = Single.MaxValue
            overallMinMax(0)(1) = Single.MaxValue
            overallMinMax(1)(0) = -3.40282347E+38F
            overallMinMax(1)(1) = -3.40282347E+38F
            For [loop] = 0 To 5
                loopMin([loop]) = Single.MaxValue
                loopMax([loop]) = -3.40282347E+38F
                haveLoop([loop]) = False
            Next
            For iPDB = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(iPDB)
                Dim coordInfo = pdb.calcLoopCoordRanges(orientation)
                Dim haveLoopIn = coordInfo.haveLoop
                Dim loopMinIn As Single() = coordInfo.loopMin
                Dim loopMaxIn As Single() = coordInfo.loopMax
                Dim overallMinMaxIn As Single()() = coordInfo.overallMinMax
                For iCoord = 0 To 1
                    If overallMinMaxIn(0)(iCoord) < overallMinMax(0)(iCoord) Then
                        overallMinMax(0)(iCoord) = overallMinMaxIn(0)(iCoord)
                    End If
                    If overallMinMaxIn(1)(iCoord) > overallMinMax(1)(iCoord) Then
                        overallMinMax(1)(iCoord) = overallMinMaxIn(1)(iCoord)
                    End If
                Next
                Dim antibodyLoopLabel As List(Of TextItem) = pdb.AntibodyLoopLabel
                If antibodyLoopLabel IsNot Nothing Then
                    For iLabel = 0 To antibodyLoopLabel.Count - 1
                        Dim label = antibodyLoopLabel(iLabel)
                        Dim labelHeight = label.TextHeight
                        Dim labelWidth = label.TextWidth
                        If orientation = RunExe.LANDSCAPE AndAlso label.Coords(1) - labelHeight < overallMinMax(0)(1) Then
                            overallMinMax(0)(1) = label.Coords(1) - labelHeight
                        ElseIf orientation = RunExe.PORTRAIT AndAlso label.Coords(0) + labelWidth > overallMinMax(1)(0) Then
                            overallMinMax(1)(0) = label.Coords(0) + labelWidth
                        End If
                    Next
                End If
                For i = 0 To 5
                    If haveLoopIn(i) Then
                        haveLoop(i) = haveLoopIn(i)
                        If loopMinIn(i) < loopMin(i) Then
                            loopMin(i) = loopMinIn(i)
                        End If
                        If loopMaxIn(i) > loopMax(i) Then
                            loopMax(i) = loopMaxIn(i)
                        End If
                    End If
                Next
            Next
            Dim width = overallMinMax(1)(0) - overallMinMax(0)(0)
            Dim height = overallMinMax(1)(1) - overallMinMax(0)(1)
            Dim midPoint = (overallMinMax(0)(0) + overallMinMax(1)(0)) / 2.0F
            If orientation = RunExe.PORTRAIT Then
                width = overallMinMax(1)(1) - overallMinMax(0)(1)
                height = overallMinMax(1)(0) - overallMinMax(0)(0)
                midPoint = (overallMinMax(0)(1) + overallMinMax(1)(1)) / 2.0F
            End If
            Console.WriteLine("Overall max and min = " & overallMinMax(0)(0).ToString() & ", " & overallMinMax(0)(1).ToString() & " -> " & overallMinMax(1)(0).ToString() & ", " & overallMinMax(1)(1).ToString())
            Console.WriteLine("width x height = " & width.ToString() & " x " & height.ToString() & "    Midpoint " & midPoint.ToString())
            Dim bestMidpoint = calcBestSplit(haveLoop, width, height, midPoint, loopMin, loopMax)
            dimShiftX = -width / 2.0F
            dimShiftY = -height * 1.1F
            If orientation = RunExe.PORTRAIT Then
                dimShiftY = width / 2.0F
                dimShiftX = height * 1.1F
            End If
            Console.WriteLine("Best midpoint = " & bestMidpoint.ToString())
            Console.WriteLine("   dimShifts = " & dimShiftX.ToString() & ", " & dimShiftY.ToString())
            cutPointX = bestMidpoint - 2.0F
            cutPointY = -2.0F * Math.Abs(overallMinMax(1)(1))
            If orientation = RunExe.PORTRAIT Then
                cutPointX = Math.Abs(2.0F * overallMinMax(1)(0))
                cutPointY = bestMidpoint - 2.0F
            End If
            applyDimShifts(cutPointX, cutPointY, dimShiftX, dimShiftY)
        End Sub

        Private Function calcLayout(nPDB As Integer, maxWidth As Single(), margin As Single) As Single()
            Dim splitWidth = New Single(1) {}
            splitWidth(1) = 0.0F
            splitWidth(0) = 0.0F
            Dim nHoriz As Single = Math.Sqrt(nPDB)
            Dim nAcross As Integer = nHoriz
            If Math.Abs(nHoriz - nAcross) > 0.0001R Then
                nAcross += 1
            End If
            Dim nVert As Single = nPDB / nAcross
            Dim nDown As Integer = nVert
            If Math.Abs(nVert - nDown) > 0.0001R Then
                nDown += 1
            End If
            If programField = DIMPLOT Then
                If orientation = RunExe.LANDSCAPE Then
                    nAcross = 1
                    nDown = nPDB
                Else
                    nAcross = nPDB
                    nDown = 1
                End If
            End If
            Dim iPDB = 0
            Dim yShift = (nDown - 1) * (maxWidth(1) + margin)
            For iDown = 0 To nDown - 1
                Dim xShift = 0.0F
                Dim iAcross = 0

                While iAcross < nAcross AndAlso iPDB < nPDB
                    Dim pdb = pdbEntryListField(iPDB)
                    pdb.setSplitScreenOffset(0, xShift)
                    pdb.setSplitScreenOffset(1, yShift)
                    xShift = xShift + maxWidth(0) + margin
                    If iDown = 0 Then
                        splitWidth(0) = splitWidth(0) + maxWidth(0)
                        If iAcross <> nAcross - 1 Then
                            splitWidth(0) = splitWidth(0) + margin
                        End If
                    End If
                    iPDB += 1
                    iAcross += 1
                End While
                yShift -= maxWidth(1) + margin
                splitWidth(1) = splitWidth(1) + maxWidth(1)
                If iDown <> nDown - 1 Then
                    splitWidth(1) = splitWidth(1) + margin
                End If
            Next
            Return splitWidth
        End Function

        Public Overridable Sub createDummyMolecules(nDummy As Integer)
            For iPDB = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(iPDB)
                pdb.createDummyMolecules(nDummy, iPDB)
            Next
        End Sub

        Public Overridable Sub finalProcessing()
            updateMaxMinCoords()
            If programField = DIMPLOT OrElse antibodyField Then
                Dim nDummy = 1
                Dim split = False
                Dim abodySplit = ligplusParams("ABODY_SPLIT")
                Console.WriteLine("abodySplit = " & abodySplit)
                If antibodyField AndAlso Not ReferenceEquals(abodySplit, Nothing) AndAlso abodySplit.Equals("TRUE") Then
                    split = True
                End If
                Dim dimplotSplit = ligplusParams("DIMPLOT_SPLIT")
                If Not antibodyField AndAlso Not ReferenceEquals(dimplotSplit, Nothing) AndAlso dimplotSplit.Equals("TRUE") Then
                    split = True
                End If
                Console.WriteLine("split = " & split.ToString())
                If split Then
                    Dim width = InterfaceWidth
                    Console.WriteLine("Interface width = " & width.ToString())
                    Dim dimplotSplitWidth = ligplusParams("DIMPLOT_SPLIT_WIDTH")
                    Dim minWidth = Single.Parse(dimplotSplitWidth)
                    Console.WriteLine("dimplotSplitWidth = " & dimplotSplitWidth)
                    Console.WriteLine("minWidth = " & minWidth.ToString())
                    If width < minWidth Then
                        split = False
                    End If
                End If
                If split Then
                    nDummy += 1
                End If
                createDummyMolecules(nDummy)
                fixDummyCoords(nDummy)
                If split Then
                    calcDimSplit()
                    shiftSecondDummy(dimShiftX, dimShiftY)
                End If
            End If
            resetUndoCoord()
            saveCoords()
        End Sub

        Public Overridable Sub fixDummyCoords(nDummy As Integer)
            Dim dummyCoords = {New Single(4) {}, New Single(4) {}}
            For iPDB = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(iPDB)
                For iDummy = 0 To nDummy - 1
                    Dim molecule = pdb.getDummyMolecule(iDummy)
                    If iPDB = 0 Then
                        dummyCoords(iDummy) = molecule.DummyCoords
                    Else
                        molecule.DummyCoords = dummyCoords(iDummy)
                    End If
                Next
            Next
        End Sub

        Public Overridable Sub fixLabelCoords()
            Dim nEntry = New Integer(5) {}
            Dim loopCoords = RectangularArray.Matrix(Of Single)(6, 2)
            For i = 0 To 5
                nEntry(i) = 0
                loopCoords(i)(1) = 0.0F
                loopCoords(i)(0) = 0.0F
            Next
            For iPDB = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(iPDB)
                Dim antibodyLoopLabel As List(Of TextItem) = pdb.AntibodyLoopLabel
                If antibodyLoopLabel IsNot Nothing Then
                    For iLabel = 0 To antibodyLoopLabel.Count - 1
                        Dim label = antibodyLoopLabel(iLabel)
                        Dim labelNo = -1
                        Dim j = 0

                        While j < 6 AndAlso labelNo = -1
                            If label.Text.Equals(Params.antibodyLoopName(j)) Then
                                labelNo = j
                                nEntry(j) = nEntry(j) + 1
                                Dim coords = label.Coords
                                loopCoords(j)(0) = loopCoords(j)(0) + coords(0)
                                loopCoords(j)(1) = loopCoords(j)(1) + coords(1)
                            End If

                            j += 1
                        End While
                    Next
                End If
            Next
            For [loop] = 0 To 5
                If nEntry([loop]) > 0 Then
                    loopCoords([loop])(0) = loopCoords([loop])(0) / nEntry([loop])
                    loopCoords([loop])(1) = loopCoords([loop])(1) / nEntry([loop])
                    For j = 0 To pdbEntryListField.Count - 1
                        Dim pdb = pdbEntryListField(j)
                        pdb.fixLoopCoords([loop], loopCoords([loop]))
                    Next
                End If
            Next
        End Sub

        Public Overridable Sub deleteSelectedPDBEntry()
            If selectedEntryField <> -1 Then
                Dim pdb = pdbEntryListField(selectedEntryField)
                Dim pdbId = pdb.PDBId
                For i = 0 To pdbEntryListField.Count - 1
                    Dim otherPDB = pdbEntryListField(i)
                    If otherPDB IsNot pdb Then
                        otherPDB.removeEquivalences(pdbId)
                    End If
                Next
                pdbEntryListField.RemoveAt(selectedEntryField)
                nPDBEntries -= 1
                selectedEntryField = nPDBEntries - 1
            End If
        End Sub

        Private Sub init()
            nPDBEntries = 0
            selectedEntryField = -1
            pdbEntryListField = New List(Of PDBEntry)()
            For i = 0 To 2
                coordsMinField(i) = 0.0F
                coordsMaxField(i) = 0.0F
            Next
            nCoords = 0
        End Sub

        Public Overridable Sub addPDBEntry(pdb As PDBEntry)
            For i = 0 To pdbEntryListField.Count - 1
                Dim existingPDB = pdbEntryListField(i)
                existingPDB.setSplitShift(0, 0.0F)
                existingPDB.setSplitShift(1, 0.0F)
            Next
            pdbEntryListField.Add(pdb.Object)
            If pdbEntryListField.Count = 1 Then
                Dim textType = 13
                Dim x = 0.0F
                Dim y = 0.0F
                plotListLabel = New TextItem(pdb, "Plots", textType, x, y)
                plotListLabel.MaxMinCoords()
                maxWidth = plotListLabel.TextWidth
            End If
            Dim plotLabel = pdb.PlotLabel
            plotLabel.MaxMinCoords()
            Dim width = plotLabel.TextWidth
            If width > maxWidth Then
                maxWidth = width
            End If
            selectedEntryField = nPDBEntries
            nPDBEntries += 1
        End Sub

        Public Overridable Sub applyDimShifts(cutPointX As Single, cutPointY As Single, dimShiftX As Single, dimShiftY As Single)
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                pdb.applyDimShifts(cutPointX, cutPointY, dimShiftX, dimShiftY)
                updateMaxMinCoords()
            Next
        End Sub

        Friend Overridable Function checkPDBCode(pdbCode As String, current As PDBEntry) As Integer
            Dim instance = 0
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                Dim code = pdb.PDBCode
                If code.Equals(pdbCode) AndAlso pdb IsNot current Then
                    Dim inst = pdb.CodeInstance
                    If inst >= instance Then
                        instance = inst + 1
                    End If
                End If
            Next
            Return instance
        End Function

        Public Overridable Function clickCheck(screenX As Integer, screenY As Integer, scale As Scale, startScreen As Boolean) As Object
            Dim done = False
            Dim clickObject As Object = Nothing
            clickPDBField = Nothing
            If pdbEntryListField.Count = 0 Then
                Return clickObject
            End If
            If pdbEntryListField.Count > 1 Then
                Dim k = 0

                While k < pdbEntryListField.Count AndAlso Not done
                    Dim pDBEntry = pdbEntryListField(k)
                    Dim plotLabel = pDBEntry.PlotLabel
                    clickObject = plotLabel.clickCheck(screenX, screenY)
                    If clickObject IsNot Nothing Then
                        selectedEntryField = k
                        done = True
                        clickPDBField = pDBEntry
                    End If

                    k += 1
                End While
            End If
            If done Then
                Return clickObject
            End If
            Dim pdb = pdbEntryListField(selectedEntryField)
            Dim splitShift = New Single(1) {}
            For j = 0 To 1
                If splitShiftOnField Then
                    splitShift(j) = pdb.getSplitShift(j)
                Else
                    splitShift(j) = 0.0F
                End If
            Next
            Dim x = scale.convertToReal(screenX, 0, pdb.getSplitScreenOffset(0), splitShift(0))
            Dim y = scale.convertToReal(screenY, 1, pdb.getSplitScreenOffset(1), splitShift(1))
            clickObject = pdb.clickCheck(x, y)
            clickPDBField = pdb
            Dim inactivePlotsOn = True
            Dim params As Properties = DefaultParams
            Dim onOffString = params("INACTIVE_PLOTS_STATUS")
            If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                inactivePlotsOn = False
            End If
            Dim i = 0
            While i < pdbEntryListField.Count AndAlso inactivePlotsOn AndAlso clickObject Is Nothing
                If i <> selectedEntryField Then
                    pdb = pdbEntryListField(i)
                    For k = 0 To 1
                        If splitShiftOnField Then
                            splitShift(k) = pdb.getSplitShift(k)
                        Else
                            splitShift(k) = 0.0F
                        End If
                    Next
                    x = scale.convertToReal(screenX, 0, pdb.getSplitScreenOffset(0), splitShift(0))
                    y = scale.convertToReal(screenY, 1, pdb.getSplitScreenOffset(1), splitShift(1))
                    clickObject = pdb.clickCheck(x, y)
                    If clickObject IsNot Nothing Then
                        selectedEntryField = i
                        clickPDBField = pdb
                    End If
                End If

                i += 1
            End While
            Return clickObject
        End Function

        Public Overridable ReadOnly Property CoordsMin As Single()
            Get
                Return coordsMinField
            End Get
        End Property

        Public Overridable ReadOnly Property CoordsMax As Single()
            Get
                Return coordsMaxField
            End Get
        End Property

        Public Overridable ReadOnly Property MaxMinCoords As Single()
            Get
                Dim lMaxMinCoords = New Single(3) {}
                Dim i As Integer
                For i = 0 To 3
                    lMaxMinCoords(i) = 0.0F
                Next
                For i = 0 To pdbEntryListField.Count - 1
                    Dim pdb = pdbEntryListField(i)
                    For icoord = 0 To 1
                        Dim offset = pdb.getSplitScreenOffset(icoord)
                        If SplitShiftOn Then
                            offset += pdb.getSplitShift(icoord)
                        End If
                        Dim cMin = pdb.getCoordsMin(icoord) + offset
                        Dim cMax = pdb.getCoordsMax(icoord) + offset
                        If i = 0 Then
                            lMaxMinCoords(icoord * 2) = cMin
                            lMaxMinCoords(icoord * 2 + 1) = cMax
                        Else
                            If cMin < lMaxMinCoords(icoord * 2) Then
                                lMaxMinCoords(icoord * 2) = cMin
                            End If
                            If cMax > lMaxMinCoords(icoord * 2 + 1) Then
                                lMaxMinCoords(icoord * 2 + 1) = cMax
                            End If
                        End If
                    Next
                Next
                Return lMaxMinCoords
            End Get
        End Property

        Public Overridable ReadOnly Property ClickPDB As PDBEntry
            Get
                Return clickPDBField
            End Get
        End Property

        Public Overridable ReadOnly Property DefaultParams As Properties
            Get
                If pdbEntryListField.Count > 0 AndAlso selectedEntryField < pdbEntryListField.Count Then
                    Dim pdb = pdbEntryListField(selectedEntryField)
                    Return pdb.Params
                End If
                Return ligplusParams
            End Get
        End Property

        Public Overridable Property DiagramType As Integer
            Get
                Return diagramTypeField
            End Get
            Set(value As Integer)
                diagramTypeField = value
            End Set
        End Property

        Public Overridable ReadOnly Property DummyCoords As Single()
            Get
                Dim found = False
                Dim lDummyCoords = New Single(4) {}
                lDummyCoords(0) = 0.0F
                Dim iPDB = 0

                While iPDB < pdbEntryListField.Count AndAlso Not found
                    Dim pdb = pdbEntryListField(iPDB)
                    Dim nDummy = pdb.NDummy
                    For iDummy = 0 To nDummy - 1
                        Dim dummyMolecule = pdb.getDummyMolecule(iDummy)
                        If dummyMolecule IsNot Nothing Then
                            lDummyCoords = dummyMolecule.DummyCoords
                            If lDummyCoords(0) > 0.0F Then
                                found = True
                            End If
                        End If
                    Next

                    iPDB += 1
                End While
                Return lDummyCoords
            End Get
        End Property

        Public Overridable Function getEntryNo(pdb As PDBEntry) As Integer
            Dim entryNo = -1
            Dim i = 0

            While i < pdbEntryListField.Count AndAlso entryNo = -1
                Dim pdbInList = pdbEntryListField(i)
                If pdbInList Is pdb Then
                    entryNo = i
                End If

                i += 1
            End While
            Return entryNo
        End Function

        Public Overridable ReadOnly Property InterfaceWidth As Single
            Get
                Dim width = 0.0F
                If orientation = RunExe.LANDSCAPE Then
                    width = coordsMaxField(0) - coordsMinField(0)
                Else
                    width = coordsMaxField(1) - coordsMinField(1)
                End If
                Return width
            End Get
        End Property

        Public Overridable Function getLoopCoords(antibodyLoopID As String) As Single()
            Dim found = False
            Dim loopCoords = New Single(2) {}
            loopCoords(0) = 0.0F
            Dim iPDB = 0

            While iPDB < pdbEntryListField.Count AndAlso Not found
                Dim pdb = pdbEntryListField(iPDB)
                Dim antibodyLoopLabel As List(Of TextItem) = pdb.AntibodyLoopLabel
                If antibodyLoopLabel IsNot Nothing Then
                    Dim i = 0

                    While i < antibodyLoopLabel.Count AndAlso Not found
                        Dim label = antibodyLoopLabel(i)
                        If label.Text.Equals(antibodyLoopID) Then
                            loopCoords(0) = 1.0F
                            Dim coords = label.Coords
                            loopCoords(1) = coords(0)
                            loopCoords(2) = coords(1)
                            found = True
                        End If

                        i += 1
                    End While
                End If

                iPDB += 1
            End While
            Return loopCoords
        End Function

        Public Overridable Function getnPDB() As Integer
            Return nPDBEntries
        End Function

        Public Overridable Function getnStored() As Integer
            Return nStored
        End Function

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property

        Public Overridable Property OrientationOption As Integer
            Get
                Return orientation
            End Get
            Set(value As Integer)
                orientation = value
            End Set
        End Property

        Public Overridable Property Program As Integer
            Get
                Return programField
            End Get
            Set(value As Integer)
                programField = value
            End Set
        End Property

        Public Overridable ReadOnly Property PDBEntryList As List(Of PDBEntry)
            Get
                Return pdbEntryListField
            End Get
        End Property

        Public Overridable Property SelectedEntry As Integer
            Get
                Return selectedEntryField
            End Get
            Set(value As Integer)
                selectedEntryField = value
            End Set
        End Property

        Public Overridable Function identifyPDBEntry(PDBId As Integer) As PDBEntry
            Dim matchPDB As PDBEntry = Nothing
            Dim iPDB = 0
            While iPDB < pdbEntryListField.Count AndAlso matchPDB Is Nothing
                Dim pdb = pdbEntryListField(iPDB)
                If PDBId = pdb.PDBId Then
                    matchPDB = pdb
                End If

                iPDB += 1
            End While
            Return matchPDB
        End Function

        Public Overridable Property Antibody As Boolean
            Get
                Return antibodyField
            End Get
            Set(value As Boolean)
                antibodyField = value
            End Set
        End Property

        Public Overridable Sub listPlots(g As IGraphics, scale As Scale)
            Dim selected = True
            Dim paintType = 0
            Dim coords = New Single(1) {}
            Dim psFile As PostScript = Nothing
            If pdbEntryListField.Count > 1 Then
                Dim plotWidth = scale.PlotWidth
                Dim plotHeight = scale.PlotHeight
                Dim size = plotListLabel.TextHeight
                coords(0) = plotWidth - maxWidth - 10.0F
                coords(1) = size + 10.0F
                plotListLabel.Coords = coords
                coords(1) = coords(1) + size / 2.0F
                plotListLabel.paintTextItem(paintType, g, psFile, scale, selected)
                For i = 0 To pdbEntryListField.Count - 1
                    If i = selectedEntryField Then
                        selected = True
                    Else
                        selected = False
                    End If
                    Dim pdb = pdbEntryListField(i)
                    Dim plotLabel = pdb.PlotLabel
                    size = plotLabel.TextHeight
                    coords(1) = coords(1) + size
                    plotLabel.Coords = coords
                    coords(1) = coords(1) + size / 2.0F
                    plotLabel.paintTextItem(paintType, g, psFile, scale, selected)
                Next
            End If
        End Sub

        Public Overridable Sub paintEnsemble(paintType As Integer, g As IGraphics, psFile As PostScript, scale As Scale, splitScreen As Boolean)
            Dim selected = False
            Dim pdb As PDBEntry = Nothing
            If splitScreen Then
                selected = True
            End If
            For i = 0 To pdbEntryListField.Count - 1
                If i <> selectedEntryField Then
                    pdb = pdbEntryListField(i)
                    If pdb IsNot Nothing Then
                        pdb.paintPDBEntry(paintType, g, psFile, scale, selected)
                    End If
                End If
            Next
            If selectedEntryField > -1 Then
                pdb = pdbEntryListField(selectedEntryField)
                selected = True
            Else
                pdb = Nothing
            End If
            If pdb IsNot Nothing Then
                pdb.paintPDBEntry(paintType, g, psFile, scale, selected)
            End If
        End Sub

        Public Overridable Sub recomputeAnnotations()
            Dim first = True
            updateMaxMinCoords()
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                If pdb IsNot Nothing Then
                    pdb.recomputeAnnotations(first)
                    first = False
                End If
            Next
        End Sub

        Public Overridable Sub resetUndoCoord()
            nStored = 0
            storePos = -1
        End Sub

        Public Overridable Sub restoreCoords()
            If nStored < 2 Then
                Return
            End If
            nStored -= 1
            storePos -= 1
            If storePos < 0 Then
                storePos = 9
            End If
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                If pdb IsNot Nothing Then
                    pdb.restoreCoords(storePos)
                End If
            Next
            updateMaxMinCoords()
        End Sub

        Public Overridable Sub saveCoords()
            If nStored = 0 Then
                storePos = 0
            Else
                storePos += 1
                If storePos > 9 Then
                    storePos = 0
                End If
            End If
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                If pdb IsNot Nothing Then
                    pdb.saveCoords(storePos)
                End If
            Next
            nStored += 1
            If nStored > 10 Then
                nStored = 10
            End If
        End Sub





        Public Overridable Sub setParameterInAll(paramName As String, value As String)
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                Dim params = pdb.Params
                If params IsNot Nothing Then
                    params(paramName) = value
                End If
            Next
            If ligplusParams IsNot Nothing Then
                ligplusParams(paramName) = value
            End If
        End Sub


        Friend Overridable Property SplitShiftOn As Boolean
            Set(value As Boolean)
                splitShiftOnField = value
            End Set
            Get
                Return splitShiftOnField
            End Get
        End Property

        Public Overridable Sub shiftSecondDummy(dimShiftX As Single, dimShiftY As Single)
            Console.WriteLine("In shiftSecondDummy: " & dimShiftX.ToString() & ", " & dimShiftY.ToString())
            Console.WriteLine("   Number of PDB entries: " & pdbEntryListField.Count.ToString())
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                Console.WriteLine("   " & i.ToString() & ". PDB " & pdb.PDBCode)
                pdb.shiftSecondDummy(dimShiftX, dimShiftY)
            Next
        End Sub

        Public Overridable Function updateMaxMinCoords() As Integer
            nCoords = 0
            For i = 0 To pdbEntryListField.Count - 1
                Dim pdb = pdbEntryListField(i)
                Dim nc As Integer = pdb.updateMaxMinCoords()
                nCoords += nc
                For icoord = 0 To 1
                    Dim cMin = pdb.getCoordsMin(icoord)
                    Dim cMax = pdb.getCoordsMax(icoord)
                    If i = 0 Then
                        coordsMinField(icoord) = cMin
                        coordsMaxField(icoord) = cMax
                    Else
                        If cMin < coordsMinField(icoord) Then
                            coordsMinField(icoord) = cMin
                        End If
                        If cMax > coordsMaxField(icoord) Then
                            coordsMaxField(icoord) = cMax
                        End If
                    End If
                Next
            Next
            Return nCoords
        End Function

    End Class

End Namespace
