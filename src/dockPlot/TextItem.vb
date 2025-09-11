Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Text.Parser

Namespace ligplus

    Public Class TextItem

        Private _Visible As Boolean
        Public Const BOLD As Boolean = True

        Public Const NOT_BOLD As Boolean = False

        Public Const EMPTY As Integer = -1

        Public Const PLOT_TITLE As Integer = 0

        Public Const LIGAND_RESIDUE_NAME As Integer = 1

        Public Const NONLIGAND_RESIDUE_NAME As Integer = 2

        Public Const NONLIGAND_RESIDUE_NAME2 As Integer = 3

        Public Const WATER_NAME As Integer = 4

        Public Const HYDROPHOBIC_NAME As Integer = 5

        Public Const HYDROPHOBIC_NAME2 As Integer = 6

        Public Const LIGAND_ATOM_NAME As Integer = 7

        Public Const NONLIGAND_ATOM_NAME As Integer = 8

        Public Const NONLIGAND_ATOM_NAME2 As Integer = 9

        Public Const HBOND_LENGTH As Integer = 10

        Public Const GROUP_HEADING As Integer = 11

        Public Const PLOT_LABEL As Integer = 12

        Public Const PLOT_LIST_HEADING As Integer = 13

        Public Const ABODY_LOOP_LABEL As Integer = 14

        Public Const ABODY_LOOP_LIST_HEADING As Integer = 15

        Public Const HBTEXT_DIM As Integer = 16

        Public Const IFACE_ATMNAME1 As Integer = 17

        Public Const IFACE_ATMNAME2 As Integer = 18

        Public Const IFACE_HYDROPHNAME1 As Integer = 19

        Public Const IFACE_HYDROPHNAME2 As Integer = 20

        Public Const IFACE_RESNAME1 As Integer = 21

        Public Const IFACE_RESNAME2 As Integer = 22

        Public Const TITLE_DIM As Integer = 23

        Public Const WATERNAME_DIM As Integer = 24

        Public Shared ReadOnly TYPE_NAME As String() = New String() {"TITLE", "LIGRESNAME", "NLIGRESNAME", "NLIGRESNAME2", "WATERNAME", "HYDROPHNAME", "HYDROPHNAME2", "LIGATMNAME", "NLIGATMNAME", "NLIGATMNAME2", "HBTEXT", "", "PLOT_LABEL", "PLOT_LIST_HEADING", "ABODY_LOOP_LABEL", "ABODY_LOOP_LIST_HEADING", "HBTEXT", "NLIGATMNAME", "NLIGATMNAME2", "HYDROPHNAME", "HYDROPHNAME2", "LIGRESNAME", "NLIGRESNAME", "TITLE", "WATERNAME"}

        Public Shared ReadOnly SIZE_TYPE_NAME As String() = New String() {"TITLE", "LIGRESNAME", "NLIGRESNAME", "NLIGRESNAME2", "WATERNAME", "HYDROPHNAME", "HYDROPHNAME2", "LIGATMNAME", "NLIGATMNAME", "NLIGATMNAME2", "HBTEXT", "", "PLOT_LABEL", "PLOT_LIST_HEADING", "ABODY_LOOP_LABEL", "ABODY_LOOP_LIST_HEADING", "HBTEXT_DIM", "IFACE_ATMNAME1", "IFACE_ATMNAME2", "IFACE_HYDROPHNAME1", "IFACE_HYDROPHNAME2", "IFACE_RESNAME1", "IFACE_RESNAME2", "TITLE_DIM", "WATERNAME_DIM"}

        Private Const CHAR_ASPECT_RATIO As Single = 0.7F

        Private isAntigen As Boolean = False

        Private isResName As Boolean = False

        Private visibleField As Boolean

        Private colour As Color = Color.Red

        Private textItemType As Integer

        Private fontSize As Single = 0.0F

        Private plotCoords As Single() = New Single(1) {}

        Private psCoords As Single() = New Single(1) {}

        Private realHeight As Single = 0.0F

        Private realWidth As Single = 0.0F

        Private size As Single = 0.0F

        Private textHeightField As Single

        Private textWidthField As Single

        Private coordsField As Single() = New Single(1) {}

        Private saveCoords_Conflict As Single()() = RectangularArray.Matrix(Of Single)(10, 2)

        Private font As Font

        Private pdb As PDBEntry = Nothing

        Private params As Properties

        Private fontFamily As String = Nothing

        Private showText As String = ""

        Private textField As String = ""

        Private saveText As String() = New String(9) {}

        Private sizeTypeName As String = Nothing

        Private typeName As String = Nothing

        Private antibodyLoopIDField As String = ""

        Private moleculeIDField As String = Nothing

        Public Sub New()
            visibleField = True
        End Sub

        Public Sub New(pdb As PDBEntry, text As String, type As Integer, x As Single, y As Single)
            Me.New()
            Me.pdb = pdb
            params = pdb.Params
            textField = text
            showText = text
            textItemType = type
            If textItemType > -1 AndAlso textItemType < TYPE_NAME.Length Then
                typeName = TYPE_NAME(textItemType)
                sizeTypeName = SIZE_TYPE_NAME(textItemType)
            End If
            isResName = False
            If textItemType = 1 OrElse textItemType = 2 OrElse textItemType = 3 OrElse textItemType = 4 OrElse textItemType = 5 OrElse textItemType = 6 OrElse textItemType = 19 OrElse textItemType = 20 OrElse textItemType = 21 OrElse textItemType = 22 OrElse textItemType = 24 Then
                isResName = True
            End If
            coordsField(0) = x
            coordsField(1) = y
        End Sub

        Public Shared Function stringTruncate(text As String) As String
            Dim length = text.Length
            Dim lastNonSpace = -1
            For i = 0 To length - 1
                If text(i) <> " "c Then
                    lastNonSpace = i
                End If
            Next
            If lastNonSpace > -1 Then
                text = text.Substring(0, lastNonSpace + 1)
            End If
            Return text
        End Function

        Public Overridable Sub applyDimShifts(dimShiftX As Single, dimShiftY As Single)
            coordsField(0) = coordsField(0) + dimShiftX
            coordsField(1) = coordsField(1) + dimShiftY
        End Sub

        Public Overridable Sub calcPlotCoords(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale)
            Dim centre = New Single(1) {}
            centre(0) = scale.ScaleFactor * (pdb.getOffset(0) + coordsField(0))
            psCoords(0) = scale.ScaleFactor * (pdb.getOffset(0) + coordsField(0))
            centre(1) = scale.ScaleFactor * (pdb.getOffset(1) + coordsField(1))
            psCoords(1) = scale.ScaleFactor * (pdb.getOffset(1) + coordsField(1))
            centre(1) = scale.PlotHeight - centre(1)
            fontSize = scale.ScaleFactor * size
            fontFamily = PlotArea.FontFamily
            If paintType = 0 Then
                setFont(graphics, fontSize, False)
            Else
                textHeightField = fontSize
            End If
            plotCoords(0) = centre(0) - textWidthField / 2.0F
            plotCoords(1) = centre(1) + textHeightField / 2.0F
            psCoords(0) = psCoords(0) - realWidth / 2.0F
            psCoords(1) = psCoords(1) - realHeight / 2.0F
            If textItemType <> 12 AndAlso textItemType <> 13 Then
                realHeight = textHeightField / scale.ScaleFactor
                realWidth = textWidthField / scale.ScaleFactor
            Else
                realHeight = textHeightField
                realWidth = textWidthField
            End If
        End Sub

        Public Overridable Function clickCheck(x As Single, y As Single) As Object
            Dim clickObject As Object = Nothing
            If x < coordsField(0) - realWidth / 2.0F OrElse x > coordsField(0) + realWidth / 2.0F OrElse y < coordsField(1) - realHeight / 2.0F OrElse y > coordsField(1) + realHeight / 2.0F Then
                Return clickObject
            End If
            clickObject = Me
            Return clickObject
        End Function

        Public Function MaxMinCoords() As Single()

            Dim textCoords = New Single(3) {}
            If realWidth = 0.0F OrElse realHeight = 0.0F Then
                setTextItemSize()
                realHeight = size
                Dim length = showText.Length
                realWidth = 0.7F * length * realHeight
            End If
            If textItemType <> 12 Then
                textCoords(0) = coordsField(0) - realWidth / 2.0F
                textCoords(1) = coordsField(0) + realWidth / 2.0F
                textCoords(2) = coordsField(1) - realHeight / 2.0F
                textCoords(3) = coordsField(1) + realHeight / 2.0F
            Else
                textCoords(0) = coordsField(0)
                textCoords(1) = coordsField(0) + realWidth
                textCoords(2) = coordsField(1)
                textCoords(3) = coordsField(1) + realHeight
            End If
            Return textCoords
        End Function

        Public Overridable Property AntibodyLoopID As String
            Get
                Return antibodyLoopIDField
            End Get
            Set(value As String)
                antibodyLoopIDField = value
            End Set
        End Property

        Public Overridable Property Coords As Single()
            Get
                Return coordsField
            End Get
            Set(value As Single())
                For i = 0 To 1
                    coordsField(i) = value(i)
                Next
            End Set
        End Property

        Public Overridable ReadOnly Property Text As String
            Get
                Return textField
            End Get
        End Property

        Public Overridable ReadOnly Property Type As Integer
            Get
                Return textItemType
            End Get
        End Property

        Public Overridable ReadOnly Property TextHeight As Single
            Get
                Return realHeight
            End Get
        End Property

        Public Overridable ReadOnly Property TextWidth As Single
            Get
                Return realWidth
            End Get
        End Property

        Private ReadOnly Property [On] As Boolean
            Get
                Dim lOn = True
                If Not ReferenceEquals(typeName, Nothing) Then
                    Dim onOffString = params(typeName & "_STATUS")
                    If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") Then
                        lOn = False
                    End If
                End If
                Return lOn
            End Get
        End Property

        Public Overridable Sub moveTextItem(realMoveX As Single, realMoveY As Single)
            coordsField(0) = coordsField(0) + realMoveX
            coordsField(1) = coordsField(1) - realMoveY
        End Sub

        Public Overridable Sub paintTextItem(paintType As Integer, graphics As IGraphics, psFile As PostScript, scale As Scale, selected As Boolean)
            Dim setColour As Color = Nothing
            showText = textField
            If textField.Equals("") AndAlso params("SHOW_BLANK_TEXT_STATUS").Equals("ON") Then
                showText = "----"
            End If
            If params("SHOW_CHAIN_IDS").Equals("OFF") AndAlso isResName AndAlso showText.Contains("(") Then
                showText = removeChainId(showText)
            End If
            setTextItemSize()
            setTextItemColour()
            If textItemType = 12 OrElse textItemType = 13 Then
                If paintType = 0 Then
                    setFont(graphics, size, False)
                End If
                For i = 0 To 1
                    plotCoords(i) = coordsField(i)
                Next
            Else
                calcPlotCoords(paintType, graphics, psFile, scale)
            End If
            If Not selected Then
                Dim onOffString = params("INACTIVE_PLOTS_STATUS")
                If Not ReferenceEquals(onOffString, Nothing) AndAlso onOffString.Equals("OFF") AndAlso textItemType <> 12 AndAlso textItemType <> 13 Then
                    visibleField = False
                Else
                    visibleField = True
                End If
            Else
                visibleField = True
            End If
            If visibleField AndAlso [On] Then
                If Not ReferenceEquals(typeName, Nothing) AndAlso textItemType = 10 Then
                    Dim bgColour = ligplus.Params.getBackgroundColour(params)
                    If paintType = 0 Then
                        graphics.FillRectangle(New SolidBrush(bgColour), plotCoords(0), plotCoords(1) - textHeightField, textWidthField, textHeightField)
                    Else
                        Dim x1 = psCoords(0) - realWidth * scale.ScaleFactor / 2.0F
                        Dim y1 = psCoords(1) - realHeight * scale.ScaleFactor / 2.0F
                        Dim x2 = x1 + realWidth * scale.ScaleFactor
                        Dim y2 = y1 + realHeight * scale.ScaleFactor
                        psFile.psUnboundedBox(x1, y1, x1, y2, x2, y2, x2, y1, bgColour)
                    End If
                End If
                If selected Then
                    setColour = colour
                Else
                    Dim inactiveName = params("INACTIVE_COLOUR")
                    Dim inactiveColour = ligplus.Params.getColour(inactiveName)
                    setColour = inactiveColour
                End If
                If paintType = 0 Then
                    graphics.DrawString(showText, font, New SolidBrush(setColour), CInt(plotCoords(0)), CInt(plotCoords(1)))
                Else
                    psFile.psCentredText(psCoords(0), psCoords(1), realHeight * scale.ScaleFactor, setColour, showText)
                End If
            End If
        End Sub

        Friend Overridable Sub flipTextItem(shiftX As Single, shiftY As Single, matrix As Double()(), inverseMatrix As Double()())
            coordsField = Angle.flipCoords(coordsField, shiftX, shiftY, matrix, inverseMatrix)
        End Sub

        Friend Overridable Sub rotateTextItem(pivotX As Single, pivotY As Single, matrix As Double()())
            coordsField = Angle.applyRotationMatrix(pivotX, pivotY, coordsField, matrix)
        End Sub


        Public Overridable Sub restoreCoords(storePos As Integer)
            coordsField(0) = saveCoords_Conflict(storePos)(0)
            coordsField(1) = saveCoords_Conflict(storePos)(1)
            textField = saveText(storePos)
        End Sub

        Public Overridable Sub saveCoords(storePos As Integer)
            saveCoords_Conflict(storePos)(0) = coordsField(0)
            saveCoords_Conflict(storePos)(1) = coordsField(1)
            saveText(storePos) = textField
        End Sub


        Public Overridable WriteOnly Property MoleculeID As String
            Set(value As String)
                moleculeIDField = value
            End Set
        End Property

        Public Property Visible As Boolean
            Get
                Return _Visible
            End Get
            Friend Set(value As Boolean)
                _Visible = value
            End Set
        End Property

        Public Overridable Sub setIsAntigen()
            isAntigen = True
        End Sub

        Private Sub setFont(graphics As IGraphics, fontSize As Integer, bold As Boolean)
            If bold Then
                font = New Font(fontFamily, 0, fontSize)
            Else
                font = New Font(fontFamily, 1, fontSize)
            End If
            graphics.Font = font
            Dim fontmetrics As SizeF = graphics.MeasureString(showText, font)
            textHeightField = fontmetrics.Height
            textWidthField = fontmetrics.Width
        End Sub

        Private Sub setTextItemColour()
            colour = Color.Black
            Dim colourName = params(typeName & "_COLOUR")
            If Not ReferenceEquals(colourName, Nothing) Then
                colour = ligplus.Params.getColour(colourName)
            End If
            Dim name As String = Nothing
            If Not antibodyLoopIDField.Equals("") Then
                If textItemType = 1 OrElse textItemType = 2 OrElse textItemType = 3 OrElse textItemType = 4 OrElse textItemType = 5 OrElse textItemType = 6 Then
                    name = "ABODY_" & antibodyLoopIDField & "_RESNAME_COLOUR"
                ElseIf textItemType = 7 OrElse textItemType = 8 OrElse textItemType = 9 Then
                    name = "ABODY_" & antibodyLoopIDField & "_ATNAME_COLOUR"
                Else
                    name = "ABODY_" & antibodyLoopIDField & "_COLOUR"
                End If
            End If
            If isAntigen Then
                name = "ABODY_ANTIGEN_COLOUR"
            End If
            If Not ReferenceEquals(name, Nothing) AndAlso params.ContainsKey(name) Then
                colourName = params(name)
                If Not ReferenceEquals(colourName, Nothing) Then
                    colour = ligplus.Params.getColour(colourName)
                End If
            End If
            If Not ReferenceEquals(moleculeIDField, Nothing) Then
                name = Nothing
                If textItemType = 1 OrElse textItemType = 2 OrElse textItemType = 3 OrElse textItemType = 4 OrElse textItemType = 5 OrElse textItemType = 6 Then
                    name = moleculeIDField & "_RESNAME_COLOUR"
                ElseIf textItemType = 7 OrElse textItemType = 8 OrElse textItemType = 9 Then
                    name = moleculeIDField & "_ATOMNAME_COLOUR"
                End If
                If Not ReferenceEquals(name, Nothing) AndAlso params.ContainsKey(name) Then
                    colourName = params(name)
                    If Not ReferenceEquals(colourName, Nothing) Then
                        colour = ligplus.Params.getColour(colourName)
                    End If
                End If
            End If
        End Sub

        Private Sub setTextItemSize()
            size = 0.0F
            If Not ReferenceEquals(typeName, Nothing) Then
                Dim numberString = params(sizeTypeName & "_SIZE")
                If Not ReferenceEquals(numberString, Nothing) Then
                    size = Single.Parse(numberString)
                    realHeight = size
                    Dim length = showText.Length
                    realWidth = 0.7F * length * realHeight
                End If
            End If
        End Sub

        Public Shared Function typesetText([string] As String) As String
            Dim newSentence = True
            Dim iToken = 0
            Dim tokens As Scanner = New Scanner([string])
            [string] = ""
            While tokens.HasNext()
                Dim token As String = tokens.Next()
                If iToken > 0 Then
                    [string] = [string] & " "
                End If
                If newSentence Then
                    Dim tmpString As String = "" & token(0).ToString()
                    [string] = [string] & tmpString.ToUpper()
                    tmpString = "" & token.Substring(1)
                    [string] = [string] & tmpString.ToLower()
                Else
                    Dim nPunct = 0, nNumbers = nPunct, nLetters = nNumbers
                    For i = 0 To token.Length - 1
                        Dim ch = token(i)
                        If ch >= "A"c AndAlso ch <= "Z"c OrElse ch >= "a"c AndAlso ch <= "z"c Then
                            nLetters += 1
                        ElseIf ch >= "0"c AndAlso ch <= "9"c Then
                            nNumbers += 1
                        Else
                            Dim specialCase = False
                            If i = token.Length - 1 AndAlso token.Length > 2 AndAlso (ch = "."c OrElse ch = ","c OrElse ch = ":"c OrElse ch = ";"c OrElse ch = ")"c OrElse ch = "]"c) Then
                                specialCase = True
                            End If
                            If i = 0 AndAlso (ch = ")"c OrElse ch = "]"c) Then
                                specialCase = True
                            End If
                            If ch = "-"c Then
                                specialCase = True
                            End If
                            If Not specialCase Then
                                nPunct += 1
                            End If
                        End If
                    Next
                    If nNumbers = 0 AndAlso nPunct = 0 Then
                        token = token.ToLower()
                    End If
                    [string] = [string] & token
                End If
                newSentence = False
                If token(token.Length - 1) = "."c AndAlso token.Length > 2 Then
                    newSentence = True
                End If
                iToken += 1
            End While
            Return [string]
        End Function

        Private Function removeChainId(text As String) As String
            Dim haveBracket = False
            Dim length = text.Length
            Dim lastChar = -1
            Dim i = 0

            While i < length AndAlso Not haveBracket
                If text(i) <> " "c Then
                    lastChar = i
                    If text(i) = "("c Then
                        haveBracket = True
                    End If
                End If

                i += 1
            End While
            If lastChar > -1 Then
                text = text.Substring(0, lastChar)
            End If
            Return text
        End Function
    End Class

End Namespace
