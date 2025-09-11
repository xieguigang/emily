Imports ligplus.models
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Imaging

Namespace ligplus

    Public Class PlotArea
        Public Const MARGIN As Single = 1.5F

        Public Const BY_RESIDUE As Integer = 0

        Public Const BY_ATOM As Integer = 1

        Public Const BY_PDB As Integer = 2

        Public Const SELECTION As Integer = 3

        Public Const NFONTS As Integer = 7

        Public Shared ReadOnly PREFERRED_FONT_NAME As String() = New String() {"Times New Roman", "Arial", "Helvetica", "Bookman", "Century Schoolbook", "Courier", "Serif", "Georgia"}

        Public Shared ReadOnly PS_FONT_NAME As String() = New String() {"Times-Roman", "Arial", "Helvetica", "Bookman-Light", "NewCenturySchlbk-Roman", "Courier", "Serif", "Georgia"}

        Public Const BOLD As Boolean = True

        Public Const NOT_BOLD As Boolean = False

        Public Shared ReadOnly PIC_SIZE As Integer() = New Integer() {16, 24, 32, 48, 64, 128, 256}

        Public Const LIGPLUS_LOGO_IMAGE As Integer = 0

        Public Const ROTATE_IMAGE As Integer = 1

        Public Const FLIP_IMAGE As Integer = 2

        Public Const SELECT_IMAGE As Integer = 3

        Public Const NIMAGES As Integer = 4

        Public Shared ReadOnly IMAGE As String() = New String() {"ligplus_logo.gif", "rotate.gif", "flip.gif", "greentick.gif"}

        Private Shared fontFamilyField As String = Nothing

        Private Shared currentFontField As String = Nothing

        Private flipAtom As Atom = Nothing

        Private pivotAtom As Atom = Nothing

        Private rotateAtom As Atom = Nothing

        Private flipBond As Bond = Nothing

        Private moveMade As Boolean = False

        Private plotChanged As Boolean = False

        Private splitScreenField As Boolean = True

        Private startScreen As Boolean = True

        Private imageField As Image() = New Image(3) {}

        Private clickClass As Type

        Public Property Width As Single
        Public Property Height As Single

        Private lastAngle As Double

        Private ensemble As Ensemble = Nothing

        Private Shared font As Font() = Nothing

        Private buttonPressed As Integer = 0

        Private lastX As Integer

        Private lastY As Integer

        Private lastMoveBy As Integer = 0

        Private moveBy As Integer = 0

        Private selectedMoleculesListField As List(Of Object) = Nothing


        Private parentFrameField As LigPlusFrame = Nothing

        Private clickObject As Object

        Private clickPDB As PDBEntry

        Private scale As Scale = Nothing

        Private clickName As String

        Private companyName As String = Nothing

        Private dateString As String = Nothing

        Private expires As String = Nothing

        Public Sub New(width As Single, height As Single)
            Me.Width = width
            Me.Height = height
        End Sub

        Friend Overridable WriteOnly Property SplitScreen As Boolean
            Set(value As Boolean)
                splitScreenField = value
                If ensemble IsNot Nothing Then
                    ensemble.SplitShiftOn = value
                End If
            End Set
        End Property

        Private Function getImageSize(radius As Integer) As Integer
            Dim found = False
            Dim picSize = 0
            If radius > 0 Then
                Dim i = 0

                While i < PIC_SIZE.Length AndAlso Not found
                    If 2 * radius < PIC_SIZE(i) Then
                        found = True
                    Else
                        picSize += 1
                    End If

                    i += 1
                End While
                If Not found Then
                    picSize = PIC_SIZE.Length - 1
                End If
            End If
            Dim pictureSize = PIC_SIZE(picSize)
            Return pictureSize
        End Function

        Public Overridable ReadOnly Property SelectedMoleculesList As List(Of Object)
            Get
                Return selectedMoleculesListField
            End Get
        End Property





        Friend Overridable Sub blankImage()
            startScreen = False
            pivotAtom = Nothing
            flipAtom = Nothing
            flipBond = Nothing
            ensemble = Nothing
        End Sub

        Public Shared Property FontFamily As String
            Get
                Return fontFamilyField
            End Get
            Set(value As String)
                fontFamilyField = value
            End Set
        End Property

        Public Shared Property CurrentFont As String
            Get
                Return currentFontField
            End Get
            Set(value As String)
                currentFontField = value
            End Set
        End Property

        Public Shared Function getFont(fontName As String) As Font
            Dim foundFont As Font = Nothing
            Dim iFont = 0

            While iFont < font.Length AndAlso foundFont Is Nothing
                'If font(iFont).Family.contains(fontName) Then
                '    foundFont = font(iFont)
                'End If

                iFont += 1
            End While
            Return foundFont
        End Function

        Public Shared ReadOnly Property PSFontFamily As String
            Get
                Dim lPSFontFamily As String = Nothing
                Dim iFont = 0

                While iFont < 7 AndAlso ReferenceEquals(lPSFontFamily, Nothing)
                    If currentFontField.Contains(PREFERRED_FONT_NAME(iFont)) Then
                        lPSFontFamily = PS_FONT_NAME(iFont)
                    End If

                    iFont += 1
                End While
                If ReferenceEquals(lPSFontFamily, Nothing) Then
                    lPSFontFamily = PS_FONT_NAME(0)
                End If
                Return lPSFontFamily
            End Get
        End Property


        Friend Overridable Sub newImage(ensemble As Ensemble)
            SyncLock Me
                startScreen = False
                Me.ensemble = ensemble
                If ensemble IsNot Nothing Then
                    Dim nCoords As Integer = ensemble.updateMaxMinCoords()
                    Dim coordsMax = ensemble.CoordsMax
                    Dim coordsMin = ensemble.CoordsMin
                    Dim splitWidth = ensemble.getSplitWidth(splitScreenField, 1.5F)
                    Dim maxMinCoords = ensemble.MaxMinCoords
                    coordsMin(0) = maxMinCoords(0)
                    coordsMin(1) = maxMinCoords(2)
                    coordsMax(0) = maxMinCoords(1)
                    coordsMax(1) = maxMinCoords(3)
                    scale.calcScale(coordsMin, coordsMax, 1.5F)
                End If
                ' repaint();
            End SyncLock
        End Sub

        Public Overridable WriteOnly Property ParentFrame As LigPlusFrame
            Set(value As LigPlusFrame)
                parentFrameField = value
            End Set
        End Property

        Friend Overridable WriteOnly Property PreferredFont As String
            Set(value As String)
                Dim newFontFamily As String = Nothing
                Dim newCurrentFont As String = Nothing
                ' GraphicsEnvironment gEnv = GraphicsEnvironment.LocalGraphicsEnvironment;
                Dim font As Font() '= gEnv.AllFonts;
                Dim iFont As Integer
                iFont = 0

                While iFont < font.Length AndAlso ReferenceEquals(newFontFamily, Nothing)
                    'If font(iFont).Family.contains(value) Then
                    '    newFontFamily = font(iFont).Family
                    'End If

                    iFont += 1
                End While
                For iFont = 0 To 6
                    If PREFERRED_FONT_NAME(iFont).Contains(value) Then
                        newCurrentFont = PREFERRED_FONT_NAME(iFont)
                    End If
                Next
                If Not ReferenceEquals(newFontFamily, Nothing) AndAlso Not ReferenceEquals(newCurrentFont, Nothing) Then
                    fontFamilyField = newFontFamily
                    currentFontField = newCurrentFont
                End If
            End Set
        End Property

        Private Sub showPivot(g As IGraphics)
            Dim x As Integer = pivotAtom.getPlotCoords(0)
            Dim y As Integer = pivotAtom.getPlotCoords(1)
            Dim radius = pivotAtom.getPlotRadius(scale)
            Dim pictureSize = getImageSize(radius)
            Dim plotX = x + radius - CInt(pictureSize / 2.0R + 0.5R)
            Dim plotY = y + radius - CInt(pictureSize / 2.0R + 0.5R)
            g.DrawImage(imageField(1), plotX, plotY, pictureSize, pictureSize)
        End Sub

        Private Sub showflipBond(g As IGraphics)
            Dim atom1 = flipBond.getAtom(0)
            Dim atom2 = flipBond.getAtom(1)
            Dim x1 As Integer = atom1.getPlotCoords(0)
            Dim y1 As Integer = atom1.getPlotCoords(1)
            Dim x2 As Integer = atom2.getPlotCoords(0)
            Dim y2 As Integer = atom2.getPlotCoords(1)
            Dim radius1 = atom1.getPlotRadius(scale)
            Dim radius2 = atom1.getPlotRadius(scale)
            Dim pictureSize = getImageSize(radius1)
            Dim plotX = CInt((x1 + radius1 + x2 + radius2) / 2.0R) - CInt(pictureSize / 2.0R + 0.5R)
            Dim plotY = CInt((y1 + radius1 + y2 + radius2) / 2.0R) - CInt(pictureSize / 2.0R + 0.5R)
            g.DrawImage(imageField(2), plotX, plotY, pictureSize, pictureSize)
        End Sub

        Private Sub showTick(g As IGraphics, molecule As Molecule)
            Dim x = 0, y = 0
            Dim radius = 0
            Dim resList As List(Of Residue) = molecule.ResidueList
            For i = 0 To resList.Count - 1
                Dim residue = resList(i)
                If molecule.MoleculeType = 3 Then
                    Dim coords = residue.HGroupPlotCoords
                    x = CInt(coords(0))
                    y = CInt(coords(1))
                    radius = CInt(residue.PlotRadius)
                    Dim pictureSize = getImageSize(radius)
                    Dim plotX = x + radius - CInt(pictureSize / 2.0R + 0.5R)
                    Dim plotY = y + radius - CInt(pictureSize / 2.0R + 0.5R)
                    g.DrawImage(imageField(3), plotX, plotY, pictureSize, pictureSize)
                Else
                    Dim atomList As List(Of Atom) = residue.AtomList
                    For j = 0 To atomList.Count - 1
                        Dim atom = atomList(j)
                        x = CInt(atom.getPlotCoords(0))
                        y = CInt(atom.getPlotCoords(1))
                        radius = atom.getPlotRadius(scale)
                        Dim pictureSize = getImageSize(radius)
                        Dim plotX = x + radius - CInt(pictureSize / 2.0R + 0.5R)
                        Dim plotY = y + radius - CInt(pictureSize / 2.0R + 0.5R)
                        g.DrawImage(imageField(3), plotX, plotY, pictureSize, pictureSize)
                    Next
                End If
            Next
        End Sub
    End Class

End Namespace
