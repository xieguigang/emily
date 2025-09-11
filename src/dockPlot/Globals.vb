Namespace ligplus
    Public Class Globals
        Private Shared colourModeField As Integer = 0

        Private Shared orientationField As Integer = 0

        Private Shared onOffTypes As String() = New String() {"H-bonds", "Hydrophobics", "Ligand atoms", "Nonlig atoms", "Water atoms", "Atom edges", "Plot title", "Ligand res names", "Nonligand res names", "Water names", "Hydrophobic res names", "Ligand atom names", "Nonligand atom names", "H-bond lengths"}

        Private Shared onOffParam As Boolean() = New Boolean(onOffTypes.Length - 1) {}

        Private Shared nparamsField As Integer = 0

        Public Const COLOUR As Integer = 0

        Public Const BLACK_AND_WHITE As Integer = 1

        Public Const PORTRAIT As Integer = 0

        Public Const LANDSCAPE As Integer = 1

        Public Sub New()
            nparamsField = 0
            For i = 0 To onOffTypes.Length - 1
                onOffParam(i) = True
            Next
            onOffParam(1) = False
        End Sub

        Public Overridable Sub extractGlobals(parameter As Char)
            Dim iparam As Integer
            Dim yesNo = False
            If parameter = "Y"c Then
                yesNo = True
            End If
            Select Case nparamsField
                Case 0
                    If parameter = "C"c Then
                        colourModeField = 0
                        Exit Select
                    End If
                    colourModeField = 1
                Case 1
                    If parameter = "P"c Then
                        orientationField = 0
                        Exit Select
                    End If
                    orientationField = 1
                Case 2
                    iparam = getOnOffNo("Ligand atoms")
                    onOffParam(iparam) = yesNo
                Case 3
                    iparam = getOnOffNo("Nonlig atoms")
                    onOffParam(iparam) = yesNo
                Case 4
                    iparam = getOnOffNo("Water atoms")
                    onOffParam(iparam) = yesNo
            End Select
            nparamsField += 1
        End Sub

        Public Overridable ReadOnly Property ColourMode As Integer
            Get
                Return colourModeField
            End Get
        End Property

        Public Overridable ReadOnly Property Nparams As Integer
            Get
                Return nparamsField
            End Get
        End Property

        Public Overridable Function getOnOffNo(onOffName As String) As Integer
            Dim iparam = -1
            Dim i = 0

            While i < onOffTypes.Length AndAlso iparam = -1
                If onOffName.Equals(onOffTypes(i)) Then
                    iparam = i
                End If

                i += 1
            End While
            Return iparam
        End Function

        Public Overridable Function getOnOffParam(onOffName As String) As Boolean
            If onOffName.Equals("ON") Then
                Return True
            End If
            If onOffName.Equals("OFF") Then
                Return False
            End If
            Dim iparam = getOnOffNo(onOffName)
            If iparam < 0 Then
                Return False
            End If
            Return onOffParam(iparam)
        End Function

        Public Overridable ReadOnly Property Orientation As Integer
            Get
                Return orientationField
            End Get
        End Property
    End Class

End Namespace
