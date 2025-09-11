Imports Microsoft.VisualBasic.Text.Parser
Imports System.Drawing


Namespace ligplus

    Public Class Params
        Public Shared defaultParamFile As String = "defaults.par"

        Public Const defaultParamFileWin As String = "defaults.win"

        Public Const defaultParamFileMac As String = "defaults.linux"

        Public Const defaultParamFileLinux As String = "defaults.linux"

        Public Const userParamFile As String = "ligplus.par"

        Public Const EXE_DIR As Integer = 0

        Public Const HET_GROUP_DICTIONARY As Integer = 1

        Public Const PARAM_DIR As Integer = 2

        Public Const PDB_DIR As Integer = 3

        Public Const TMP_DIR As Integer = 4

        Public Const RASMOL_EXE As Integer = 5

        Public Const FIRST_TIME As Integer = 6

        Public Const MY_ACCOUNT As Integer = 7

        Public Const MY_DOMAIN As Integer = 8

        Public Const PYMOL_EXE As Integer = 9

        Public Shared ReadOnly paramName As String() = New String() {"EXE_DIR", "HET_GROUP_DICTIONARY", "PARAM_DIR", "PDB_DIR", "TMP_DIR", "RASMOL_EXE", "FIRST_TIME", "MY_ACCOUNT", "MY_DOMAIN", "PYMOL_EXE"}

        Public Const TO_SCREEN As Integer = 0

        Public Const PS_PLOT As Integer = 1

        Public Const NRESTORE_POINTS As Integer = 10

        Public Const USE_IF_SENSIBLE As Integer = 1

        Public Const IGNORE As Integer = 2

        Public Const ACCEPT_ALL As Integer = 3

        Public Const HBPLUS_DPARAM As String = "3.35"

        Public Const HBPLUS_HPARAM As String = "2.70"

        Public Const HPHOBIC_ONLY As Integer = 0

        Public Const HPHOBIC_ANY As Integer = 1

        Public Const ANY_ANY As Integer = 2

        Public Const HBPLUS_NB_DPARAM As String = "3.90"

        Public Const HBPLUS_NB_HPARAM As String = "2.90"

        Public Const NONE As Integer = -1

        Public Const KABAT As Integer = 0

        Public Const CHOTHIA As Integer = 1

        Public Const MARTIN As Integer = 2

        Public Const OTHER As Integer = 3

        Public Const NSCHEMES As Integer = 3

        Public Const NLOOPS As Integer = 6

        Public Shared ReadOnly antibodyLoop As String()()() = New String()()() {New String()() {New String() {"L1", "L  24 ", "L  34 "}, New String() {"L2", "L  50 ", "L  56 "}, New String() {"L3", "L  89 ", "L  97 "}, New String() {"H1", "H  31 ", "H  35B"}, New String() {"H2", "H  50 ", "H  65 "}, New String() {"H3", "H  95 ", "H 102 "}}, New String()() {New String() {"L1", "L  24 ", "L  34 "}, New String() {"L2", "L  50 ", "L  56 "}, New String() {"L3", "L  89 ", "L  97 "}, New String() {"H1", "H  26 ", "H  32 "}, New String() {"H2", "H  52 ", "H  56 "}, New String() {"H3", "H  95 ", "H 102 "}}, New String()() {New String() {"L1", "L  24 ", "L  34 "}, New String() {"L2", "L  50 ", "L  56 "}, New String() {"L3", "L  89 ", "L  97 "}, New String() {"H1", "H  26 ", "H  35 "}, New String() {"H2", "H  50 ", "H  58 "}, New String() {"H3", "H  95 ", "H 102 "}}}

        Public Shared ReadOnly antibodyLoopID As String() = New String() {"L1", "L2", "L3", "H1", "H2", "H3"}

        Public Shared ReadOnly antibodyLoopName As String() = New String() {"Loop L1", "Loop L2", "Loop L3", "Loop H1", "Loop H2", "Loop H3"}

        Private Shared colourIndexField As Dictionary(Of String, String) = Nothing

        Private Shared ligplusParams As Dictionary(Of String, String) = Nothing

        Public Shared defaultParamFileName As String = Nothing

        Public Shared docsPath As String = Nothing

        Public Shared exePath As String = Nothing

        Public Shared imgPath As String = Nothing

        Public Shared libPath As String = Nothing

        Public Shared paramPath As String = Nothing

        Private Shared colourListField As List(Of Object) = Nothing

        Private Shared colourNameListField As List(Of Object) = Nothing

        Public currentDirectory As String = Nothing

        Public Sub New(docsPath As String, exePath As String, imgPath As String, libPath As String, paramPath As String, currentDirectory As String, OS As String)
            Params.docsPath = docsPath
            Params.exePath = exePath
            Params.imgPath = imgPath
            Params.libPath = libPath
            Params.paramPath = paramPath
            Me.currentDirectory = currentDirectory
            Console.WriteLine("Current directory set to: " & currentDirectory)
            If Not ReferenceEquals(OS, Nothing) Then
                If OS.Equals("Windows") Then
                    defaultParamFile = "defaults.win"
                ElseIf OS.Equals("Mac") Then
                    defaultParamFile = "defaults.linux"
                ElseIf OS.Equals("linux") OrElse OS.Equals("linux64") Then
                    defaultParamFile = "defaults.linux"
                End If
            End If
            readInParameters()
            If ligplusParams IsNot Nothing Then
                createColourParameters()
            End If
        End Sub

        Public Shared Function getAntibodyLoopID(numberingScheme As Integer, fullChain As String, fullResNum As Integer, fullHeavyChain As String, fullLightChain As String) As String
            Dim antibodyLoopID = "OTHER"
            If numberingScheme > -1 AndAlso numberingScheme < 3 Then
                Dim newFullChain = fullChain
                If fullChain.Equals(fullLightChain) Then
                    newFullChain = "L"
                ElseIf fullChain.Equals(fullHeavyChain) Then
                    newFullChain = "H"
                End If
                Dim search = ""
                If fullResNum > 0 AndAlso fullResNum < 10000 Then
                    search = String.Format("{0}{1,4:D} ", New Object() {newFullChain, Convert.ToInt32(fullResNum)})
                End If
                For [loop] = 0 To 5
                    If search.CompareTo(antibodyLoop(numberingScheme)([loop])(1)) >= 0 AndAlso search.CompareTo(antibodyLoop(numberingScheme)([loop])(2)) <= 0 Then
                        antibodyLoopID = antibodyLoop(numberingScheme)([loop])(0)
                    End If
                Next
            End If
            Return antibodyLoopID
        End Function

        Public Shared Function getBackgroundColour(params As Properties) As Color
            Dim bgColour = Color.White
            Dim colourName = params("BACKGROUND_COLOUR")
            If Not ReferenceEquals(colourName, Nothing) Then
                bgColour = getColour(colourName)
            End If
            Return bgColour
        End Function

        Public Shared Function getColour(colourName As String) As Color
            Dim colour = Color.BLACK
            Dim index = 0
            If colourIndexField Is Nothing Then
                Return colour
            End If
            Dim numberString = colourIndexField(colourName)
            If ReferenceEquals(numberString, Nothing) Then
                Return colour
            End If
            index = Integer.Parse(numberString)
            If index < colourListField.Count Then
                colour = colourListField(index)
            End If
            Return colour
        End Function

        Public Shared ReadOnly Property ColourIndex As Dictionary(Of String, String)
            Get
                Return colourIndexField
            End Get
        End Property

        Public Shared ReadOnly Property ColourList As List(Of Object)
            Get
                Return colourListField
            End Get
        End Property

        Public Shared ReadOnly Property ColourNameList As List(Of Object)
            Get
                Return colourNameListField
            End Get
        End Property

        Public Shared Function getFontName(params As Dictionary(Of String, String)) As String
            Dim fontName = params("FONT_NAME")
            Return fontName
        End Function

        Public Shared Function getGlobalProperty(globalProperty As Integer, instance As Integer) As String
            Dim key As String, value As String = Nothing
            If globalProperty < paramName.Length Then
                key = paramName(globalProperty)
            Else
                Return value
            End If
            If instance > 0 Then
                key = key & instance.ToString()
            End If
            value = ligplusParams(key)
            Return value
        End Function

        Public Sub New(i As Integer)
        End Sub

        Public Overridable ReadOnly Property DefaultFileName As String
            Get
                Return defaultParamFileName
            End Get
        End Property

        Private Sub createColourParameters()
            Dim done = False
            Dim icol = 0
            Dim nColour = 0
            Dim red = 0.0F, green = 0.0F, blue = 0.0F
            Dim colourName As String = Nothing
            colourListField = New List(Of Object)()
            colourNameListField = New List(Of Object)()
            colourIndexField = New Dictionary(Of String, String)()
            done = False
            icol = 0
            While Not done
                Dim key As String = "COLOUR" & icol.ToString()
                Dim colourString = ligplusParams(key)
                If Not ReferenceEquals(colourString, Nothing) Then
                    colourName = ""
                    Dim token As Scanner = New Scanner(colourString)
                    Dim itoken = 0

                    While token.HasNext() = True
                        Dim f As Single?
                        Dim numberString As String = token.Next()
                        Select Case itoken
                            Case 0
                                f = Convert.ToSingle(Single.Parse(numberString))
                                red = f.Value
                            Case 1
                                f = Convert.ToSingle(Single.Parse(numberString))
                                green = f.Value
                            Case 2
                                f = Convert.ToSingle(Single.Parse(numberString))
                                blue = f.Value
                            Case 3
                                colourName = numberString
                        End Select

                        itoken += 1
                    End While
                    If Not ReferenceEquals(colourName, Nothing) Then
                        Dim colour = getColourFromRGB(red, green, blue)
                        Dim index As String = "" & nColour.ToString()
                        colourIndexField(colourName) = index
                        colourListField.Add(colour)
                        colourNameListField.Add(colourName)
                    End If
                Else
                    done = True
                End If
                icol += 1
                nColour += 1
            End While
        End Sub

        Private Function getColourFromRGB(red As Single, green As Single, blue As Single) As Color
            Dim hsb = New Single(2) {}
            Dim colour = Color.Black
            Dim rInt As Integer = Math.Round(red * 255.0F, MidpointRounding.AwayFromZero)
            Dim gInt As Integer = Math.Round(green * 255.0F, MidpointRounding.AwayFromZero)
            Dim bInt As Integer = Math.Round(blue * 255.0F, MidpointRounding.AwayFromZero)

            colour = Color.FromArgb(rInt, gInt, bInt)
            Return colour
        End Function

        Public Overridable ReadOnly Property Parameters As Dictionary(Of String, String)
            Get
                Return ligplusParams
            End Get
        End Property

        Public Overridable Function getProperty(key As String) As String
            Dim value As String = Nothing
            If ligplusParams Is Nothing Then
                Return value
            End If
            value = ligplusParams(key)
            Return value
        End Function

        Public Overridable Function readParamFile(file As String) As Dictionary(Of String, String)
            Return Nothing
        End Function

        Private Sub readInParameters()

        End Sub

        Public Overridable Function saveGlobalParams(params As Dictionary(Of String, String)) As Boolean
            Dim ok = True
            Return ok
        End Function

        Public Overridable Function writeParams(params As Dictionary(Of String, String), paramFile As String) As Boolean
            Dim ok = True
            Return ok
        End Function
    End Class

End Namespace
