Imports ligplus.file

Namespace ligplus
    Public Class Scale
        Private Const NEAR_ZERO As Single = 0.000001F

        Public Const TO_SCREEN As Integer = 0

        Public Const POSTSCRIPT_PLOT As Integer = 1

        Private landscape As Boolean = True

        Private centre As Single() = New Single(1) {}

        Public height As Single = 0.0F

        Private offset As Single() = New Single(1) {}

        Private scaleFactorField As Single = 1.0F

        Public width As Single = 0.0F

        Private plotAreaHeight As Integer = 0

        Private plotAreaWidth As Integer = 0

        Private plotType As Integer = 0

        Private plotArea As PlotArea = Nothing

        Private psPage As WritePSFile = Nothing

        Public Sub New(plotArea As PlotArea)
            init()
            Me.plotArea = plotArea
            plotType = 0
        End Sub

        Public Sub New(psPage As WritePSFile, landscape As Boolean)
            init()
            Me.psPage = psPage
            Me.landscape = landscape
            plotType = 1
        End Sub

        Public Overridable Function convertToReal(coord As Integer, i As Integer, splitScreenOffset As Single, splitShift As Single) As Single
            If i = 1 Then
                coord = plotArea.Height - coord
            End If
            Dim realCoord = coord / scaleFactorField - offset(i) + splitScreenOffset + splitShift
            Return realCoord
        End Function

        Private Sub init()
            For i = 0 To 1
                offset(i) = 0.0F
            Next
        End Sub

        Public Overridable Sub adjustOffset(i As Integer, moveX As Integer)
            offset(i) = offset(i) + moveX / scaleFactorField
        End Sub

        Public Overridable Sub adjustScaleFactor(fraction As Double)
            Dim oldScaleFactor = scaleFactorField
            scaleFactorField = CSng(scaleFactorField * (1.0R + fraction))
            For i = 0 To 1
                offset(i) = oldScaleFactor / scaleFactorField * (centre(i) + offset(i)) - centre(i)
            Next
        End Sub

        Public Overridable Sub calcScale(coordsMin As Single(), coordsMax As Single(), margin As Single)
            Dim cMin = New Single(1) {}
            Dim cMax = New Single(1) {}
            Dim i As Integer
            For i = 0 To 1
                cMin(i) = coordsMin(i) - margin
                cMax(i) = coordsMax(i) + margin
            Next
            height = cMax(1) - cMin(1)
            width = cMax(0) - cMin(0)
            centre(0) = (cMin(0) + cMax(0)) / 2.0F
            centre(1) = (cMin(1) + cMax(1)) / 2.0F
            If Math.Abs(width) < 0.000001F Then
                width = 1.0F
            End If
            If Math.Abs(height) < 0.000001F Then
                height = 1.0F
            End If
            For i = 0 To 1
                offset(i) = -cMin(i)
            Next
            If plotType = 0 Then
                plotAreaHeight = plotArea.Height
                plotAreaWidth = plotArea.Width
            Else
                plotAreaHeight = PostScript.getHeight(landscape)
                plotAreaWidth = PostScript.getWidth(landscape)
            End If
            If height / width < plotAreaHeight / plotAreaWidth Then
                scaleFactorField = plotAreaWidth / width
                offset(1) = offset(1) + (plotAreaHeight / scaleFactorField - height) / 2.0F
            Else
                scaleFactorField = plotAreaHeight / height
                offset(0) = offset(0) + (plotAreaWidth / scaleFactorField - width) / 2.0F
            End If
        End Sub

        Public Overridable Function getOffset(i As Integer) As Single
            Return offset(i)
        End Function

        Public Overridable ReadOnly Property PlotHeight As Integer
            Get
                If plotType = 0 Then
                    Return plotArea.Height
                End If
                Return plotAreaHeight
            End Get
        End Property

        Public Overridable ReadOnly Property PlotWidth As Integer
            Get
                If plotType = 0 Then
                    Return plotArea.Width
                End If
                Return plotAreaWidth
            End Get
        End Property

        Public Overridable ReadOnly Property ScaleFactor As Single
            Get
                Return scaleFactorField
            End Get
        End Property
    End Class

End Namespace
