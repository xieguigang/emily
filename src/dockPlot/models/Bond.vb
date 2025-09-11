Imports System.Drawing
Imports ligplus.ligplus
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Imaging

Namespace models

    Public Class Bond
        Public Const UNKNOWN As Integer = -1

        Public Const COVALENT As Integer = 0

        Public Const HBOND As Integer = 1

        Public Const CONTACT As Integer = 2

        Public Const INTERNAL As Integer = 3

        Public Const SALT_BRIDGE As Integer = 4

        Public Const DISULPHIDE As Integer = 5

        Public Const DUMMY As Integer = 6

        Public Const DELETED As Integer = -99

        Public Const [SINGLE] As Integer = 0

        Public Const [DOUBLE] As Integer = 1

        Public Const TRIPLE As Integer = 2

        Public Const FLAT As Integer = 0

        Public Const ROUNDED As Integer = 1

        Public Const DASH_GAP As Single = 0.08F

        Public Const DASH_WIDTH As Single = 0.2F

        Public Const DEFAULT_WIDTH As Single = 0.05F

        Public Const SHORT_DASH_WIDTH As Single = 0.1F

        Public Const EXPAND_BONDS As Single = 3.0F

        Private atom As Atom() = New Atom(1) {}

        Private checkedField As Boolean = False

        Private inEquivalencedResidue As Boolean = False

        Private inLigand As Boolean = False

        Private sameResidue As Boolean = False

        Private doneField As Boolean = False

        Private shortestBondField As Boolean = False

        Private dummyField As Boolean = False

        Private isAntigen As Boolean = False

        Private visible As Boolean = True

        Private fullWidth As Single = 0.0F

        Private width As Single = 0.0F

        Private lengthField As Single = 0.0F

        Private plotCoords1 As Integer() = New Integer(1) {}

        Private plotCoords2 As Integer() = New Integer(1) {}

        Private bondOrderField As Integer = 0

        Private order As Integer = 0

        Private program As Integer = RunExe.LIGPLOT

        Private typeField As Integer

        Private bondType As String = Nothing

        Private bondSizeType As String = Nothing

        Private antibodyLoopID As String = ""

        Private moleculeID As String = Nothing

        Private pdb As PDBEntry

        Private params As Properties

        Private bondLabelField As TextItem = Nothing

        Public Sub New(pdb As PDBEntry, atom As Atom(), type As Integer)
            Dim molecule = New Molecule(1) {}
            Dim residue = New Residue(1) {}
            Me.pdb = pdb
            Me.atom(0) = atom(0)
            Me.atom(1) = atom(1)
            typeField = type
            atom(0).InUse = True
            atom(1).InUse = True
            lengthField = 0.0F
            For i = 0 To 2
                Dim coord1 = atom(0).getOriginalCoord(i)
                Dim coord2 = atom(1).getOriginalCoord(i)
                lengthField += (coord1 - coord2) * (coord1 - coord2)
            Next
            lengthField = CSng(Math.Sqrt(lengthField))
            params = pdb.Params
            If params("PROGRAM").Equals("LIGPLOT") Then
                program = RunExe.LIGPLOT
            Else
                program = RunExe.DIMPLOT
            End If
            molecule(0) = atom(0).Molecule
            molecule(1) = atom(1).Molecule
            If molecule(0) Is molecule(1) Then
                moleculeID = molecule(0).MoleculeID
            End If
            residue(0) = atom(0).Residue
            residue(1) = atom(1).Residue
            If residue(0) Is residue(1) Then
                antibodyLoopID = residue(0).AntibodyLoopID
                isAntigen = residue(0).Antigen
            End If
            If molecule(0).MoleculeType = 7 Then
                dummyField = True
            End If
            If type = 0 Then
                If molecule(0).MoleculeType = 1 AndAlso molecule(1).MoleculeType = 1 Then
                    If molecule(0).InterFace = 0 Then
                        bondSizeType = "LIGBOND"
                        bondType = "LIGBOND"
                    ElseIf molecule(0).InterFace = 1 Then
                        bondType = "NLIGBOND"
                        bondSizeType = "IFACE_BOND1"
                    ElseIf molecule(0).InterFace = 2 Then
                        bondType = "NLIGBOND2"
                        bondSizeType = "IFACE_BOND2"
                    End If
                ElseIf molecule(0).MoleculeType <> 1 AndAlso molecule(1).MoleculeType <> 1 Then
                    bondSizeType = "NLIGBOND"
                    bondType = "NLIGBOND"
                    If molecule(0).InterFace = 1 Then
                        bondType = "NLIGBOND"
                        bondSizeType = "IFACE_BOND1"
                    ElseIf molecule(0).InterFace = 2 Then
                        bondType = "NLIGBOND2"
                        bondSizeType = "IFACE_BOND2"
                    End If
                Else
                    bondSizeType = "EXTBOND"
                    bondType = "EXTBOND"
                    If program = RunExe.DIMPLOT Then
                        bondSizeType = "EXTBOND_DIM"
                    End If
                End If
            ElseIf type = 1 OrElse type = 3 Then
                bondSizeType = "HBOND"
                bondType = "HBOND"
                If program = RunExe.DIMPLOT Then
                    bondSizeType = "HBOND_DIM"
                End If
            ElseIf type = 2 Then
                bondSizeType = "HYDROPHOBIC"
                bondType = "HYDROPHOBIC"
                If program = RunExe.DIMPLOT Then
                    bondSizeType = "HYDROPHOBIC_DIM"
                End If
            ElseIf type = 4 Then
                bondSizeType = "SALT_BRIDGE"
                bondType = "SALT_BRIDGE"
            ElseIf type = 5 Then
                bondSizeType = "DISULPHIDE"
                bondType = "DISULPHIDE"
            ElseIf type = 6 Then
                bondSizeType = "DUMMY"
                bondType = "DUMMY"
            Else
                bondSizeType = "UNKNOWN"
                bondType = "UNKNOWN"
            End If
        End Sub

        Private Sub calcBondWidth(doubleBonds As Boolean)
            Dim numberString = params(bondSizeType & "_WIDTH")
            If Not ReferenceEquals(numberString, Nothing) Then
                width = Single.Parse(numberString)
            Else
                width = 0.05F
            End If
            fullWidth = width
            bondOrderField = 0
            If order = 1 AndAlso doubleBonds Then
                bondOrderField = 1
            End If
            If order = 2 AndAlso doubleBonds Then
                bondOrderField = 2
            End If
            If bondOrderField = 1 Then
                width /= 2.0F
                fullWidth = width * 3.0F
            ElseIf bondOrderField = 2 Then
                width /= 3.0F
                fullWidth = width * 5.0F
            End If
        End Sub

        Private Function calcClickDist(x As Single, y As Single, clickDistCutOff As Single) As Boolean
            Dim angle As Double
            Dim clicked = False
            Dim coords1 = New Single(1) {}
            Dim coords2 = New Single(1) {}
            For i = 0 To 1
                coords1(i) = atom(0).getCoord(i)
                coords2(i) = atom(1).getCoord(i)
            Next
            Dim v1x As Double = coords2(0) - coords1(0)
            Dim v1y As Double = coords2(1) - coords1(1)
            Dim v2x As Double = x - coords1(0)
            Dim v2y As Double = y - coords1(1)
            Dim len1 As Single = Math.Sqrt(v1x * v1x + v1y * v1y)
            Dim len2 As Single = Math.Sqrt(v2x * v2x + v2y * v2y)
            Dim dot = v1x * v2x + v1y * v2y
            Dim calc = dot / (len1 * len2)
            If Math.Abs(calc) < 1.0R Then
                angle = Math.Acos(calc)
            Else
                angle = 0.0R
            End If
            Dim distance As Single = len2 * Math.Sin(angle)
            If distance <= clickDistCutOff Then
                clicked = True
            End If
            Return clicked
        End Function

        Public Overridable Sub calcPlotCoords(scale As Scale)
            Dim coords1 = New Single(1) {}
            Dim coords2 = New Single(1) {}
            coords1(0) = atom(0).getCoord(0)
            coords1(1) = atom(0).getCoord(1)
            coords2(0) = atom(1).getCoord(0)
            coords2(1) = atom(1).getCoord(1)
            Dim residue1 = atom(0).Residue
            Dim residue2 = atom(1).Residue
            plotCoords1(0) = CInt(scale.ScaleFactor * (pdb.getOffset(0) + coords1(0)))
            plotCoords1(1) = CInt(scale.ScaleFactor * (pdb.getOffset(1) + coords1(1)))
            plotCoords2(0) = CInt(scale.ScaleFactor * (pdb.getOffset(0) + coords2(0)))
            plotCoords2(1) = CInt(scale.ScaleFactor * (pdb.getOffset(1) + coords2(1)))
            plotCoords1(1) = scale.PlotHeight - plotCoords1(1)
            plotCoords2(1) = scale.PlotHeight - plotCoords2(1)
            If residue1 Is residue2 AndAlso residue1.Equivalenced Then
                inEquivalencedResidue = True
            Else
                inEquivalencedResidue = False
            End If
            If residue1 Is residue2 AndAlso residue1.Molecule.MoleculeType = 1 Then
                inLigand = True
            Else
                inLigand = False
            End If
            If residue1 Is residue2 Then
                sameResidue = True
            Else
                sameResidue = False
            End If
        End Sub

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim doubleBonds = True
            Dim clickObject As Object = Nothing
            Dim onOffString = params("DOUBLE_BONDS_STATUS")
            If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                doubleBonds = False
            End If
            If bondLabelField IsNot Nothing Then
                clickObject = bondLabelField.clickCheck(x, y)
                If clickObject IsNot Nothing Then
                    Return clickObject
                End If
            End If
            If typeField = 0 OrElse typeField = 6 Then
                Dim quickWidth = 2.0F * fullWidth
                If x < atom(0).getCoord(0) - quickWidth AndAlso x < atom(1).getCoord(0) - quickWidth OrElse x > atom(0).getCoord(0) + quickWidth AndAlso x > atom(1).getCoord(0) + quickWidth OrElse y < atom(0).getCoord(1) - quickWidth AndAlso y < atom(1).getCoord(1) - quickWidth OrElse y > atom(0).getCoord(1) + quickWidth AndAlso y > atom(1).getCoord(1) + quickWidth Then
                    Return clickObject
                End If
                Dim clickWidth = fullWidth
                If order = 1 AndAlso doubleBonds Then
                    clickWidth = CSng(2.5R * clickWidth)
                ElseIf order = 2 AndAlso doubleBonds Then
                    clickWidth = CSng(4.0R * clickWidth)
                End If
                Dim clickDistCutOff As Single = clickWidth / 2.0R
                Dim clicked = calcClickDist(x, y, clickDistCutOff)
                If clicked Then
                    clickObject = Me
                End If
            End If
            Return clickObject
        End Function

        Public Overridable Function getAtom(i As Integer) As Atom
            Return atom(i)
        End Function

        Public Overridable Property BondLabel As TextItem
            Get
                Return bondLabelField
            End Get
            Set(value As TextItem)
                bondLabelField = value
            End Set
        End Property

        Public Overridable Property BondOrder As Integer
            Get
                Return order
            End Get
            Set(value As Integer)
                order = value
            End Set
        End Property

        Public Overridable Property Checked As Boolean
            Get
                Return checkedField
            End Get
            Set(value As Boolean)
                checkedField = value
            End Set
        End Property

        Public Overridable Property Done As Boolean
            Get
                Return doneField
            End Get
            Set(value As Boolean)
                doneField = value
            End Set
        End Property

        Private ReadOnly Property DoubleBondsStatus As Boolean
            Get
                Dim doubleBonds As Boolean
                Dim onOffString = params("DOUBLE_BONDS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    doubleBonds = False
                Else
                    doubleBonds = True
                End If
                Return doubleBonds
            End Get
        End Property

        Public Overridable ReadOnly Property Length As Single
            Get
                Return lengthField
            End Get
        End Property

        Public Overridable ReadOnly Property Molecule As Molecule
            Get
                Return atom(0).Molecule
            End Get
        End Property

        Public Overridable ReadOnly Property OtherMolecule As Molecule
            Get
                Return atom(1).Molecule
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property

        Public Overridable ReadOnly Property Type As Integer
            Get
                Return typeField
            End Get
        End Property

        Public Overridable Function hasAtom(atom As Atom) As Boolean
            If atom.Equals(Me.atom(0)) OrElse atom.Equals(Me.atom(1)) Then
                Return True
            End If
            Return False
        End Function

        Private ReadOnly Property [On] As Boolean
            Get
                Dim lOn = True
                If Not ReferenceEquals(bondType, Nothing) Then
                    Dim onOffString = params(bondType & "_STATUS")
                    If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                        lOn = False
                    End If
                    If bondType.Equals("HBOND") AndAlso typeField = 3 Then
                        onOffString = params("INTERNAL_HBONDS_STATUS")
                        If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                            lOn = False
                        End If
                    End If
                    If bondType.Equals("HYDROPHOBIC") Then
                        onOffString = params("SHORTEST_ONLY_STATUS")
                        If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("ON") AndAlso Not shortestBondField Then
                            lOn = False
                        End If
                    End If
                End If
                Return lOn
            End Get
        End Property

        Public Overridable Sub paintBond(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, nonEquivs As Boolean)
            Dim dashed = False
            Dim colour As Color = Nothing
            Dim dashGap = 0.0F
            Dim dashWidth = 0.0F
            Dim wanted = isWanted(selected)
            Dim doubleBonds = DoubleBondsStatus
            width = 0.05F
            fullWidth = 0.05F
            If wanted AndAlso visible Then
                calcPlotCoords(scale)
                If [On] Then
                    Dim stroke As Pen
                    calcBondWidth(doubleBonds)
                    Dim plotWidth = scale.ScaleFactor * width
                    If selected AndAlso sameResidue AndAlso Not dummyField AndAlso (Not nonEquivs AndAlso inEquivalencedResidue OrElse nonEquivs AndAlso Not inEquivalencedResidue AndAlso Not inLigand) Then
                        Dim underlay As Boolean
                        Dim onOffString = params("EQUIV_SIDECHAINS_UNDERLAY_STATUS")
                        If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                            underlay = False
                        Else
                            underlay = True
                        End If
                        If underlay Then
                            plotUnderlay(paintType, graphics, psFile, scale, plotWidth)
                        End If
                    End If
                    Dim colourName = params(bondType & "_COLOUR")
                    If Not ReferenceEquals(colourName, Nothing) Then
                        colour = ligplus.Params.getColour(colourName)
                    Else
                        colour = Color.Black
                    End If
                    If Not antibodyLoopID.Equals("") Then
                        Dim name = "ABODY_" & antibodyLoopID & "_COLOUR"
                        If params.ContainsKey(name) Then
                            colourName = params(name)
                            If Not ReferenceEquals(colourName, Nothing) Then
                                colour = ligplus.Params.getColour(colourName)
                            End If
                        End If
                    ElseIf isAntigen Then
                        Dim name = "ABODY_ANTIGEN_COLOUR"
                        If params.ContainsKey(name) Then
                            colourName = params(name)
                            If Not ReferenceEquals(colourName, Nothing) Then
                                colour = ligplus.Params.getColour(colourName)
                            End If
                        End If
                    End If
                    If Not ReferenceEquals(moleculeID, Nothing) Then
                        Dim name = moleculeID & "_RESIDUE_COLOUR"
                        If params.ContainsKey(name) Then
                            colourName = params(name)
                            If Not ReferenceEquals(colourName, Nothing) Then
                                colour = ligplus.Params.getColour(colourName)
                            End If
                        End If
                    End If
                    If Not selected Then
                        Dim inactiveName = params("INACTIVE_COLOUR")
                        colour = ligplus.Params.getColour(inactiveName)
                    End If
                    If paintType = 0 Then
                        '  graphics.Color = colour
                    End If
                    If paintType = 0 Then
                        '  graphics2D = CType(graphics, Graphics2D)
                    End If
                    If bondType.Equals("HBOND") OrElse bondType.Equals("DUMMY") OrElse bondType.Equals("SALT_BRIDGE") OrElse bondType.Equals("DISULPHIDE") Then
                        dashWidth = scale.ScaleFactor * 0.2F
                        dashGap = scale.ScaleFactor * 0.08F
                        Dim dashPattern = New Single() {dashWidth, dashGap}
                        stroke = New Pen(colour, plotWidth) With {.DashStyle = DashStyle.Dash}
                        dashed = True
                    ElseIf bondType.Equals("HYDROPHOBIC") Then
                        dashWidth = scale.ScaleFactor * 0.1F
                        dashGap = dashWidth
                        Dim dashPattern = New Single() {dashWidth}
                        stroke = New Pen(Color.Black, plotWidth) With {.DashStyle = DashStyle.Dot}
                        dashed = True
                    Else
                        stroke = New Pen(Color.Black, plotWidth)
                    End If
                    If paintType = 0 Then
                        ' graphics2D.Stroke = stroke
                    ElseIf dashed Then
                        psFile.setDashPattern(dashWidth, dashWidth)
                    End If
                    plotSingleDoubleTriple(paintType, graphics, psFile, scale, selected, colourName, colour, plotWidth, bondOrderField)
                    If dashed AndAlso paintType = 1 Then
                        psFile.setDashPattern(0.0F, 0.0F)
                    End If
                    If bondType.Equals("HBOND") AndAlso bondLabelField IsNot Nothing Then
                        Dim coords = New Single(1) {}
                        For i = 0 To 1
                            coords(i) = (atom(0).getCoord(i) + atom(1).getCoord(i)) / 2.0F
                        Next
                        bondLabelField.Coords = coords
                        bondLabelField.paintTextItem(paintType, graphics, psFile, scale, selected)
                    End If
                End If
            End If
            If typeField = 2 Then
                Dim x1 = atom(0).getCoord(0)
                Dim y1 = atom(0).getCoord(1)
                Dim x2 = atom(1).getCoord(0)
                Dim y2 = atom(1).getCoord(1)
                Dim directionAngle As Single = Angle.calcAngle(x1, y1, x2, y2)
                atom(0).Spokes = directionAngle
                atom(1).Spokes = directionAngle - 180.0F
            End If
        End Sub





        Public Overridable WriteOnly Property ShortestBond As Boolean
            Set(value As Boolean)
                shortestBondField = value
            End Set
        End Property

        Public Overrides Function ToString() As String
            Dim [string] = ""
            For i = 0 To 1
                If i = 1 Then
                    [string] = [string] & " -> "
                End If
                Dim atm = atom(i)
                [string] = [string] & atm.AtomNumber.ToString() & " " & atm.AtomName
                Dim residue = atm.Residue
                If residue IsNot Nothing Then
                    [string] = [string] & residue.ToString()
                End If
            Next
            Return [string]
        End Function

        Private Function isWanted(selected As Boolean) As Boolean
            Dim wanted = True
            If typeField = -99 OrElse typeField = 0 AndAlso (atom(0).Molecule.MoleculeType = 3 OrElse atom(1).Molecule.MoleculeType = 3) Then
                wanted = False
            End If
            If Not selected Then
                Dim onOffString = params("INACTIVE_PLOTS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    visible = False
                Else
                    visible = True
                End If
            Else
                visible = True
            End If
            Return wanted
        End Function

        Private Sub plotBond(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, colourName As String, colour As Color, plotWidth As Single, shift As Single)
            Dim coords1 = New Single(1) {}
            Dim coords2 = New Single(1) {}
            Dim pCoords1 = New Single(1) {}
            Dim pCoords2 = New Single(1) {}
            coords1(0) = atom(0).getCoord(0)
            coords1(1) = atom(0).getCoord(1)
            coords2(0) = atom(1).getCoord(0)
            coords2(1) = atom(1).getCoord(1)
            If shift <> 0.0F Then
                Dim dx = 0.0R
                Dim dy = 0.0R
                Dim x As Double = coords2(0) - coords1(0)
                Dim y As Double = coords2(1) - coords1(1)
                If Math.Abs(x) < 0.00001R Then
                    dx = -shift
                    dy = 0.0R
                ElseIf Math.Abs(y) < 0.00001R Then
                    dx = 0.0R
                    dy = shift
                Else
                    Dim len = x * x + y * y
                    len = Math.Sqrt(len)
                    dx = -shift * y / len
                    dy = shift * x / len
                End If
                coords1(0) = coords1(0) + CSng(dx)
                coords1(1) = coords1(1) + CSng(dy)
                coords2(0) = coords2(0) + CSng(dx)
                coords2(1) = coords2(1) + CSng(dy)
            End If
            Dim residue1 = atom(0).Residue
            Dim residue2 = atom(1).Residue
            pCoords1(0) = CInt(scale.ScaleFactor * (pdb.getOffset(0) + coords1(0)))
            pCoords1(1) = CInt(scale.ScaleFactor * (pdb.getOffset(1) + coords1(1)))
            pCoords2(0) = CInt(scale.ScaleFactor * (pdb.getOffset(0) + coords2(0)))
            pCoords2(1) = CInt(scale.ScaleFactor * (pdb.getOffset(1) + coords2(1)))
            pCoords1(1) = scale.PlotHeight - pCoords1(1)
            pCoords2(1) = scale.PlotHeight - pCoords2(1)
            If selected AndAlso Not ReferenceEquals(colourName, Nothing) AndAlso colourName.Equals("ATOM_COLOUR") Then
                Dim midX As Single = (pCoords1(0) + pCoords2(0)) / 2.0F + 0.5R
                Dim midY As Single = (pCoords1(1) + pCoords2(1)) / 2.0F + 0.5R
                Dim colour1 = atom(0).AtomColour
                Dim colour2 = atom(1).AtomColour
                If paintType = 0 Then
                    ' graphics.Color = colour1
                    graphics.DrawLine(New Pen(colour1, 1), CInt(pCoords1(0)), CInt(pCoords1(1)), CInt(midX), CInt(midY))
                    ' graphics.Color = colour2
                    graphics.DrawLine(New Pen(colour2, 1), CInt(midX), CInt(midY), CInt(pCoords2(0)), CInt(pCoords2(1)))
                Else
                    psFile.psDrawLine(pCoords1(0), scale.PlotHeight - pCoords1(1), midX, scale.PlotHeight - midY, plotWidth, colour1)
                    psFile.psDrawLine(midX, scale.PlotHeight - midY, pCoords2(0), scale.PlotHeight - pCoords2(1), plotWidth, colour2)
                End If
            ElseIf paintType = 0 Then
                graphics.DrawLine(Pens.Black, CInt(pCoords1(0)), CInt(pCoords1(1)), CInt(pCoords2(0)), CInt(pCoords2(1)))
            Else
                psFile.psDrawLine(pCoords1(0), scale.PlotHeight - pCoords1(1), pCoords2(0), scale.PlotHeight - pCoords2(1), plotWidth, colour)
            End If
        End Sub

        Private Sub plotSingleDoubleTriple(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, colourName As String, colour As Color, plotWidth As Single, bondOrder As Integer)
            If bondOrder <> 1 Then
                plotBond(paintType, graphics, psFile, scale, selected, colourName, colour, plotWidth, 0.0F)
            End If
            If bondOrder = 1 OrElse bondOrder = 2 Then
                Dim shift As Single
                If bondOrder = 1 Then
                    shift = width
                Else
                    shift = 2.0F * width
                End If
                For i = 0 To 1
                    plotBond(paintType, graphics, psFile, scale, selected, colourName, colour, plotWidth, shift)
                    shift = -shift
                Next
            End If
        End Sub

        Private Sub plotUnderlay(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, plotWidth As Single)
            Dim selected = True
            Dim underlayWidth = plotWidth * 3.0F
            Dim underlayName = params("UNDERLAY_COLOUR")
            Dim underlayColour = Color.Black
            If Not ReferenceEquals(underlayName, Nothing) Then
                underlayColour = ligplus.Params.getColour(underlayName)
            End If
            If paintType = 0 Then
                ' graphics.Color = underlayColour
                '  graphics2D = CType(graphics, Graphics2D)
                Dim stroke As New Pen(Color.Black, underlayWidth)
                ' graphics2D.Stroke = stroke
            End If
            plotSingleDoubleTriple(paintType, graphics, psFile, scale, selected, underlayName, underlayColour, underlayWidth, bondOrderField)
        End Sub
    End Class

End Namespace
