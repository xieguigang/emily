Imports System.Drawing
Imports ligplus.ligplus
Imports ligplus.models
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq

Namespace pdb

    Public Class Residue
        Public Shared nResidues As Integer = 0

        Public Const UNKNOWN As Integer = 0

        Public Const STANDARD As Integer = 1

        Public Const NON_STANDARD As Integer = 2

        Public Const NUCLEIC_ACID As Integer = 3

        Public Const METAL_ION As Integer = 4

        Public Const WATER As Integer = 5

        Public Const RESIDUE_ATTACHMENT As Integer = 6

        Public Const NRESIDUE_TYPES As Integer = 7

        Public Const NORMAL_PLOT As Integer = 0

        Public Const PLOT_UNDERLAY As Integer = 1

        Private Const X As Integer = 0

        Private Const Y As Integer = 1

        Public Shared ReadOnly aaName As String() = New String() {"ALA", "CYS", "ASP", "GLU", "PHE", "GLY", "HIS", "ILE", "LYS", "LEU", "MET", "ASN", "PRO", "GLN", "ARG", "SER", "THR", "VAL", "TRP", "TYR"}

        Public Const amino As String = "ACDEFGHIKLMNPQRSTVWY"

        Public Shared ReadOnly baseName As String() = New String() {"  A", "  C", "  G", "  I", "  T", "  U", " +A", " +C", " +G", " +I", " +T", " +U", " DA", " DC", " DG", " DI", " DT", " DU"}

        Public Const base As String = "ACGITUACGITUACGITU"

        Private Const MAX_SPOKES As Integer = 30

        Public Const SPOKE_EXTENT As Single = 1.4F

        Public Const SPOKE_MIN As Single = 1.0F

        Public Const ARC_ANGLE As Double = 120.0R

        Public Const DEFAULT_ELLIPSE_WIDTH As Single = 0.1F

        Public Const ELLIPSE_MARGIN As Single = 1.0F

        Public Const EXPAND_SPOKES As Single = 2.0F

        Public Const UNDERLAY_SHIFT As Single = 0.025F

        Public Const ANGLE_STEP As Single = 10.0F

        Private first As Boolean = True

        Private moved As Boolean = True

        Private postTERField As Boolean = False

        Private visible As Boolean = True

        Private wantedField As Boolean = False

        Private spoke As Boolean() = New Boolean(29) {}

        Private aaCodeField As Char = "X"c

        Private atHet As Char = " "c

        Private chainField As Char = " "c

        Private fullInsCodeField As String = " "

        Private fullChainField As String = " "

        Private domainField As Integer = 0

        Private residueTypeField As Integer = 0

        Private moleculeType As Integer = 3

        Private nArcs As Integer = 0

        Private nAtomsField As Integer = 0

        Private nBonds As Integer = 0

        Private nCoords As Integer = 0

        Private nspokes As Integer = 0

        Private nspokesSet As Integer = 0

        Private program As Integer = RunExe.LIGPLOT

        Private fullResNumField As Integer = -99

        Private ellipseRadius As Single() = New Single(1) {}

        Private plotEllipseDiameter As Single() = New Single(1) {}

        Private plotDiameter As Single = 0.0F

        Private plotRadiusField As Single = 0.0F

        Private arc As Single()() = RectangularArray.Matrix(Of Single)(30, 2)

        Private radius As Single = 0.0F

        Private theta As Single = 0.0F

        Private coords As Single() = New Single(1) {}

        Private coordsAccum As Single() = New Single(2) {}

        Private coordsCentre As Single() = New Single(2) {}

        Private coordsMin As Single() = New Single(2) {}

        Private coordsMax As Single() = New Single(2) {}

        Private ellipseCentre As Single() = New Single(1) {}

        Private plotCoords As Single() = New Single(1) {}

        Private plotEllipseCoords As Single() = New Single(1) {}

        Private psCoords As Single() = New Single(1) {}

        Private psEllipseCentre As Single() = New Single(1) {}

        Private psEllipseCoords As Single() = New Single(1) {}

        Private saveEllipseCentre As Single()() = RectangularArray.Matrix(Of Single)(10, 2)

        Private saveEllipseRadius As Single()() = RectangularArray.Matrix(Of Single)(10, 2)

        Private saveTheta As Single() = New Single(9) {}

        Private antibodyLoopIDField As String = ""

        Private isAntigen As Boolean = False

        Private residueSizeType As String = "HPHOBIC_RADIUS"

        Public hetGroupField As HetGroup = Nothing


        Private atomListField As List(Of Atom)

        Private equivalentResiduesList As List(Of Object) = Nothing

        Private pdb As PDBEntry = Nothing

        Private params As Properties

        Private attachedResidue As Residue = Nothing

        Private residueIdField As String = Nothing

        Private resNameField As String

        Private entityDescField As String = Nothing

        Private fullResNameField As String = Nothing

        Private resNumField As String

        Private residueLabelField As TextItem = Nothing

        Public Sub New(pdb As PDBEntry, resName As String, resNum As String, chain As Char, fullResName As String, fullResNum As Integer, fullInsCode As String, fullChain As String, ensemble As Ensemble, moleculeType As Integer)
            resNameField = resName
            resNumField = resNum
            chainField = chain
            fullResNameField = fullResName
            fullResNumField = fullResNum
            fullChainField = fullChain
            fullInsCodeField = fullInsCode
            params = pdb.Params
            If ensemble IsNot Nothing Then
                program = ensemble.Program
            End If
            Me.moleculeType = moleculeType
            init(pdb)
            Me.pdb = pdb
            nResidues += 1
            first = True
        End Sub

        Public Overridable ReadOnly Property Equivalenced As Boolean
            Get
                Dim lEquivalenced = False
                If equivalentResiduesList.Count > 0 Then
                    lEquivalenced = True
                End If
                Return lEquivalenced
            End Get
        End Property

        Private Sub init(pdb As PDBEntry)
            attachedResidue = Nothing
            For i = 0 To 2
                coordsMin(i) = 0.0F
                coordsMax(i) = 0.0F
                coordsCentre(i) = 0.0F
            Next
            postTERField = False
            residueTypeField = 0
            atomListField = New List(Of Atom)()
            equivalentResiduesList = New List(Of Object)()
            residueIdField = String.Format("[{0} {1} {2}]", New Object() {resNameField, resNumField, Convert.ToChar(chainField)})
        End Sub

        Public Overridable Sub applyDimShifts(cutPointX As Single, cutPointY As Single, dimShiftX As Single, dimShiftY As Single)
            Dim dimShift = New Single(1) {}
            dimShift(0) = 0.0F
            dimShift(1) = 0.0F
            If coordsMin(0) >= cutPointX OrElse coordsMin(1) < cutPointY Then
                dimShift(0) = dimShiftX
                dimShift(1) = dimShiftY
            ElseIf coordsMax(0) >= cutPointX OrElse coordsMax(1) < cutPointY Then
                Dim shift = False
                Dim dist = Math.Abs(coordsMax(0) - cutPointX)
                If Math.Abs(coordsMin(0) - cutPointX) < dist Then
                    dist = Math.Abs(coordsMin(0) - cutPointX)
                    shift = True
                End If
                If Math.Abs(coordsMax(1) - cutPointY) < dist Then
                    dist = Math.Abs(coordsMax(1) - cutPointY)
                    shift = False
                End If
                If Math.Abs(coordsMin(1) - cutPointY) < dist Then
                    dist = Math.Abs(coordsMin(1) - cutPointY)
                    shift = True
                End If
                If shift Then
                    dimShift(0) = dimShiftX
                    dimShift(1) = dimShiftY
                End If
            End If
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.applyDimShifts(dimShift(0), dimShift(1))
            Next
            updateMaxMinCoords()
            If residueLabelField IsNot Nothing Then
                residueLabelField.applyDimShifts(dimShift(0), dimShift(1))
            End If
        End Sub

        Private Sub calcEllipseCoords(scale As Scale)
            Dim plotRadius = New Single(1) {}
            For i = 0 To 1
                plotEllipseDiameter(i) = 2.0F * ellipseRadius(i) * scale.ScaleFactor
                psEllipseCentre(i) = scale.ScaleFactor * (pdb.getOffset(i) + ellipseCentre(i))
                plotRadius(i) = scale.ScaleFactor * ellipseRadius(i)
            Next
            plotEllipseCoords(0) = psEllipseCentre(0) - plotRadius(0)
            plotEllipseCoords(1) = psEllipseCentre(1) + plotRadius(1)
            plotEllipseCoords(1) = scale.PlotHeight - plotEllipseCoords(1)
            psEllipseCoords(0) = psEllipseCentre(0)
            psEllipseCoords(1) = scale.PlotHeight - psEllipseCentre(1)
        End Sub

        Private Sub calcEquivEllipse()
            theta = 0.0F
            Dim atomCoords = RectangularArray.Matrix(Of Single)(nAtomsField, 2)
            Dim cOfG = New Single(1) {}
            cOfG(1) = 0.0F
            cOfG(0) = 0.0F
            Dim maxRadius = 0.0F
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atomCoords(i)(0) = atom.getCoord(0)
                atomCoords(i)(1) = atom.getCoord(1)
                cOfG(0) = cOfG(0) + atomCoords(i)(0)
                cOfG(1) = cOfG(1) + atomCoords(i)(1)
                Dim atomRadius = atom.Radius
                If atomRadius > maxRadius Then
                    maxRadius = atomRadius
                End If
            Next
            If nAtomsField > 0 Then
                cOfG(0) = cOfG(0) / nAtomsField
                ellipseCentre(0) = cOfG(0) / nAtomsField
                cOfG(1) = cOfG(1) / nAtomsField
                ellipseCentre(1) = cOfG(1) / nAtomsField
            End If
            If Molecule.MoleculeType <> 3 Then
                radius = maxRadius
            End If
            If nAtomsField < 2 OrElse Molecule.MoleculeType = 3 Then
                ellipseRadius(0) = radius + 1.0F
                ellipseRadius(1) = radius + 1.0F
            Else
                getBestEllipse(atomCoords, cOfG, nAtomsField)
            End If
            first = False
        End Sub

        Private Sub getBestEllipse(atomCoords As Single()(), cOfG As Single(), nAtoms As Integer)
            Dim angle = 0.0F
            Dim maxRatio = 0.0F
            Dim matrix = {New Double(1) {}, New Double(1) {}}
            Dim cMin = New Single(1) {}
            Dim cMax = New Single(1) {}
            For i = 0 To nAtoms - 1
                atomCoords(i)(0) = atomCoords(i)(0) - cOfG(0)
                atomCoords(i)(1) = atomCoords(i)(1) - cOfG(1)
            Next
            Dim nSteps = 18
            matrix = ligplus.Angle.calcRotMatrix(10.0R)
            For iStep = 0 To nSteps - 1
                For j = 0 To nAtoms - 1
                    If j = 0 Then
                        cMax(0) = atomCoords(j)(0)
                        cMin(0) = atomCoords(j)(0)
                        cMax(1) = atomCoords(j)(1)
                        cMin(1) = atomCoords(j)(1)
                    Else
                        For iCoord = 0 To 1
                            If atomCoords(j)(iCoord) < cMin(iCoord) Then
                                cMin(iCoord) = atomCoords(j)(iCoord)
                            End If
                            If atomCoords(j)(iCoord) > cMax(iCoord) Then
                                cMax(iCoord) = atomCoords(j)(iCoord)
                            End If
                        Next
                    End If
                    atomCoords(j) = ligplus.Angle.applyRotationMatrix(0.0F, 0.0F, atomCoords(j), matrix)
                Next
                Dim width = cMax(0) - cMin(0) + 2.0F
                Dim height = cMax(1) - cMin(1) + 2.0F
                Dim ratio = width / height
                If ratio > maxRatio Then
                    If angle > 0.0F Then
                        theta = -angle
                    Else
                        theta = 0.0F
                    End If
                    ellipseRadius(0) = width / 2.0F
                    ellipseRadius(1) = height / 2.0F
                    maxRatio = ratio
                End If
                angle += 10.0F
            Next
        End Sub

        Public Overridable Sub addAtom(atom As Atom)
            atomListField.Add(atom)
            For i = 0 To 2
                Dim coord = atom.getCoord(i)
                If nAtomsField = 0 Then
                    coordsMax(i) = coord
                    coordsMin(i) = coord
                    coordsCentre(i) = coord
                    coordsAccum(i) = coord
                Else
                    coordsAccum(i) = coordsAccum(i) + coord
                    coordsCentre(i) = coordsAccum(i) / (nAtomsField + 1)
                    If coord > coordsMax(i) Then
                        coordsMax(i) = coord
                    End If
                    If coord < coordsMin(i) Then
                        coordsMin(i) = coord
                    End If
                End If
            Next
            If nAtomsField = 0 Then
                atHet = atom.AtHet
                If atHet = "H"c AndAlso pdb IsNot Nothing Then
                    Dim hetGroupList As List(Of HetGroup) = pdb.HetGroupList
                    Dim j = 0

                    While j < hetGroupList.Count AndAlso hetGroupField Is Nothing
                        Dim het = hetGroupList(j)
                        If fullResNameField.Equals(het.HetName) Then
                            hetGroupField = het
                        End If

                        j += 1
                    End While
                End If
            End If
            nAtomsField += 1
        End Sub

        Public Overridable Sub addAttachment(residue As Residue)
            attachedResidue = residue
        End Sub

        Public Overridable Sub calcPlotCoords(scale As Scale)
            Dim numberString = params(residueSizeType)
            radius = Single.Parse(numberString)
            plotDiameter = 2.0F * radius * scale.ScaleFactor
            coords = HGroupCoords
            plotRadiusField = scale.ScaleFactor * radius
            psCoords(0) = scale.ScaleFactor * (pdb.getOffset(0) + coords(0))
            psCoords(1) = scale.ScaleFactor * (pdb.getOffset(1) + coords(1))
            plotCoords(0) = psCoords(0) - plotRadiusField
            plotCoords(1) = psCoords(1) + plotRadiusField
            plotCoords(1) = scale.PlotHeight - plotCoords(1)
        End Sub

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim clickObject As Object = Nothing
            If residueLabelField IsNot Nothing Then
                clickObject = residueLabelField.clickCheck(x, y)
                If clickObject IsNot Nothing Then
                    Return clickObject
                End If
            End If
            If x < coordsMin(0) OrElse x > coordsMax(0) OrElse y < coordsMin(1) OrElse y > coordsMax(1) Then
                Return clickObject
            End If
            If Molecule.MoleculeType = 3 Then
                coords = HGroupCoords
                Dim distSqrd = (x - coords(0)) * (x - coords(0)) + (y - coords(1)) * (y - coords(1))
                If distSqrd < 1.95999992F * radius * radius Then
                    clickObject = Me
                End If
            Else
                Dim i = 0

                While i < atomListField.Count AndAlso clickObject Is Nothing
                    Dim atom As Atom = atomListField(i)
                    clickObject = atom.clickCheck(x, y)
                    i += 1
                End While
            End If
            Return clickObject
        End Function

        Public Overridable Function countWantedAtoms() As Integer
            Dim nWanted = 0
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                If atom.Wanted Then
                    nWanted += 1
                End If
            Next
            Return nWanted
        End Function

        Public Overridable Sub determineResidueType()
            If residueTypeField <> 0 Then
                Return
            End If
            residueTypeField = 0
            If nAtomsField = 1 Then
                Dim atom As Atom = atomListField(0)
                If Not Atom.isMetal(atom.AtomName).Equals("  ") Then
                    residueTypeField = 4
                End If
            End If
            If fullResNameField.Equals("HOH") Then
                residueTypeField = 5
            End If
            Dim i As Integer
            i = 0

            While i < aaName.Length AndAlso residueTypeField = 0
                If fullResNameField.Equals(aaName(i)) Then
                    aaCodeField = "ACDEFGHIKLMNPQRSTVWY"(i)
                    residueTypeField = 1
                End If

                i += 1
            End While
            i = 0

            While i < baseName.Length AndAlso residueTypeField = 0
                If fullResNameField.Equals(baseName(i)) Then
                    residueTypeField = 3
                End If

                i += 1
            End While
            If residueTypeField = 0 Then
                Dim haveN = False
                Dim haveCA = False
                Dim haveC = False
                Dim haveO = False
                For j = 0 To atomListField.Count - 1
                    Dim atom As Atom = atomListField(j)
                    Dim atomName = atom.AtomName
                    If atomName.Equals(" N  ") Then
                        haveN = True
                    End If
                    If atomName.Equals(" CA ") Then
                        haveCA = True
                    End If
                    If atomName.Equals(" C  ") Then
                        haveC = True
                    End If
                    If atomName.Equals(" O  ") Then
                        haveO = True
                    End If
                Next
                If haveN AndAlso haveCA AndAlso haveC AndAlso haveO Then
                    residueTypeField = 2
                End If
            End If
        End Sub

        Public Overridable Sub flipAtoms(shiftX As Single, shiftY As Single, matrix As Double()(), inverseMatrix As Double()())
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                If atom.Wanted Then
                    atom.flipAtom(shiftX, shiftY, matrix, inverseMatrix)
                End If
            Next
            moved = True
        End Sub

        Public Overridable ReadOnly Property AaCode As String
            Get
                Return "" & aaCodeField.ToString()
            End Get
        End Property

        Public Overridable Property AntibodyLoopID As String
            Get
                Return antibodyLoopIDField
            End Get
            Set(value As String)
                If value.Equals("H2") Then
                    Console.WriteLine("Setting antibodyLoopID to " & value & FullResName & FullResNum.ToString() & FullChain)
                End If
                antibodyLoopIDField = value
            End Set
        End Property

        Public Overridable ReadOnly Property Antigen As Boolean
            Get
                Return isAntigen
            End Get
        End Property

        Public Overridable ReadOnly Property AtomList As List(Of Atom)
            Get
                Return atomListField
            End Get
        End Property

        Public Overridable ReadOnly Property Attachment As Residue
            Get
                Return attachedResidue
            End Get
        End Property

        Public Overridable Property Chain As Char
            Get
                Return chainField
            End Get
            Set(value As Char)
                chainField = value
            End Set
        End Property

        Public Overridable Function getCoordsAccum(i As Integer) As Single
            Return coordsAccum(i)
        End Function

        Public Overridable Function getCoordsCentre(i As Integer) As Single
            Return coordsCentre(i)
        End Function

        Public Overridable Function getCoordsMin(i As Integer) As Single
            Return coordsMin(i)
        End Function

        Public Overridable Function getCoordsMax(i As Integer) As Single
            Return coordsMax(i)
        End Function

        Public Overridable Property Domain As Integer
            Get
                Return domainField
            End Get
            Set(value As Integer)
                domainField = value
            End Set
        End Property

        Public Overridable Function getResidueEquivalence(iRes As Integer) As Object()
            Dim equivalence = New Object(1) {}
            equivalence = equivalentResiduesList(iRes)
            Return equivalence
        End Function

        Public Overridable ReadOnly Property HetGroupProp As HetGroup
            Get
                Return hetGroupField
            End Get
        End Property

        Public Overridable ReadOnly Property HGroupPlotCoords As Single()
            Get
                Return plotCoords
            End Get
        End Property

        Friend Overridable Property DummyCoords As Single()
            Get
                Dim lDummyCoords = New Single(4) {}
                lDummyCoords(0) = 1.0F
                Dim atom As Atom = atomListField(0)
                lDummyCoords(1) = atom.X
                lDummyCoords(2) = atom.Y
                atom = atomListField(1)
                lDummyCoords(3) = atom.X
                lDummyCoords(4) = atom.Y
                Return lDummyCoords
            End Get
            Set(value As Single())
                Dim atom As Atom = atomListField(0)
                atom.setCoord(0, value(1))
                atom.setCoord(1, value(2))
                atom = atomListField(1)
                atom.setCoord(0, value(3))
                atom.setCoord(1, value(4))
            End Set
        End Property

        Public Overridable ReadOnly Property HGroupCoords As Single()
            Get
                Dim hCoords = New Single(1) {}
                If atomListField.Count > 0 Then
                    Dim atom As Atom = atomListField(0)
                    hCoords(0) = atom.getCoord(0)
                    hCoords(1) = atom.getCoord(1)
                Else
                    hCoords(1) = 0.0F
                    hCoords(0) = 0.0F
                End If
                Return hCoords
            End Get
        End Property

        Public Overridable Property Molecule As Molecule

        Public Overridable ReadOnly Property Natoms As Integer
            Get
                Return nAtomsField
            End Get
        End Property

        Public Overridable ReadOnly Property Nequiv As Integer
            Get
                Return equivalentResiduesList.Count
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property

        Public Overridable ReadOnly Property PlotRadius As Single
            Get
                Return plotRadiusField
            End Get
        End Property

        Public Overridable ReadOnly Property PostTER As Boolean
            Get
                Return postTERField
            End Get
        End Property

        Public Overridable ReadOnly Property ResidueId As String
            Get
                Return residueIdField
            End Get
        End Property

        Public Overridable Property ResidueLabel As TextItem
            Get
                Return residueLabelField
            End Get
            Set(value As TextItem)
                residueLabelField = value
            End Set
        End Property

        Public Overridable Property ResidueType As Integer
            Get
                Return residueTypeField
            End Get
            Set(value As Integer)
                residueTypeField = value
            End Set
        End Property

        Public Overridable Property ResName As String
            Get
                Return resNameField
            End Get
            Set(value As String)
                resNameField = value
            End Set
        End Property

        Public Overridable Property ResNum As String
            Get
                Return resNumField
            End Get
            Set(value As String)
                resNumField = value
            End Set
        End Property

        Public Overridable ReadOnly Property FullChain As String
            Get
                Return fullChainField
            End Get
        End Property

        Public Overridable ReadOnly Property FullResName As String
            Get
                Return fullResNameField
            End Get
        End Property

        Public Overridable ReadOnly Property FullResNum As Integer
            Get
                Return fullResNumField
            End Get
        End Property

        Public Overridable Sub harvestSpokes()
            nspokes = 0
            nspokesSet = 0
            nspokes = CInt(Math.Round(30.0F, MidpointRounding.AwayFromZero))
            For i = 0 To nspokes - 1
                spoke(i) = False
            Next
            For iAtom = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(iAtom)
                For j = 0 To nspokes - 1
                    If atom.getSpoke(j) Then
                        spoke(j) = True
                        nspokesSet += 1
                    End If
                Next
            Next
        End Sub

        Private Sub identifyArcs()
            nArcs = 0
            Dim firstSpoke = -1
            Dim inBlanks = False
            Dim i = 0

            While i < nspokes + 1 AndAlso firstSpoke = -1
                Dim ispoke = i
                If ispoke >= nspokes Then
                    ispoke -= nspokes
                End If
                If spoke(ispoke) AndAlso inBlanks Then
                    firstSpoke = ispoke
                ElseIf Not spoke(ispoke) Then
                    inBlanks = True
                End If

                i += 1
            End While
            If firstSpoke <> -1 Then
                Dim ispoke = firstSpoke
                Dim angle As Single = ispoke * 12
                arc(nArcs)(0) = angle
                arc(nArcs)(1) = 0.0F
                inBlanks = False
                For j = 1 To nspokes - 1
                    ispoke += 1
                    If ispoke >= nspokes Then
                        ispoke = 0
                    End If
                    angle = ispoke * 12
                    If Not spoke(ispoke) AndAlso Not inBlanks Then
                        inBlanks = True
                        nArcs += 1
                    ElseIf spoke(ispoke) Then
                        If Not inBlanks Then
                            arc(nArcs)(1) = arc(nArcs)(1) + 12.0F
                        Else
                            arc(nArcs)(0) = angle
                            arc(nArcs)(1) = 0.0F
                        End If
                        inBlanks = False
                    End If
                Next
            End If
        End Sub

        Public Overridable ReadOnly Property HetGroup As Boolean
            Get
                If atHet = "H"c Then
                    Return True
                End If
                Return False
            End Get
        End Property

        Public Overridable Property Wanted As Boolean
            Get
                Return wantedField
            End Get
            Set(value As Boolean)
                wantedField = value
            End Set
        End Property

        Friend Overridable Sub moveResidue(realMoveX As Single, realMoveY As Single)
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.moveAtom(realMoveX, realMoveY)
            Next
            If residueLabelField IsNot Nothing Then
                residueLabelField.moveTextItem(realMoveX, realMoveY)
            End If
            moved = True
        End Sub

        Public Overridable Sub paintResidue(paintType As Integer, g As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, nonEquivs As Boolean)
            If Molecule.MoleculeType <> 3 Then
                For i = 0 To atomListField.Count - 1
                    Dim atom As Atom = atomListField(i)
                    atom.paintAtom(paintType, g, psFile, scale, selected, nonEquivs)
                Next
            ElseIf Molecule.MoleculeType = 3 Then
                calcPlotCoords(scale)
                harvestSpokes()
                If selected AndAlso (Not nonEquivs AndAlso Equivalenced OrElse nonEquivs AndAlso Not Equivalenced) AndAlso Molecule.MoleculeType <> 1 AndAlso Molecule.MoleculeType <> 7 Then
                    Dim underlay As Boolean
                    Dim onOffString = params("EQUIV_HGROUPS_UNDERLAY_STATUS")
                    If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                        underlay = False
                    Else
                        underlay = True
                    End If
                    If underlay Then
                        plotHgroup(paintType, g, psFile, scale, selected, 1)
                    End If
                End If
                plotHgroup(paintType, g, psFile, scale, selected, 0)
            End If
            If residueLabelField IsNot Nothing Then
                residueLabelField.paintTextItem(paintType, g, psFile, scale, selected)
            End If
            If (Not nonEquivs AndAlso Equivalenced OrElse nonEquivs AndAlso Not Equivalenced) AndAlso Molecule.MoleculeType <> 1 AndAlso Molecule.MoleculeType <> 7 Then
                Dim ellipse As Boolean
                Dim onOffString = params("EQUIV_ELLIPSES_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    ellipse = False
                Else
                    ellipse = True
                End If
                If ellipse Then
                    plotEquivEllipse(paintType, g, psFile, scale, selected)
                End If
            End If
        End Sub

        Private Sub plotArcs(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, setColour As Color, plotMode As Integer)
            ' Dim graphics2D As Graphics2D = Nothing
            Dim width = 0.0F
            Dim numberString = params("ARC_WIDTH")
            If Not ReferenceEquals(numberString, Nothing) Then
                width = Single.Parse(numberString)
            Else
                width = 0.05F
            End If
            If plotMode = 1 Then
                width = 2.0F * width
            End If
            Dim plotWidth = scale.ScaleFactor * width
            If paintType = 0 Then
                ' graphics2D = CType(graphics, Graphics2D)
            End If
            Dim stroke As New Pen(setColour, plotWidth)
            If paintType = 0 Then
                ' graphics2D.Stroke = stroke
            End If
            For i = 0 To nArcs - 1
                If paintType = 0 Then
                    graphics.DrawArc(stroke, CInt(plotCoords(0)), CInt(plotCoords(1)), CInt(plotDiameter), CInt(plotDiameter), CInt(arc(i)(0)), CInt(arc(i)(1)))
                Else
                    Dim endAngle = arc(i)(0) + arc(i)(1)
                    Dim startAngle = arc(i)(0)
                    psFile.psArc(psCoords(0), psCoords(1), plotDiameter / 2.0F, startAngle, endAngle, plotWidth, setColour)
                End If
            Next
        End Sub

        Private Sub plotEquivEllipse(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean)
            Dim setColour = Color.Black
            If moved OrElse first Then
                calcEquivEllipse()
                moved = False
            End If
            If selected Then
                Dim colourName = params("ELLIPSE_COLOUR")
                If Not ReferenceEquals(colourName, Nothing) Then
                    Dim ellipseColour = ligplus.Params.getColour(colourName)
                    If Not ellipseColour.IsEmpty Then
                        setColour = ellipseColour
                    End If
                End If
            Else
                Dim inactiveName = params("INACTIVE_COLOUR")
                Dim inactiveColour = ligplus.Params.getColour(inactiveName)
                setColour = inactiveColour
            End If
            Dim width = 0.0F
            Dim numberString = params("ELLIPSE_WIDTH")
            If Not ReferenceEquals(numberString, Nothing) Then
                width = Single.Parse(numberString)
            Else
                width = 0.1F
            End If
            Dim plotWidth = scale.ScaleFactor * width
            calcEllipseCoords(scale)
            If paintType = 0 Then
                '  graphics.Color = setColour
                ' Dim graphics2D As Graphics2D = CType(graphics, Graphics2D)
                Dim lineStroke As New Pen(Color.Black, plotWidth)
                '  graphics2D.Stroke = lineStroke
                Dim ellipseShape As New EllipseShape(plotEllipseDiameter(0), plotEllipseDiameter(1), New PointF(plotEllipseCoords(0), plotEllipseCoords(1)))
                Dim path = ellipseShape.GetPolygonPath

                Call GeomTransform.Rotate(path.xpoints, path.ypoints, theta)

                ' graphics.rotate(Trigonometric.ToRadians(theta), psEllipseCoords(0), psEllipseCoords(1))
                '  graphics.drawOval(plotEllipseCoords(0), plotEllipseCoords(1), plotEllipseDiameter(0), plotEllipseDiameter(1))
                ' graphics.rotate(Trigonometric.ToRadians(-theta), psEllipseCoords(0), psEllipseCoords(1))
                Call graphics.DrawPolygon(lineStroke, path.AsEnumerable.ToArray)
            Else
                psFile.psEllipse(psEllipseCentre(0), psEllipseCentre(1), plotEllipseDiameter(0) / 2.0F, plotEllipseDiameter(1) / 2.0F, setColour, plotWidth, theta)
            End If
        End Sub

        Private Sub plotHgroup(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, plotMode As Integer)
            Dim setColour As Color = Nothing
            Dim bgColour = ligplus.Params.getBackgroundColour(params)
            If paintType = 0 Then
                ' graphics.Color = bgColour
            End If
            If selected Then
                visible = True
            Else
                Dim onOffString = params("INACTIVE_PLOTS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                    visible = False
                Else
                    visible = True
                End If
            End If
            If visible AndAlso nspokesSet > 0 Then
                Dim colourName As String = Nothing
                If plotMode = 0 Then
                    colourName = params("HYDROPHOBIC_COLOUR")
                Else
                    colourName = params("UNDERLAY_COLOUR")
                End If
                If Molecule.MoleculeType = 3 AndAlso Molecule.InterFace = 2 Then
                    colourName = params("HYDROPHOBIC2_COLOUR")
                End If
                If Not antibodyLoopIDField.Equals("") Then
                    Dim name = "ABODY_" & antibodyLoopIDField & "_COLOUR"
                    If params.ContainsKey(name) Then
                        colourName = params(name)
                    End If
                ElseIf isAntigen Then
                    Dim name = "ABODY_ANTIGEN_COLOUR"
                    If params.ContainsKey(name) Then
                        colourName = params(name)
                    End If
                End If
                If Molecule IsNot Nothing Then
                    Dim moleculeID = Molecule.MoleculeID
                    If Not ReferenceEquals(moleculeID, Nothing) Then
                        Dim name = moleculeID & "_RESIDUE_COLOUR"
                        If params.ContainsKey(name) Then
                            colourName = params(name)
                        End If
                    End If
                End If
                If ReferenceEquals(colourName, Nothing) Then
                    colourName = "BLACK"
                End If
                Dim spokeColour = ligplus.Params.getColour(colourName)
                If selected Then
                    setColour = spokeColour
                Else
                    Dim inactiveName = params("INACTIVE_COLOUR")
                    Dim inactiveColour = ligplus.Params.getColour(inactiveName)
                    setColour = inactiveColour
                End If
                plotSpokes(paintType, graphics, psFile, scale, setColour, plotMode)
                identifyArcs()
                plotArcs(paintType, graphics, psFile, scale, setColour, plotMode)
            End If
        End Sub

        Public Overridable Sub plotSpokes(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, setColour As Color, plotMode As Integer)
            ' Dim graphics2D As Graphics2D = Nothing
            If paintType = 0 Then
                ' graphics.Color = setColour
            End If
            If visible Then
                Dim width = 0.0F
                Dim numberString = params("HYDROPHOBIC_WIDTH")
                If Molecule.MoleculeType = 3 AndAlso Molecule.InterFace = 2 Then
                    numberString = params("HYDROPHOBIC2_WIDTH")
                End If
                If Not ReferenceEquals(numberString, Nothing) Then
                    width = Single.Parse(numberString)
                Else
                    width = 0.05F
                End If
                If plotMode = 1 Then
                    width = 2.0F * width
                End If
                Dim plotWidth = scale.ScaleFactor * width
                If paintType = 0 Then
                    ' graphics2D = CType(graphics, Graphics2D)
                End If
                Dim stroke As New Pen(Color.Black, plotWidth)
                If paintType = 0 Then
                    '  graphics2D.Stroke = stroke
                End If
                For i = 0 To nspokes - 1
                    If spoke(i) Then
                        Dim contactAngle = i * 12 / 57.295779513082323R
                        Dim innerRadius = radius * 1.0F
                        Dim outerRadius = radius * 1.4F
                        If plotMode = 1 Then
                            outerRadius *= 1.025F
                        End If
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

        Public Overridable Sub removeEquivalences(pdbId As Integer)
            Dim removeEntry = -1
            Dim i = 0
            While i < equivalentResiduesList.Count AndAlso removeEntry = -1
                Dim equivalence = getResidueEquivalence(i)
                Dim equivPDB = CType(equivalence(0), PDBEntry)
                If equivPDB.PDBId = pdbId Then
                    removeEntry = i
                End If

                i += 1
            End While
            If removeEntry > -1 Then
                equivalentResiduesList.RemoveAt(removeEntry)
            End If
        End Sub

        Public Overridable Sub rotateResidue(pivotX As Single, pivotY As Single, matrix As Double()())
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.rotateAtom(pivotX, pivotY, matrix)
            Next
            If residueLabelField IsNot Nothing Then
                residueLabelField.rotateTextItem(pivotX, pivotY, matrix)
            End If
            moved = True
        End Sub

        Public Overridable Sub restoreCoords(storePos As Integer)
            Dim i As Integer
            For i = 0 To 1
                ellipseCentre(i) = saveEllipseCentre(storePos)(i)
                ellipseRadius(i) = saveEllipseRadius(storePos)(i)
            Next
            theta = saveTheta(storePos)
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.restoreCoords(storePos)
            Next
            updateMaxMinCoords()
            If residueLabelField IsNot Nothing Then
                residueLabelField.restoreCoords(storePos)
            End If
        End Sub

        Public Overridable Sub saveCoords(storePos As Integer)
            calcEquivEllipse()
            Dim i As Integer
            For i = 0 To 1
                saveEllipseCentre(storePos)(i) = ellipseCentre(i)
                saveEllipseRadius(storePos)(i) = ellipseRadius(i)
            Next
            saveTheta(storePos) = theta
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                atom.saveCoords(storePos)
            Next
            updateMaxMinCoords()
            If residueLabelField IsNot Nothing Then
                residueLabelField.saveCoords(storePos)
            End If
        End Sub


        Public Overridable Sub setAntigen()
            isAntigen = True
        End Sub




        Public Overridable Sub setMoved()
            moved = True
        End Sub

        Public Overridable Sub setPostTER()
            postTERField = True
        End Sub

        Public Overridable WriteOnly Property EntityDesc As String
            Set(value As String)
                entityDescField = value
            End Set
        End Property


        Public Overridable Sub setResidueSizeType()
            If moleculeType = 3 Then
                residueSizeType = "HPHOBIC_RADIUS"
                If program = RunExe.DIMPLOT Then
                    If Molecule.InterFace = 1 Then
                        residueSizeType = "IFACE_HPHOBIC1_RADIUS"
                    Else
                        residueSizeType = "IFACE_HPHOBIC2_RADIUS"
                    End If
                End If
                Dim numberString = params(residueSizeType)
                radius = Single.Parse(numberString)
            End If
        End Sub


        Public Overridable WriteOnly Property WantedAtoms As Boolean
            Set(value As Boolean)
                For i = 0 To atomListField.Count - 1
                    Dim atom As Atom = atomListField(i)
                    atom.Wanted = value
                Next
            End Set
        End Property

        Public Overridable Sub storeResidueEquivalence(otherPDB As PDBEntry, otherResidue As Residue)
            Dim haveEquiv = False
            Dim i = 0

            While i < equivalentResiduesList.Count AndAlso Not haveEquiv
                Dim equivalence = getResidueEquivalence(i)
                Dim equivPDB = CType(equivalence(0), PDBEntry)
                Dim equivResidue = CType(equivalence(1), Residue)
                If equivPDB Is otherPDB AndAlso equivResidue Is otherResidue Then
                    haveEquiv = True
                End If

                i += 1
            End While
            If Not haveEquiv Then
                Dim equivalence = New Object(1) {}
                equivalence(0) = otherPDB
                equivalence(1) = otherResidue
                equivalentResiduesList.Add(equivalence)
            End If
        End Sub

        Public Overrides Function ToString() As String
            Dim [string] = resNameField & " " & resNumField
            If chainField <> " "c Then
                [string] = [string] & "(" & chainField.ToString() & ")"
            End If
            Return [string]
        End Function

        Public Overridable Function updateMaxMinCoords() As Integer
            nCoords = 0
            If Molecule.MoleculeType = 3 Then
                Dim numberString = params(residueSizeType)
                radius = Single.Parse(numberString)
                Dim spokeRadius = radius * 1.4F
                If atomListField.Count > 0 Then
                    Dim atom As Atom = atomListField(0)
                    For icoord = 0 To 1
                        coordsMin(icoord) = atom.getCoord(icoord) - spokeRadius
                        coordsMax(icoord) = atom.getCoord(icoord) + spokeRadius
                        coordsAccum(icoord) = atom.getCoord(icoord)
                        coordsCentre(icoord) = atom.getCoord(icoord)
                    Next
                    nCoords = 1
                End If
            Else
                For i = 0 To atomListField.Count - 1
                    Dim atom As Atom = atomListField(i)
                    nCoords += 1
                    For icoord = 0 To 1
                        Dim cMin = atom.getCoord(icoord) - atom.Radius
                        Dim cMax = atom.getCoord(icoord) + atom.Radius
                        Dim cAccum = atom.getCoord(icoord)
                        If i = 0 Then
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
                    Dim atomLabel = atom.AtomLabel
                    If atomLabel IsNot Nothing AndAlso Not atomLabel.Text.Equals("") Then
                        Dim textCoords = New Single(3) {}
                        textCoords = atomLabel.MaxMinCoords
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
                    End If
                Next
            End If
            If residueLabelField IsNot Nothing AndAlso Not residueLabelField.Text.Equals("") Then
                Dim textCoords = residueLabelField.MaxMinCoords
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
            End If
            Return nCoords
        End Function

        Public Overridable Sub updateDummyMoleculeCoords(coords As Single())
            For i = 0 To atomListField.Count - 1
                Dim atom As Atom = atomListField(i)
                If i = 0 Then
                    atom.setCoord(0, coords(1))
                    atom.setCoord(1, coords(2))
                ElseIf i = 1 Then
                    atom.setCoord(0, coords(3))
                    atom.setCoord(1, coords(4))
                End If
            Next
        End Sub

        Public Overridable ReadOnly Property FullInsCode As String
            Get
                Return fullInsCodeField
            End Get
        End Property
    End Class

End Namespace
