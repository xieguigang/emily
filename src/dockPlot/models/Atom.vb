Imports System.Drawing
Imports ligplus.ligplus
Imports ligplus.pdb
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Imaging

Namespace models

    Public Class Atom
        Public Shared METAL_NAME As String() = New String() {"HE  ", "LI  ", "BE  ", " B  ", " F  ", "NE  ", "NA  ", "MG  ", "AL  ", "SI  ", "CL  ", "AR  ", " K  ", "CA  ", "SC  ", "TI  ", " V  ", "CR  ", "MN  ", "FE  ", "CO  ", "NI  ", "CU  ", "ZN  ", "GA  ", "GE  ", "AS  ", "SE  ", "BR  ", "KR  ", "RB  ", "SR  ", " Y  ", "ZR  ", "NB  ", "MO  ", "TC  ", "RU  ", "RH  ", "PD  ", "AG  ", "CD  ", "IN  ", "SN  ", "SB  ", "TE  ", " I  ", "XE  ", "CS  ", "BA  ", "LA  ", "CE  ", "PR  ", "ND  ", "PM  ", "SM  ", "EU  ", "GD  ", "TB  ", "DY  ", "HO  ", "ER  ", "TM  ", "YB  ", "LU  ", "HF  ", "TA  ", " W  ", "RE  ", "OS  ", "IR  ", "PT  ", "AU  ", "HG  ", "TL  ", "PB  ", "BI  ", "po  ", "AT  ", "RN  ", "FR  ", "RA  ", "ac  ", "TH  ", "PA  ", " U  ", "np  ", "PU  ", "AM  ", "CM  ", "BK  ", "CF  ", "ES  ", "FM  ", "MD  ", "no  ", "LR  ", "....", "....", "...."}

        Public Shared ELEMENTField As String() = New String() {" N", " C", " O", " S", " P"}

        Private metal As Boolean = False

        Private Shared ATOM_EDGE_WIDTH As Single = 0.01F

        Private Const MAX_SPOKES As Integer = 30

        Public Const SPOKE_ANGLE_ATOM As Integer = 18

        Public Const SPOKE_ANGLE_RESIDUE As Integer = 12

        Public Const SPOKE_EXTENT As Single = 1.6F

        Public Const SPOKE_MIN As Single = 1.2F

        Public Const ARC_ANGLE As Double = 120.0R

        Public Const EXPAND_UNDERLAY As Single = 1.4F

        Private inUseField As Boolean = False

        Private visible As Boolean = True

        Private wantedField As Boolean = True

        Private spoke As Boolean() = New Boolean(29) {}

        Private atHetField As Char

        Private colour As Color = Color.Red

        Private edgeColour As Color = Color.Black

        Private bValueField As Single

        Private occupancyField As Single

        Private radiusField As Single = 0.0F

        Private plotDiameter As Single = 10.0F

        Private coords As Single() = New Single(2) {}

        Private originalCoords As Single() = New Single(2) {}

        Private plotCoords As Single() = New Single(1) {}

        Private psCoords As Single() = New Single(1) {}

        Private saveCoords_Conflict As Single()() = RectangularArray.Matrix(Of Single)(10, 2)

        Private atomNumberField As Integer

        Private moleculeType As Integer

        Private nspokes As Integer

        Private nspokesSet As Integer

        Private program As Integer = RunExe.LIGPLOT

        Private spokeAngle As Integer

        Private pdb As PDBEntry

        Private moleculeField As Molecule

        Private params As Properties

        Private residueField As Residue = Nothing

        Private atomNameField As String

        Private fullAtomNameField As String

        Private atomSizeType As String = "NO"

        Private atomType As String = "NO"

        Private atomColourType As String = "OTHER"

        Private elementField1 As String

        Private atomLabelField As TextItem = Nothing

        Public Shared Function identifyElement(atomName As String) As String
            Dim elementName = "  "
            elementName = isMetal(atomName)
            If elementName.Equals("  ") Then
                Dim hydrogen = isHydrogen(atomName)
                If hydrogen Then
                    elementName = " H"
                Else
                    elementName = " " & atomName.Substring(1, 1)
                    Dim found = False
                    Dim ielem = 0

                    While ielem < ELEMENTField.Length AndAlso Not found
                        If atomName.Substring(0, 2).Equals(ELEMENTField(ielem)) OrElse atomName(1) = ELEMENTField(ielem)(1) AndAlso (atomName(0) = "A"c OrElse atomName(0) = "N"c OrElse atomName(0) = "P"c) Then
                            elementName = ELEMENTField(ielem)
                            found = True
                        End If

                        ielem += 1
                    End While
                End If
            End If
            Return elementName
        End Function

        Private Shared Function isHydrogen(atomName As String) As Boolean
            Dim hydrogen = False
            If atomName(0) = "H"c OrElse atomName(1) = "H"c OrElse atomName(1) = "Q"c OrElse atomName(1) = "D"c AndAlso (atomName(0) = " "c OrElse atomName(0) = "1"c OrElse atomName(0) = "2"c OrElse atomName(0) = "3"c) Then
                hydrogen = True
            End If
            If atomName(0) >= "0"c AndAlso atomName(0) <= "9"c AndAlso atomName(2) = "H"c Then
                hydrogen = True
            End If
            Return hydrogen
        End Function

        Public Shared Function isMetal(atomName As String) As String
            Dim metal = False
            Dim elementName = "  "
            Dim i = 0

            While i < METAL_NAME.Length AndAlso Not metal
                If atomName.Substring(0, 2).Equals(METAL_NAME(i).Substring(0, 2)) Then
                    If atomName(2) = " "c OrElse atomName(2) >= "0"c AndAlso atomName(2) <= "9"c Then
                        metal = True
                        elementName = METAL_NAME(i).Substring(0, 2)
                    End If
                End If

                i += 1
            End While
            Return elementName
        End Function

        Public Sub New(pdb As PDBEntry, atHet As Char, atomNumber As Integer, atomName As String, coords As Single(), bValue As Single, occupancy As Single, residue As Residue, element As String, originalCoords As Single())
            init()
            Me.pdb = pdb
            atHetField = atHet
            atomNumberField = atomNumber
            atomNameField = atomName
            fullAtomNameField = atomName.Trim()
            Dim i As Integer
            For i = 0 To 2
                Me.coords(i) = coords(i)
            Next
            For i = 0 To 2
                Me.originalCoords(i) = originalCoords(i)
            Next
            bValueField = bValue
            occupancyField = occupancy
            residueField = residue
            elementField1 = element
            residue.addAtom(Me)
            params = pdb.Params
            If params Is Nothing Then
                Return
            End If
            If params("PROGRAM").Equals("LIGPLOT") Then
                program = RunExe.LIGPLOT
            Else
                program = RunExe.DIMPLOT
            End If
            moleculeField = residue.Molecule
            moleculeType = moleculeField.MoleculeType
            If moleculeType = 3 Then
                spokeAngle = 12
            Else
                spokeAngle = 18
            End If
            nspokes = CInt(Math.Round(360.0F / spokeAngle, MidpointRounding.AwayFromZero))
            metal = False
            Dim elementName = isMetal(atomName)
            If Not elementName.Equals("  ") Then
                Dim hydrogen = isHydrogen(atomName)
                If Not hydrogen Then
                    metal = True
                End If
            End If
            setAtomColourType()
            setAtomColour()
            setAtomSizeType()
            setAtomSize()
        End Sub

        Public Overridable Property AtomName As String
            Set(value As String)
                atomNameField = value
            End Set
            Get
                Return atomNameField
            End Get
        End Property

        Public Overridable Property AtomNumber As Integer
            Set(value As Integer)
                atomNumberField = value
            End Set
            Get
                Return atomNumberField
            End Get
        End Property

        Public Overridable Sub setCoord(iCoord As Integer, newValue As Single)
            coords(iCoord) = newValue
        End Sub

        Friend Overridable Property Occupancy As Single
            Set(value As Single)
                occupancyField = value
            End Set
            Get
                Return occupancyField
            End Get
        End Property

        Public Overridable Property InUse As Boolean
            Set(value As Boolean)
                inUseField = value
            End Set
            Get
                Return inUseField
            End Get
        End Property

        Private Sub init()
            For i = 0 To 1
                plotCoords(i) = 0.0F
            Next
        End Sub

        Public Overridable Sub applyDimShifts(dimShiftX As Single, dimShiftY As Single)
            coords(0) = coords(0) + dimShiftX
            coords(1) = coords(1) + dimShiftY
            If atomLabelField IsNot Nothing Then
                atomLabelField.applyDimShifts(dimShiftX, dimShiftY)
            End If
        End Sub

        Public Overridable Sub calcPlotCoords(scale As Scale)
            plotDiameter = 2.0F * radiusField * scale.ScaleFactor
            Dim plotRadius = scale.ScaleFactor * radiusField
            For i = 0 To 1
                psCoords(i) = scale.ScaleFactor * (pdb.getOffset(i) + coords(i))
            Next
            plotCoords(0) = psCoords(0) - plotRadius
            plotCoords(1) = psCoords(1) + plotRadius
            plotCoords(1) = scale.PlotHeight - plotCoords(1)
        End Sub

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim clickObject As Object = Nothing
            If visible Then
                If atomLabelField IsNot Nothing Then
                    clickObject = atomLabelField.clickCheck(x, y)
                    If clickObject IsNot Nothing Then
                        Return clickObject
                    End If
                End If
                If x >= coords(0) - radiusField AndAlso x <= coords(0) + radiusField AndAlso y >= coords(1) - radiusField AndAlso y <= coords(1) + radiusField Then
                    Dim distSqrd = (x - coords(0)) * (x - coords(0)) + (y - coords(1)) * (y - coords(1))
                    If distSqrd < radiusField * radiusField Then
                        clickObject = Me
                    End If
                End If
            End If
            Return clickObject
        End Function

        Public Overridable Sub flipAtom(shiftX As Single, shiftY As Single, matrix As Double()(), inverseMatrix As Double()())
            coords = Angle.flipCoords(coords, shiftX, shiftY, matrix, inverseMatrix)
            If atomLabelField IsNot Nothing Then
                atomLabelField.flipTextItem(shiftX, shiftY, matrix, inverseMatrix)
            End If
        End Sub

        Public Overridable ReadOnly Property AtHet As Char
            Get
                Return atHetField
            End Get
        End Property

        Public Overridable ReadOnly Property AtomColour As Color
            Get
                setAtomColour()
                Return colour
            End Get
        End Property

        Public Overridable Property AtomLabel As TextItem
            Get
                Return atomLabelField
            End Get
            Set(value As TextItem)
                atomLabelField = value
            End Set
        End Property


        Public Overridable ReadOnly Property FullAtomName As String
            Get
                Return fullAtomNameField
            End Get
        End Property


        Public Overridable ReadOnly Property BValue As Single
            Get
                Return bValueField
            End Get
        End Property

        Public Overridable Function getCoord(i As Integer) As Single
            Return coords(i)
        End Function

        Public Overridable ReadOnly Property Element As String
            Get
                Return elementField1
            End Get
        End Property


        Public Overridable ReadOnly Property KeyWord As String
            Get
                If atHetField = "A"c Then
                    Return "ATOM  "
                End If
                Return "HETATM"
            End Get
        End Property

        Public Overridable ReadOnly Property Molecule As Molecule
            Get
                Dim lMolecule As Molecule = Nothing
                If residueField Is Nothing Then
                    Return lMolecule
                End If
                lMolecule = residueField.Molecule
                Return lMolecule
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property


        Public Overridable Function getOriginalCoord(i As Integer) As Single
            Return originalCoords(i)
        End Function

        Public Overridable Function getPlotCoords(i As Integer) As Single
            Return plotCoords(i)
        End Function

        Public Overridable Function getPlotRadius(scale As Scale) As Integer
            Return radiusField * scale.ScaleFactor + 0.5R
        End Function

        Public Overridable ReadOnly Property Radius As Single
            Get
                Return radiusField
            End Get
        End Property

        Public Overridable ReadOnly Property Residue As Residue
            Get
                Return residueField
            End Get
        End Property

        Public Overridable Function getSpoke(i As Integer) As Boolean
            Return spoke(i)
        End Function

        Public Overridable ReadOnly Property X As Single
            Get
                Return coords(0)
            End Get
        End Property

        Public Overridable ReadOnly Property Y As Single
            Get
                Return coords(1)
            End Get
        End Property

        Public Overridable Sub initSpokes()
            nspokesSet = 0
            For i = 0 To nspokes - 1
                spoke(i) = False
            Next
        End Sub

        Public Overridable ReadOnly Property InWantedResidue As Boolean
            Get
                Return residueField.Wanted
            End Get
        End Property

        Private Function isOn(typeName As String) As Boolean
            Dim [on] = False
            If Not ReferenceEquals(typeName, Nothing) Then
                Dim onOffString = params(typeName & "_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("ON") Then
                    [on] = True
                End If
            End If
            Return [on]
        End Function

        Public Overridable Property Wanted As Boolean
            Get
                Return wantedField
            End Get
            Set(value As Boolean)
                wantedField = value
            End Set
        End Property

        Public Overridable Sub moveAtom(realMoveX As Single, realMoveY As Single)
            coords(0) = coords(0) + realMoveX
            coords(1) = coords(1) - realMoveY
            If atomLabelField IsNot Nothing Then
                atomLabelField.moveTextItem(realMoveX, realMoveY)
            End If
        End Sub

        Public Overridable Sub rotateAtom(pivotX As Single, pivotY As Single, matrix As Double()())
            coords = Angle.applyRotationMatrix(pivotX, pivotY, coords, matrix)
            If atomLabelField IsNot Nothing Then
                atomLabelField.rotateTextItem(pivotX, pivotY, matrix)
            End If
        End Sub

        Public Overridable Sub restoreCoords(storePos As Integer)
            coords(0) = saveCoords_Conflict(storePos)(0)
            coords(1) = saveCoords_Conflict(storePos)(1)
            If atomLabelField IsNot Nothing Then
                atomLabelField.restoreCoords(storePos)
            End If
        End Sub

        Public Overridable Sub saveCoords(storePos As Integer)
            saveCoords_Conflict(storePos)(0) = coords(0)
            saveCoords_Conflict(storePos)(1) = coords(1)
            If atomLabelField IsNot Nothing Then
                atomLabelField.saveCoords(storePos)
            End If
        End Sub

        Public Overridable Sub paintAtom(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, nonEquivs As Boolean)
            Dim inactive = False
            Dim atomEdgeColour As Color = Nothing
            Dim setColour As Color = Nothing
            setAtomSize()
            setAtomColour()
            calcPlotCoords(scale)
            If Not selected Then
                Dim onOffString = params("INACTIVE_PLOTS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    visible = False
                ElseIf moleculeType <> 3 Then
                    visible = True
                End If
            ElseIf moleculeType <> 3 Then
                visible = True
            End If
            If visible AndAlso isOn(atomType) Then
                If selected AndAlso (Not nonEquivs AndAlso residueField.Equivalenced OrElse nonEquivs AndAlso Not residueField.Equivalenced AndAlso moleculeField.MoleculeType <> 1) Then
                    Dim underlay As Boolean
                    Dim onOffString = params("EQUIV_SIDECHAINS_UNDERLAY_STATUS")
                    If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                        underlay = False
                    Else
                        underlay = True
                    End If
                    If underlay Then
                        Dim colourName = params("UNDERLAY_COLOUR")
                        If ReferenceEquals(colourName, Nothing) Then
                            colourName = "BLACK"
                        End If
                        Dim underlayColour = ligplus.Params.getColour(colourName)
                        Dim diameter = 1.4F * plotDiameter
                        Dim shift = 0.399999976F * plotDiameter / 2.0F
                        If paintType = 0 Then
                            graphics.FillEllipse(New SolidBrush(underlayColour), plotCoords(0) - shift, plotCoords(1) - shift, diameter, diameter)
                        Else
                            psFile.psFilledCircle(psCoords(0), psCoords(1), diameter / 2.0F, underlayColour)
                        End If
                    End If
                End If
                If selected Then
                    setColour = colour
                Else
                    Dim colourName = params("INACTIVE_COLOUR")
                    Dim inactiveColour = ligplus.Params.getColour(colourName)
                    setColour = inactiveColour
                    inactive = True
                End If
                atomEdgeColour = setColour
                If isOn("ATOM_EDGES") AndAlso selected Then
                    atomEdgeColour = edgeColour
                End If
                Dim width = ATOM_EDGE_WIDTH
                Dim plotWidth = scale.ScaleFactor * width
                If paintType = 0 Then
                    graphics.FillEllipse(New SolidBrush(setColour), plotCoords(0), plotCoords(1), plotDiameter, plotDiameter)

                    Dim lineStroke As New Pen(atomEdgeColour, plotWidth)

                    graphics.DrawEllipse(lineStroke, plotCoords(0), plotCoords(1), plotDiameter, plotDiameter)
                ElseIf inactive Then
                    psFile.psFilledCircle(psCoords(0), psCoords(1), plotDiameter / 2.0F, setColour)
                Else
                    psFile.psSphere(psCoords(0), psCoords(1), plotDiameter / 2.0F, setColour, atomEdgeColour)
                End If
            End If
            If atomLabelField IsNot Nothing Then
                atomLabelField.paintTextItem(paintType, graphics, psFile, scale, selected)
            End If
            If nspokesSet > 0 Then
                plotSpokes(paintType, graphics, psFile, scale, selected)
            End If
        End Sub

        Public Overridable Sub plotSpokes(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean)
            Dim spokesVisible = True
            Dim colourName = params("HYDROPHOBIC_COLOUR")
            Dim spokeColour = ligplus.Params.getColour(colourName)
            Dim setColour As Color = Nothing
            If selected Then
                setColour = spokeColour
            Else
                Dim inactiveName = params("INACTIVE_COLOUR")
                Dim inactiveColour = ligplus.Params.getColour(inactiveName)
                setColour = inactiveColour
                Dim onOffString = params("INACTIVE_PLOTS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    spokesVisible = False
                End If
            End If
            If spokesVisible Then
                Dim width = 0.0F
                Dim numberString = params("SPOKE_WIDTH")
                If Not ReferenceEquals(numberString, Nothing) Then
                    width = Single.Parse(numberString) / 3.0F
                Else
                    width = 0.05F
                End If
                Dim plotWidth = scale.ScaleFactor * width
                Dim stroke As Pen = Pens.Black
                If paintType = 0 Then


                    stroke = New Pen(setColour, plotWidth)

                End If
                For i = 0 To nspokes - 1
                    If spoke(i) Then
                        Dim contactAngle = i * spokeAngle / 57.295779513082323R
                        Dim innerRadius = radiusField * 1.2F
                        Dim outerRadius = radiusField * 1.6F
                        Dim x1 = coords(0) + CSng(innerRadius * Math.Cos(contactAngle))
                        Dim y1 = coords(1) + CSng(innerRadius * Math.Sin(contactAngle))
                        Dim x2 = coords(0) + CSng(outerRadius * Math.Cos(contactAngle))
                        Dim y2 = coords(1) + CSng(outerRadius * Math.Sin(contactAngle))
                        Dim plotX1 = scale.ScaleFactor * (pdb.getOffset(0) + x1)
                        Dim plotY1 = scale.ScaleFactor * (pdb.getOffset(1) + y1)
                        Dim plotX2 = scale.ScaleFactor * (pdb.getOffset(0) + x2)
                        Dim plotY2 = scale.ScaleFactor * (pdb.getOffset(1) + y2)
                        plotY1 = scale.PlotHeight - plotY1
                        plotY2 = scale.PlotHeight - plotY2
                        If paintType = 0 Then
                            graphics.DrawLine(stroke, CInt(plotX1), CInt(plotY1), CInt(plotX2), CInt(plotY2))
                        Else
                            psFile.psDrawLine(plotX1, scale.PlotHeight - plotY1, plotX2, scale.PlotHeight - plotY2, plotWidth, setColour)
                        End If
                    End If
                Next
            End If
        End Sub

        Private Sub setAtomColour()
            colour = Color.Black
            Dim colourName = params(atomColourType & "_COLOUR")
            If Not ReferenceEquals(colourName, Nothing) Then
                colour = ligplus.Params.getColour(colourName)
            End If
        End Sub

        Private Sub setAtomColourType()
            If moleculeType = 4 Then
                atomColourType = "WATER"
            ElseIf metal Then
                atomColourType = "METAL"
            ElseIf atomNameField(1) = "N"c AndAlso atomNameField(0) <> "Z"c AndAlso atomNameField(0) <> "M"c Then
                atomColourType = "NITROGEN"
            ElseIf atomNameField(1) = "C"c Then
                atomColourType = "CARBON"
            ElseIf atomNameField(1) = "O"c AndAlso atomNameField(0) <> "C"c AndAlso atomNameField(0) <> "M"c Then
                atomColourType = "OXYGEN"
            ElseIf atomNameField(1) = "S"c Then
                atomColourType = "SULPHUR"
            ElseIf atomNameField(1) = "P"c Then
                atomColourType = "PHOSPHORUS"
            End If
        End Sub


        Private Sub setAtomSize()
            If atomSizeType.Equals("NO", StringComparison.OrdinalIgnoreCase) Then
                radiusField = 0.0F
            Else
                Dim numberString = params(atomSizeType & "_RADIUS")
                If Not ReferenceEquals(numberString, Nothing) Then
                    radiusField = Single.Parse(numberString)
                End If
            End If
        End Sub

        Private Sub setAtomSizeType()
            If moleculeType = 4 Then
                atomSizeType = "WATER"
                atomType = "WATER"
                If program = RunExe.DIMPLOT Then
                    atomSizeType = "WATER_DIM"
                End If
            ElseIf moleculeType = 3 Then
                atomSizeType = "NO"
                atomType = "NO"
                visible = False
            ElseIf moleculeType = 1 Then
                atomSizeType = "LIGATOM"
                atomType = "LIGATOM"
                If moleculeField.InterFace = 1 Then
                    atomType = "NLIGATOM"
                    atomSizeType = "IFACE_ATOM1"
                ElseIf moleculeField.InterFace = 2 Then
                    atomType = "NLIGATOM2"
                    atomSizeType = "IFACE_ATOM2"
                End If
            ElseIf moleculeType = 2 Then
                atomSizeType = "NLIGATOM"
                atomType = "NLIGATOM"
                If moleculeField.InterFace = 1 Then
                    atomType = "NLIGATOM"
                    atomSizeType = "IFACE_ATOM1"
                ElseIf moleculeField.InterFace = 2 Then
                    atomType = "NLIGATOM2"
                    atomSizeType = "IFACE_ATOM2"
                End If
            Else
                atomSizeType = "NO"
                atomType = "NO"
            End If
        End Sub

        Public Overridable WriteOnly Property Spokes As Single
            Set(value As Single)
                Dim angleStart As Integer = Math.Round(value - 60.0R, MidpointRounding.AwayFromZero)
                If angleStart < 0 Then
                    angleStart += 360
                End If
                Dim angleEnd = angleStart + 120
                Dim angle = 0
                For i = 0 To nspokes - 1
                    If angle >= angleStart AndAlso angle <= angleEnd Then
                        spoke(i) = True
                        nspokesSet += 1
                    End If
                    If angle + 360 >= angleStart AndAlso angle + 360 <= angleEnd Then
                        spoke(i) = True
                        nspokesSet += 1
                    End If
                    angle += spokeAngle
                Next
            End Set
        End Property


        Public Overrides Function ToString() As String
            Dim [string] As String = atomNumberField.ToString() & " " & atomNameField
            If residueField IsNot Nothing Then
                [string] = [string] & residueField.ToString()
            End If
            Return [string]
        End Function
    End Class

End Namespace
