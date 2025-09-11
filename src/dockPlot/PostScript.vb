Imports System.Drawing
Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class PostScript
        Private lastColour As Color = Nothing

        Private lastCircleColour As Color = Nothing

        Private lastSphereColour As Color = Nothing

        Private lastLineWidth As Single = -1.0F

        Private file As PrintStream = Nothing

        Private Shared xx1 As Double = -1.0R

        Private Shared xx2 As Double = 650.0R

        Private Shared xy1 As Double = -1.0R

        Private Shared xy2 As Double = 951.0R

        Private Shared offset_x As Single

        Private Shared offset_y As Single

        Private Shared pageCentrex As Single

        Private Shared pageCentrey As Single

        Private Shared pageMaxx As Single

        Private Shared pageMaxy As Single

        Private Shared pageMinx As Single

        Private Shared pageMiny As Single

        Private Shared scale As Scale

        Private Const DEFAULT_LINE_WIDTH As Single = 0.2F

        Private Const NEAR_ZERO As Single = 1.0E-6F

        Public Const PLOT_MARGIN As Single = 0.3F

        Public Const SPLIT_MARGIN As Single = 0.3F

        Public Const PORTRAIT As Integer = 0

        Public Const LANDSCAPE As Integer = 1

        Public Const BBOXX1 As Integer = 30

        Public Const BBOXX2 As Integer = 550

        Public Const BBOXY1 As Integer = 50

        Public Const BBOXY2 As Integer = 780

        Public Const BORDER_MARGIN As Double = 0.93R

        Friend Sub New(file As PrintStream, scale As Scale)
            Me.file = file
            ' this;
            PostScript.scale = scale
        End Sub

        Public Shared Function getWidth(landscape As Boolean) As Integer
            Dim width = 0
            If landscape Then
                width = 730
            Else
                width = 520
            End If
            Return width
        End Function

        Public Shared Function getHeight(landscape As Boolean) As Integer
            Dim height = 0
            If landscape Then
                height = 520
            Else
                height = 730
            End If
            Return height
        End Function

        Public Overridable Sub psArc(x As Single, y As Single, radius As Single, angle_start As Single, angle_end As Single, lineWidth As Single, colour As Color)
            RGBcolour = colour
            x = convertx(x)
            y = converty(y)
            radius = convertLength(radius)
            writeCommand(x, y, radius, angle_start, angle_end, "Arc", 2)
        End Sub

        Public Overridable Sub psFilledCircle(x As Single, y As Single, radius As Single, colour As Color)
            CircleColour = colour
            x = convertx(x)
            y = converty(y)
            radius = convertLength(radius)
            writeCommand(x, y, radius, "Ucircle", 2)
        End Sub

        Public Overridable Sub psCentredText(x As Single, y As Single, textSize As Single, colour As Color, text As String)
            x = convertx(x)
            y = converty(y)
            textSize = convertLength(textSize)
            RGBcolour = colour
            writeCommand(x, y, "moveto", 2)
            file.format("(%s) %s Center" & vbLf, New Object() {text, format(textSize, 2)})
            file.format("(%s) %s Print" & vbLf, New Object() {text, format(textSize, 2)})
        End Sub

        Public Overridable Sub psComment([string] As String)
            file.format(vbLf, New Object(-1) {})
            file.format("%% %s" & vbLf, New Object() {[string]})
        End Sub

        Public Overridable Sub psDrawDashedLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, width As Single, colour As Color, [on] As Single, off As Single)
            [on] = convertLength([on])
            off = convertLength(off)
            file.format("[ %s %s ] 0 setdash" & vbLf, New Object() {format([on], 2), format(off, 2)})
            psDrawLine(x1, y1, x2, y2, width, colour)
            file.format("[] 0 setdash" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub psEllipse(x As Single, y As Single, width As Single, height As Single, colour As Color, lineWidth As Single, angle As Single)
            Dim ratio = height / width
            x = convertx(x)
            y = converty(y)
            width = convertLength(width)
            lineWidth = convertLength(lineWidth)
            If Math.Abs(lineWidth) < 1.0E-6F Then
                lineWidth = 0.2F
            End If
            writeCommand(x, y, "moveto", 2)
            writeCommand(x, y, -angle, "RotAngle", 2)
            Me.LineWidth = lineWidth
            RGBcolour = colour
            writeCommand(0.0F, 0.0F, width, 1.0F, ratio, 0.0F, 0.0F, "Oellipse", 2)
            file.format("R" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub setDashPattern([on] As Single, off As Single)
            [on] = convertLength([on])
            off = convertLength(off)
            If [on] > 0.0F AndAlso off > 0.0F Then
                file.format("[ %s %s ] 0 setdash" & vbLf, New Object() {format([on], 2), format(off, 2)})
            Else
                file.format("[] 0 setdash" & vbLf, New Object(-1) {})
            End If
        End Sub

        Public Overridable Sub psDrawLine(x1 As Single, y1 As Single, x2 As Single, y2 As Single, width As Single, colour As Color)
            x1 = convertx(x1)
            y1 = converty(y1)
            x2 = convertx(x2)
            y2 = converty(y2)
            width = convertLength(width)
            If Math.Abs(width) < 1.0E-6F Then
                width = 0.2F
            End If
            LineWidth = width
            RGBcolour = colour
            writeCommand(x1, y1, x2, y2, "L", 2)
        End Sub

        Public Overridable Sub psFillColour(colour As Color)
        End Sub

        Public Overridable Sub psRotate90(new_origin_x As Single, new_origin_y As Single)
            file.format(vbLf, New Object(-1) {})
            writeCommand(new_origin_x, new_origin_y, " moveto Rot90", 1)
            file.format(vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub psSphere(x As Single, y As Single, radius As Single, colour As Color, edgeColour As Color)
            x = convertx(x)
            y = converty(y)
            radius = convertLength(radius)
            RGBcolour = edgeColour
            SphereColour = colour
            LineWidth = 0.2F
            writeCommand(x, y, radius, "Sphere", 2)
        End Sub

        Public Overridable Sub psUnboundedBox(x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single, colour As Color)
            x1 = convertx(x1)
            y1 = converty(y1)
            x2 = convertx(x2)
            y2 = converty(y2)
            x3 = convertx(x3)
            y3 = converty(y3)
            x4 = convertx(x4)
            y4 = converty(y4)
            file.format("G" & vbLf, New Object(-1) {})
            RGBcolour = colour
            LineWidth = 0.0F
            file.format("G" & vbLf, New Object(-1) {})
            writeCommand(x1, y1, x2, y2, x3, y3, x4, y4, "Pl4", 2)
            file.format("R" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub writeClosingLines()
            file.format("%%Trailer" & vbLf, New Object(-1) {})
            Dim [string] As String = 29.ToString() & " " & 49.ToString() & " " & 551.ToString() & " " & 781.ToString()
            file.format("%%BoundingBox: %s" & vbLf, New Object() {[string]})
            file.format("%%EOF" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub writeEndPage()
            file.format(vbLf, New Object(-1) {})
            file.format(vbLf, New Object(-1) {})
            file.format("LigPlusSave restore" & vbLf, New Object(-1) {})
            file.format("showpage" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub writeMainHeaders(plotTitle As String, [date] As String, npages As Integer, PSFontFamily As String)
            file.format("%%!PS-Adobe-3.0" & vbLf, New Object(-1) {})
            file.format("%%%%Creator: LigPlus v.2.3.1" & vbLf, New Object(-1) {})
            file.format("%%%%DocumentNeededResources: font %s Symbol" & vbLf, New Object() {PSFontFamily})
            file.format("%%%%BoundingBox: (atend)" & vbLf, New Object(-1) {})
            file.format("%%%%Pages: %d" & vbLf, New Object() {Convert.ToInt32(npages)})
            file.format("%%%%Date: %s" & vbLf, New Object() {[date]})
            file.format("%%%%Title: %s" & vbLf, New Object() {plotTitle})
            file.format("%%%%EndComments" & vbLf, New Object(-1) {})
            file.format("%%%%BeginProlog" & vbLf, New Object(-1) {})
            file.format("/L { moveto lineto stroke } bind def" & vbLf, New Object(-1) {})
            file.format("/G { gsave } bind def" & vbLf, New Object(-1) {})
            file.format("/R { grestore } bind def" & vbLf, New Object(-1) {})
            file.format("/W { setlinewidth } bind def" & vbLf, New Object(-1) {})
            file.format("/D { setdash } bind def" & vbLf, New Object(-1) {})
            file.format("/Col { setrgbcolor } bind def" & vbLf, New Object(-1) {})
            file.format("/Zero_linewidth { 0.0 } def" & vbLf, New Object(-1) {})
            file.format("/Sphcol { 1 setgray } def" & vbLf, New Object(-1) {})
            file.format("/Sphere { newpath 3 copy 0 360 arc gsave Sphcol fill 0" & vbLf, New Object(-1) {})
            file.format("          setgray 0.5 setlinewidth 3 copy 0.94 mul" & vbLf, New Object(-1) {})
            file.format("          260 350 arc stroke 3 copy 0.87 mul 275 335" & vbLf, New Object(-1) {})
            file.format("          arc stroke 3 copy 0.79 mul 295 315 arc" & vbLf, New Object(-1) {})
            file.format("          stroke 3 copy 0.8 mul 115 135 arc 3 copy" & vbLf, New Object(-1) {})
            file.format("          0.6 mul 135 115 arcn closepath gsave 1 setgray" & vbLf, New Object(-1) {})
            file.format("          fill grestore stroke 3 copy 0.7 mul 115 135" & vbLf, New Object(-1) {})
            file.format("          arc stroke 3 copy 0.6 mul 124.9 125 arc" & vbLf, New Object(-1) {})
            file.format("          0.8 mul 125 125.1 arc stroke grestore stroke" & vbLf, New Object(-1) {})
            file.format("        } bind def" & vbLf, New Object(-1) {})
            file.format("/Poly3  { moveto lineto lineto fill grestore } bind def" & vbLf, New Object(-1) {})
            file.format("/Pl3    { 6 copy Poly3 moveto moveto moveto closepath " & vbLf, New Object(-1) {})
            file.format("          stroke } bind def" & vbLf, New Object(-1) {})
            file.format("/Pline3 { 6 copy Poly3 moveto lineto lineto closepath" & vbLf, New Object(-1) {})
            file.format("          stroke } bind def" & vbLf, New Object(-1) {})
            file.format("/Poly4  { moveto lineto lineto lineto fill grestore } " & vbLf, New Object(-1) {})
            file.format("          bind def" & vbLf, New Object(-1) {})
            file.format("/Pl4    { 8 copy Poly4 moveto moveto moveto moveto " & vbLf, New Object(-1) {})
            file.format("          closepath stroke } bind def" & vbLf, New Object(-1) {})
            file.format("/Pline4 { 8 copy Poly4 moveto lineto lineto lineto" & vbLf, New Object(-1) {})
            file.format("          closepath stroke } bind def" & vbLf, New Object(-1) {})
            file.format("/Circol { 1 setgray } def" & vbLf, New Object(-1) {})
            file.format("/Circle { gsave newpath 0 360 arc gsave Circol fill " & vbLf, New Object(-1) {})
            file.format("          grestore stroke grestore } bind def" & vbLf, New Object(-1) {})
            file.format("/Ocircle { gsave newpath 0 360 arc stroke grestore } bind def " & vbLf, New Object(-1) {})
            file.format("/Ucircle { gsave newpath 0 360 arc gsave Circol fill " & vbLf, New Object(-1) {})
            file.format("           grestore grestore } bind def" & vbLf, New Object(-1) {})
            file.format("/Arc    { newpath arc stroke newpath } bind def" & vbLf, New Object(-1) {})
            file.format("/Ellipse { gsave translate scale Circle grestore } ", New Object(-1) {})
            file.format("bind def" & vbLf, New Object(-1) {})
            file.format("/Oellipse { gsave translate scale Ocircle grestore } ", New Object(-1) {})
            file.format("bind def" & vbLf, New Object(-1) {})
            file.format("/Print  { /%s findfont exch scalefont setfont" & vbLf, New Object() {PSFontFamily})
            file.format("          show } bind def" & vbLf, New Object(-1) {})
            file.format("/Gprint { /Symbol findfont exch scalefont setfont show" & vbLf, New Object(-1) {})
            file.format("          } bind def" & vbLf, New Object(-1) {})
            file.format("/Center { dup /%s findfont exch scalefont" & vbLf, New Object() {PSFontFamily})
            file.format("          setfont exch stringwidth pop -2 div exch -3" & vbLf, New Object(-1) {})
            file.format("          div rmoveto } bind def" & vbLf, New Object(-1) {})
            file.format("/CenterRot90 {" & vbLf, New Object(-1) {})
            file.format("          dup /%s findfont exch scalefont" & vbLf, New Object() {PSFontFamily})
            file.format("          setfont exch stringwidth pop -2 div exch 3" & vbLf, New Object(-1) {})
            file.format("          div exch rmoveto } bind def" & vbLf, New Object(-1) {})
            file.format("/UncenterRot90 {" & vbLf, New Object(-1) {})
            file.format("          dup /%s findfont exch scalefont" & vbLf, New Object() {PSFontFamily})
            file.format("          setfont exch stringwidth } bind def" & vbLf, New Object(-1) {})
            file.format("/Rot90  { gsave currentpoint translate 90 rotate }" & vbLf, New Object(-1) {})
            file.format("          bind def" & vbLf, New Object(-1) {})
            file.format("/RotAngle  { gsave currentpoint translate rotate } bind def" & vbLf, New Object(-1) {})
            file.format("%%%%EndProlog" & vbLf, New Object(-1) {})
            file.format("%%%%BeginSetup" & vbLf, New Object(-1) {})
            file.format("1 setlinecap 1 setlinejoin 1 setlinewidth 0 setgray" & vbLf, New Object(-1) {})
            file.format(" [ ] 0 setdash newpath" & vbLf, New Object(-1) {})
            file.format("%%%%EndSetup" & vbLf, New Object(-1) {})
        End Sub

        Public Overridable Sub writePageHeaders(page As Integer, npages As Integer, bgColour As Color)
            file.format("%%%%Page: p%d %d" & vbLf, New Object() {Convert.ToInt32(page), Convert.ToInt32(page)})
            file.format("/LigPlusSave save def" & vbLf, New Object(-1) {})
            Dim [string] As String = CSng(xx1).ToString() & " " & CSng(xy1).ToString() & " moveto " & CSng(xx2).ToString() & " " & CSng(xy1).ToString() & " lineto " & CSng(xx2).ToString() & " " & CSng(xy2).ToString() & " lineto "
            file.format("%s" & vbLf, New Object() {[string]})
            [string] = CSng(xx1).ToString() & " " & CSng(xy2).ToString() & " lineto closepath"
            file.format("%s" & vbLf, New Object() {[string]})
            file.format("gsave 1.0000 setgray fill grestore" & vbLf, New Object(-1) {})
            file.format("stroke gsave" & vbLf, New Object(-1) {})
            psComment("Background" & vbLf)
            RGBcolour = bgColour
            writeCommand(30.0F, 50.0F, 550.0F, 50.0F, 550.0F, 780.0F, 30.0F, 780.0F, "Pl4", 2)
            lastColour = Nothing
            lastCircleColour = Nothing
            lastSphereColour = Nothing
            lastLineWidth = -1.0F
            LineWidth = 0.2F
        End Sub

        Public Overridable Sub writeLandscape()
            psComment("Landscape orientation" & vbLf)
            psRotate90(580.0F, 0.0F)
        End Sub

        Public Overridable Function convertLength(length As Single) As Single
            Return length
        End Function

        Public Overridable Function convertx(x As Single) As Single
            Return x + 30.0F
        End Function

        Public Overridable Function converty(y As Single) As Single
            Return y + 50.0F
        End Function

        Private Function format(x As Single, nDecimals As Integer) As String
            Return x.ToString($"F{nDecimals}")
        End Function

        Private Sub getLandscapeHeight()
            pageMinx = 50.0F
            pageMaxx = 780.0F
        End Sub

        Private WriteOnly Property LineWidth As Single
            Set(value As Single)
                If value <> lastLineWidth Then
                    writeCommand(value, "W", 2)
                End If
                lastLineWidth = value
            End Set
        End Property

        Private WriteOnly Property CircleColour As Color
            Set(value As Color)
                If value <> lastCircleColour Then
                    Dim red = value.R / 255.0F
                    Dim green = value.G / 255.0F
                    Dim blue = value.B / 255.0F
                    file.format("/Circol { " & format(red, 4) & " " & format(green, 4) & " " & format(blue, 4) & " setrgbcolor } def" & vbLf, New Object(-1) {})
                End If
                lastCircleColour = value
            End Set
        End Property

        Private WriteOnly Property RGBcolour As Color
            Set(value As Color)
                If value <> lastColour Then
                    Dim red = value.R / 255.0F
                    Dim green = value.G / 255.0F
                    Dim blue = value.B / 255.0F
                    writeCommand(red, green, blue, "setrgbcolor", 4)
                End If
                lastColour = value
            End Set
        End Property

        Private WriteOnly Property SphereColour As Color
            Set(value As Color)
                If value <> lastSphereColour Then
                    Dim red = value.R / 255.0F
                    Dim green = value.G / 255.0F
                    Dim blue = value.B / 255.0F
                    file.format("/Sphcol { " & format(red, 4) & " " & format(green, 4) & " " & format(blue, 4) & " setrgbcolor } def" & vbLf, New Object(-1) {})
                End If
                lastSphereColour = value
            End Set
        End Property

        Private Sub writeCommand(x As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(x As Single, y As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x, nDecimals) & " " & format(y, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(x As Single, y As Single, z As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x, nDecimals) & " " & format(y, nDecimals) & " " & format(z, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(x1 As Single, y1 As Single, x2 As Single, y2 As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x1, nDecimals) & " " & format(y1, nDecimals) & " " & format(x2, nDecimals) & " " & format(y2, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(x1 As Single, y1 As Single, x2 As Single, y2 As Single, z As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x1, nDecimals) & " " & format(y1, nDecimals) & " " & format(x2, nDecimals) & " " & format(y2, nDecimals) & " " & format(z, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(f As Single, f0 As Single, radius As Single, elongX As Single, elongY As Single, x As Single, y As Single, command As String, nDecimals As Integer)
            Dim [string] = format(f, nDecimals) & " " & format(f0, nDecimals) & " " & format(radius, nDecimals) & " " & format(elongX, nDecimals) & " " & format(elongY, nDecimals) & " " & format(x, nDecimals) & " " & format(y, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub

        Private Sub writeCommand(x1 As Single, y1 As Single, x2 As Single, y2 As Single, x3 As Single, y3 As Single, x4 As Single, y4 As Single, command As String, nDecimals As Integer)
            Dim [string] = format(x1, nDecimals) & " " & format(y1, nDecimals) & " " & format(x2, nDecimals) & " " & format(y2, nDecimals) & " " & format(x3, nDecimals) & " " & format(y3, nDecimals) & " " & format(x4, nDecimals) & " " & format(y4, nDecimals) & " " & command
            file.format("%s" & vbLf, New Object() {[string]})
        End Sub
    End Class

End Namespace
