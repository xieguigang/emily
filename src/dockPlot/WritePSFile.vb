Imports Microsoft.VisualBasic.Imaging
Imports System.Collections.Generic
Imports System.Drawing
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class WritePSFile
        Private okField As Boolean = False

        Private fontFamily As String

        Public Sub New(fileName As String, ensemble As Ensemble, landscape As Boolean, allPlots As Boolean, separatePages As Boolean, splitScreen As Boolean)
            Dim selected = False
            Dim bgColour = Color.White
            Dim g As IGraphics = Nothing
            Dim page = 0
            Dim npages = 1
            Dim paintType = 1
            Dim selectedPDBEntry As PDBEntry = Nothing
            Dim psFile As PostScript = Nothing
            fontFamily = PlotArea.PSFontFamily
            Dim pdbEntryList As List(Of PDBEntry) = ensemble.PDBEntryList
            Dim selectedEntry = ensemble.SelectedEntry
            If ensemble.getnPDB() > 1 AndAlso allPlots AndAlso separatePages Then
                npages = ensemble.getnPDB()
            End If
            Dim plotList As List(Of PDBEntry) = New List(Of PDBEntry)()
            Dim i As Integer
            For i = 0 To pdbEntryList.Count - 1
                Dim wanted = True
                If i = selectedEntry AndAlso allPlots AndAlso Not separatePages AndAlso Not splitScreen Then
                    wanted = False
                    selectedPDBEntry = pdbEntryList(i)
                End If
                If wanted Then
                    plotList.Add(pdbEntryList(i))
                End If
            Next
            If selectedPDBEntry IsNot Nothing Then
                plotList.Add(selectedPDBEntry)
                selectedEntry = plotList.Count - 1
            End If
            selectedPDBEntry = Nothing
            If Not allPlots Then
                For i = 0 To pdbEntryList.Count - 1
                    If i = selectedEntry Then
                        selectedPDBEntry = pdbEntryList(i)
                    End If
                Next
            End If
            Dim scale = calculateScale(ensemble, selectedPDBEntry, landscape, allPlots, separatePages, splitScreen)
            Dim params = ensemble.DefaultParams
            If params IsNot Nothing Then
                bgColour = ligplus.Params.getBackgroundColour(params)
            End If
            Using out = New PrintStream(fileName)
                Dim today = Date.Now
                psFile = New PostScript(out, scale)
                Dim title = "PostScript output"
                psFile.writeMainHeaders(title, today.ToString(), npages, fontFamily)
                For j = 0 To plotList.Count - 1
                    If j = selectedEntry Then
                        selected = True
                    Else
                        selected = False
                    End If
                    If selected OrElse allPlots Then
                        If separatePages OrElse splitScreen Then
                            selected = True
                        End If
                        If page = 0 OrElse separatePages Then
                            page += 1
                            psFile.writePageHeaders(page, npages, bgColour)
                            If landscape Then
                                psFile.writeLandscape()
                            End If
                        End If
                        Dim pdb = plotList(j)
                        psFile.psComment("PDB entry: " & pdb.PlotLabel.Text)
                        If allPlots AndAlso separatePages Then
                            scale = calculateScale(ensemble, pdb, landscape, allPlots, separatePages, splitScreen)
                        End If
                        pdb.paintPDBEntry(paintType, g, psFile, scale, selected)
                        If j = plotList.Count - 1 OrElse separatePages Then
                            psFile.writeEndPage()
                        End If
                    End If
                Next

                okField = True
                psFile.writeClosingLines()
                out.Close()
            End Using
        End Sub

        Public Overridable ReadOnly Property OK As Boolean
            Get
                Return okField
            End Get
        End Property

        Private Function calculateScale(ensemble As Ensemble, selectedPDBEntry As PDBEntry, landscape As Boolean, allPlots As Boolean, separatePages As Boolean, splitScreen As Boolean) As Scale
            Dim coordsMax = New Single(2) {}
            Dim coordsMin = New Single(2) {}
            Dim scale As Scale = New Scale(Me, landscape)
            Dim nCoords As Integer = ensemble.updateMaxMinCoords()
            If selectedPDBEntry IsNot Nothing Then
                For icoord = 0 To 1
                    coordsMin(icoord) = selectedPDBEntry.getCoordsMin(icoord)
                    coordsMax(icoord) = selectedPDBEntry.getCoordsMax(icoord)
                Next
            Else
                coordsMax = ensemble.CoordsMax
                coordsMin = ensemble.CoordsMin
                Dim arrayOfFloat = ensemble.getSplitWidth(splitScreen, 0.3F)
            End If
            Dim maxMinCoords = ensemble.MaxMinCoords
            coordsMin(0) = maxMinCoords(0)
            coordsMin(1) = maxMinCoords(2)
            coordsMax(0) = maxMinCoords(1)
            coordsMax(1) = maxMinCoords(3)
            scale.calcScale(coordsMin, coordsMax, 0.3F)
            Return scale
        End Function
    End Class

End Namespace
