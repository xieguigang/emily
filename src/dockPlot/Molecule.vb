Imports System.Drawing
Imports Microsoft.VisualBasic.Imaging

Namespace ligplus

    Public Class Molecule
        Public Const MOL_UNKNOWN As Integer = 0

        Public Const MOL_PROTEIN As Integer = 1

        Public Const MOL_CALPHA_ONLY As Integer = 2

        Public Const MOL_DNA_RNA As Integer = 3

        Public Const MOL_LIGAND As Integer = 4

        Public Const MOL_METAL As Integer = 5

        Public Const MOL_WATER As Integer = 6

        Public Const DUMMY_MOLECULE As Integer = 7

        Public Const NMOLECULE_TYPES As Integer = 8

        Public Shared ReadOnly MOLECULE_TYPE As String() = New String() {"Unknown", "protein chain", "CA-only chain", "DNA/RNA chain", "ligand", "metal", "water molecule", "dummy"}

        Public Const PLOT_UNKNOWN As Integer = 0

        Public Const PLOT_LIGAND As Integer = 1

        Public Const PLOT_HGROUP As Integer = 2

        Public Const PLOT_HYDROPHOBIC As Integer = 3

        Public Const PLOT_WATER As Integer = 4

        Public Const PLOT_SIMPLE_HGROUP As Integer = 5

        Public Const PLOT_SIMPLE As Integer = 6

        Private deletedField As Boolean = False

        Private wantedField As Boolean = False

        Private chainField As Char = " "c

        Private coordsAccum As Single() = New Single(2) {}

        Private coordsCentre As Single() = New Single(2) {}

        Private coordsMin As Single() = New Single(2) {}

        Private coordsMax As Single() = New Single(2) {}

        Private atomRecCountField As Integer = 0

        Private hetatmRecCountField As Integer = 0

        Private interFaceField As Integer = 0

        Private moleculeTypeField As Integer = 0

        Private nAtomsField As Integer = 0

        Private nCoords As Integer = 0

        Private nResiduesField As Integer = 0

        Private nTextItems As Integer = 0

        Private residueTypeCount As Integer()

        Private moleculeIDField As String = Nothing

        Private params As Properties

        Private fullChainField As String = Nothing

        Private pdb As PDBEntry

        Private bondList As List(Of Object)

        Private residueListField As List(Of Residue)

        Private textItemList As List(Of Object)

        Private firstResidueField As Residue = Nothing

        Private lastResidue As Residue = Nothing

        Public Sub New(chain As Char, fullChain As String, pdb As PDBEntry, moleculeID As String, params As Properties)
            chainField = chain
            fullChainField = fullChain
            wantedField = False
            Me.pdb = pdb
            moleculeIDField = moleculeID
            Me.params = params
            init()
        End Sub

        Public Overridable Sub restoreCoords(storePos As Integer)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.restoreCoords(storePos)
            Next
        End Sub

        Public Overridable Sub saveCoords(storePos As Integer)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.saveCoords(storePos)
            Next
        End Sub

        Public Overridable Property DummyCoords As Single()
            Set(value As Single())
                If residueListField.Count > 0 Then
                    Dim residue As Residue = residueListField(0)
                    residue.DummyCoords = value
                End If
            End Set
            Get
                Dim lDummyCoords = New Single(4) {}
                lDummyCoords(0) = 0.0F
                If residueListField.Count > 0 Then
                    Dim residue As Residue = residueListField(0)
                    lDummyCoords = residue.DummyCoords
                End If
                Return lDummyCoords
            End Get
        End Property

        Public Overridable Property Wanted As Boolean
            Set(value As Boolean)
                wantedField = value
            End Set
            Get
                Return wantedField
            End Get
        End Property

        Public Overridable Property Chain As Char
            Set(value As Char)
                chainField = value
            End Set
            Get
                Return chainField
            End Get
        End Property

        Public Overridable Property [InterFace] As Integer
            Set(value As Integer)
                interFaceField = value
            End Set
            Get
                Return interFaceField
            End Get
        End Property

        Private Sub init()
            Dim i As Integer
            For i = 0 To 2
                coordsMin(i) = 0.0F
                coordsMax(i) = 0.0F
                coordsCentre(i) = 0.0F
            Next
            residueTypeCount = New Integer(6) {}
            For i = 0 To 6
                residueTypeCount(i) = 0
            Next
            bondList = New List(Of Object)()
            residueListField = New List(Of Residue)()
            textItemList = New List(Of Object)()
            nTextItems = 0
            nResiduesField = 0
            nAtomsField = 0
        End Sub

        Public Overridable Sub addBond(bond As Bond)
            bondList.Add(bond.Object)
        End Sub

        Public Overridable Sub addResidue(residue As Residue)
            residueListField.Add(residue)
            residue.Molecule = Me
            residue.setResidueSizeType()
            Dim residueType = residue.ResidueType
            If residueType > -1 AndAlso residueType < 7 Then
                residueTypeCount(residueType) = residueTypeCount(residueType) + 1
            End If
            nAtomsField += residue.Natoms
            For i = 0 To 2
                If nResiduesField = 0 Then
                    coordsAccum(i) = residue.getCoordsAccum(i)
                    coordsCentre(i) = residue.getCoordsCentre(i)
                    coordsMin(i) = residue.getCoordsMin(i)
                    coordsMax(i) = residue.getCoordsMax(i)
                    firstResidueField = residue
                Else
                    coordsAccum(i) = coordsAccum(i) + residue.getCoordsAccum(i)
                    coordsCentre(i) = coordsAccum(i) / nAtomsField
                    If residue.getCoordsMax(i) > coordsMax(i) Then
                        coordsMax(i) = residue.getCoordsMax(i)
                    End If
                    If residue.getCoordsMin(i) < coordsMin(i) Then
                        coordsMin(i) = residue.getCoordsMin(i)
                    End If
                End If
                lastResidue = residue
            Next
            nResiduesField += 1
        End Sub

        Public Overridable Sub addTextItem(textItem As TextItem)
            textItemList.Add(textItem)
            nTextItems += 1
        End Sub

        Public Overridable Sub applyDimShifts(cutPointX As Single, cutPointY As Single, dimShiftX As Single, dimShiftY As Single)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.applyDimShifts(cutPointX, cutPointY, dimShiftX, dimShiftY)
            Next
            updateMaxMinCoords()
        End Sub

        Private Function calcFlipTransformation(flipBond As Bond) As Double()()
            Dim atom1 = flipBond.getAtom(0)
            Dim atom2 = flipBond.getAtom(1)
            Dim rotAngle = Angle.calcAngle(atom1.getCoord(0), atom1.getCoord(1), atom2.getCoord(0), atom2.getCoord(1))
            Dim matrix = Angle.calcRotMatrix(rotAngle)
            Return matrix
        End Function

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim clickObject As Object = Nothing
            If x < coordsMin(0) OrElse x > coordsMax(0) OrElse y < coordsMin(1) OrElse y > coordsMax(1) Then
                Return clickObject
            End If
            Dim i = 0

            While i < residueListField.Count AndAlso clickObject Is Nothing
                Dim residue As Residue = residueListField(i)
                clickObject = residue.clickCheck(x, y)
                i += 1
            End While
            Return clickObject
        End Function

        Public Overridable Sub deleteMolecule(b As Boolean)
            deletedField = True
        End Sub

        Private Sub flipAtoms(flipBond As Bond, matrix As Double()())
            Dim shiftX = flipBond.getAtom(0).getCoord(0)
            Dim shiftY = flipBond.getAtom(0).getCoord(1)
            Dim inverseMatrix = Angle.getInverseMatrix(matrix)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.flipAtoms(shiftX, shiftY, matrix, inverseMatrix)
            Next
        End Sub

        Public Overridable ReadOnly Property MoleculeColour As Color
            Get
                Dim bondTypeName As String
                Dim colour As Color = Nothing
                If [InterFace] = 0 Then
                    bondTypeName = "LIGBOND"
                ElseIf [InterFace] = 1 Then
                    bondTypeName = "NLIGBOND"
                ElseIf [InterFace] = 2 Then
                    bondTypeName = "NLIGBOND2"
                Else
                    bondTypeName = "NONE"
                End If
                Dim colourName = params(bondTypeName & "_COLOUR")
                If Not ReferenceEquals(colourName, Nothing) Then
                    colour = ligplus.Params.getColour(colourName)
                Else
                    colour = Color.BLACK
                End If
                Dim antibodyLoopID = firstResidueField.AntibodyLoopID
                Dim isAntigen = firstResidueField.Antigen
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
                    End If
                End If
                If Not ReferenceEquals(moleculeIDField, Nothing) Then
                    Dim name = moleculeIDField & "_RESIDUE_COLOUR"
                    If params.ContainsKey(name) Then
                        colourName = params(name)
                        If Not ReferenceEquals(colourName, Nothing) Then
                            colour = ligplus.Params.getColour(colourName)
                        End If
                    End If
                End If
                Return colour
            End Get
        End Property

        Public Overridable ReadOnly Property AtomRecCount As Integer
            Get
                Return atomRecCountField
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


        Public Overridable ReadOnly Property FirstResidue As Residue
            Get
                Return firstResidueField
            End Get
        End Property

        Public Overridable ReadOnly Property HetatmRecCount As Integer
            Get
                Return hetatmRecCountField
            End Get
        End Property


        Public Overridable Function getLigandDescription(typeSet As Boolean) As String
            Dim nRes = 0
            Dim desc = ""
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                If residue.HetGroup AndAlso residue.hetGroupField IsNot Nothing Then
                    Dim hetGroupDescription As String = residue.hetGroupField.HetDescription
                    If hetGroupDescription.Length > 0 AndAlso typeSet Then
                        hetGroupDescription = TextItem.typesetText(hetGroupDescription)
                    End If
                    If nResiduesField = 1 Then
                        desc = hetGroupDescription
                    Else
                        If nRes > 0 Then
                            desc = desc & ", "
                        End If
                        desc = desc & residue.FullResName & "=" & hetGroupDescription
                    End If
                    nRes += 1
                End If
            Next
            Return desc
        End Function

        Public Overridable ReadOnly Property LigandSequence As String
            Get
                Dim nRes = 0
                Dim sequence = ""
                For i = 0 To residueListField.Count - 1
                    Dim residue As Residue = residueListField(i)
                    If nRes > 0 Then
                        sequence = sequence & "-"
                    End If
                    sequence = sequence & residue.FullResName
                    nRes += 1
                Next
                Return sequence
            End Get
        End Property

        Public Overridable ReadOnly Property MoleculeID As String
            Get
                Return moleculeIDField
            End Get
        End Property

        Public Overridable Property MoleculeType As Integer
            Get
                Return moleculeTypeField
            End Get
            Set(value As Integer)
                moleculeTypeField = value
            End Set
        End Property

        Public Overridable ReadOnly Property NAtoms As Integer
            Get
                Return nAtomsField
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property

        Public Overridable ReadOnly Property ResidueList As List(Of Residue)
            Get
                Return residueListField
            End Get
        End Property

        Public Overridable ReadOnly Property ResidueRange As String
            Get
                Dim range = ""
                Dim ch1 = ""
                Dim ch2 = ""
                Dim chain1 = firstResidueField.FullChain
                Dim chain2 = lastResidue.FullChain
                If chain1.Equals(chain2) AndAlso Not chain2.Equals(" ") Then
                    ch2 = "(" & chain2 & ")"
                ElseIf chain1.Equals(chain2) Then
                    ch1 = "(" & chain1 & ")"
                    ch2 = "(" & chain2 & ")"
                End If
                If nResiduesField = 1 Then
                    If Not chain1.Equals(" ") Then
                        ch1 = "(" & chain1 & ")"
                    Else
                        ch1 = ""
                    End If
                    range = firstResidueField.FullResNum.ToString() & ch1
                Else
                    range = firstResidueField.FullResNum.ToString() & ch1 & "-" & lastResidue.FullResNum.ToString() & ch2
                End If
                Dim tmp = ""
                For i = 0 To range.Length - 1
                    If range(i) <> " "c Then
                        tmp = tmp & range(i).ToString()
                    End If
                Next
                range = tmp
                Return range
            End Get
        End Property

        Public Overridable Function getResidueTypeCount(i As Integer) As Integer
            Return residueTypeCount(i)
        End Function


        Public Overridable ReadOnly Property FullChain As String
            Get
                Return fullChainField
            End Get
        End Property

        Public Overridable ReadOnly Property NResidues As Integer
            Get
                Return nResiduesField
            End Get
        End Property


        Private Function initFlipAtoms(flipBond As Bond, flipAtom As Atom) As List(Of Bond)
            Dim nBonds = 0
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.WantedAtoms = False
            Next
            Dim bondStack As List(Of Bond) = New List(Of Bond)()
            For j = 0 To bondList.Count - 1
                Dim checked As Boolean
                Dim bond As Bond = bondList(j)
                If Not bond.Equals(flipBond) AndAlso bond.hasAtom(flipAtom) Then
                    bondStack.Add(bond)
                    nBonds += 1
                    bond.getAtom(0).Wanted = True
                    bond.getAtom(1).Wanted = True
                    checked = True
                Else
                    checked = False
                End If
                bond.Checked = checked
            Next
            Return bondStack
        End Function

        Public Overridable ReadOnly Property Deleted As Boolean
            Get
                Return deletedField
            End Get
        End Property

        Public Overridable Sub moveMolecule(realMoveX As Single, realMoveY As Single)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.moveResidue(realMoveX, realMoveY)
            Next
        End Sub

        Friend Overridable Sub paintMolecule(paintType As Integer, g As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean, nonEquivs As Boolean)
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.paintResidue(paintType, g, psFile, scale, selected, nonEquivs)
            Next
        End Sub

        Friend Overridable Sub performFlip(flipBond As Bond, flipAtom As Atom)
            Dim bondStack = initFlipAtoms(flipBond, flipAtom)
            Dim nWanted = processBondStack(bondStack, flipBond, flipAtom)
            If nWanted > 0 Then
                Dim matrix = calcFlipTransformation(flipBond)
                flipAtoms(flipBond, matrix)
            End If
        End Sub

        Private Function processBondStack(bondStack As List(Of Bond), flipBond As Bond, flipAtom As Atom) As Integer
            Dim iStack = 0
            While iStack < bondStack.Count
                Dim currentBond = bondStack(iStack)
                Dim atom1 = currentBond.getAtom(0)
                Dim atom2 = currentBond.getAtom(1)
                For j = 0 To bondList.Count - 1
                    Dim bond As Bond = bondList(j)
                    If Not bond.Equals(flipBond) AndAlso Not bond.Checked AndAlso (bond.hasAtom(atom1) OrElse bond.hasAtom(atom2)) Then
                        bondStack.Add(bond)
                        bond.getAtom(0).Wanted = True
                        bond.getAtom(1).Wanted = True
                        bond.Checked = True
                    End If
                Next
                iStack += 1
            End While
            flipBond.getAtom(0).Wanted = False
            flipBond.getAtom(1).Wanted = False
            Dim nWanted = 0
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                nWanted += residue.countWantedAtoms()
            Next
            Return nWanted
        End Function

        Public Overridable Sub rotateMolecule(pivotX As Single, pivotY As Single, matrix As Double()())
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                residue.rotateResidue(pivotX, pivotY, matrix)
            Next
        End Sub


        Public Overrides Function ToString() As String
            Dim [string] As String
            If firstResidueField IsNot Nothing AndAlso lastResidue IsNot Nothing Then
                [string] = MOLECULE_TYPE(moleculeTypeField) & firstResidueField.ToString() & "-" & lastResidue.ToString()
            Else
                [string] = MOLECULE_TYPE(moleculeTypeField)
            End If
            Return [string]
        End Function

        Public Overridable Sub updateAtomHetatmCount(atHet As Char)
            If atHet = "A"c Then
                atomRecCountField += 1
            Else
                hetatmRecCountField += 1
            End If
        End Sub

        Public Overridable Sub updateCounts(nextMolecule As Molecule)
            Dim i As Integer
            For i = 0 To 6
                residueTypeCount(i) = residueTypeCount(i) + nextMolecule.getResidueTypeCount(i)
            Next
            nAtomsField += nextMolecule.NAtoms
            nResiduesField += nextMolecule.NResidues
            atomRecCountField += nextMolecule.AtomRecCount
            hetatmRecCountField += nextMolecule.HetatmRecCount
            For i = 0 To 2
                coordsAccum(i) = coordsAccum(i) + nextMolecule.getCoordsAccum(i)
                coordsCentre(i) = coordsAccum(i) / nAtomsField
                If nextMolecule.getCoordsMax(i) > coordsMax(i) Then
                    coordsMax(i) = nextMolecule.getCoordsMax(i)
                End If
                If nextMolecule.getCoordsMin(i) < coordsMin(i) Then
                    coordsMin(i) = nextMolecule.getCoordsMin(i)
                End If
            Next
            Dim resList As List(Of Residue) = nextMolecule.ResidueList
            For j = 0 To resList.Count - 1
                Dim residue = resList(j)
                residueListField.Add(residue)
                lastResidue = residue
            Next
        End Sub

        Public Overridable Function updateMaxMinCoords() As Integer
            nCoords = 0
            For i = 0 To residueListField.Count - 1
                Dim residue As Residue = residueListField(i)
                Dim nc As Integer = residue.updateMaxMinCoords()
                nCoords += nc
                For icoord = 0 To 1
                    Dim cMin = residue.getCoordsMin(icoord)
                    Dim cMax = residue.getCoordsMax(icoord)
                    Dim cAccum = residue.getCoordsAccum(icoord)
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
            Next
            Return nCoords
        End Function

        Public Overridable Sub updateDummyMoleculeCoords(coords As Single())
            If residueListField.Count > 0 Then
                Dim residue As Residue = residueListField(0)
                residue.updateDummyMoleculeCoords(coords)
            End If
            coordsMin(0) = coords(1)
            coordsMin(1) = coords(2)
            coordsMax(0) = coords(3)
            coordsMax(1) = coords(4)
        End Sub
    End Class

End Namespace
