Imports System.Drawing
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Data.ChartPlots
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Legend
Imports Microsoft.VisualBasic.Data.ChartPlots.Plot3D.Device
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.d3js.scale
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Text.Nudge
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports Microsoft.VisualBasic.MIME.Html.Render
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.genomics.Data.RCSB.PDB
Imports SMRUCC.genomics.Data.RCSB.PDB.Keywords
Imports SMRUCC.genomics.SequenceModel.Polypeptides

#If NET48 Then
Imports Brushes = System.Drawing.Brushes
Imports DashStyle = System.Drawing.Drawing2D.DashStyle
#Else
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
Imports DashStyle = Microsoft.VisualBasic.Imaging.DashStyle
#End If

Public Class Ligand2DPlot : Inherits Plot

    ReadOnly pdb As PDB
    ReadOnly target As Het.HETRecord
    ReadOnly hetAtoms As HETATM.HETATMRecord()
    ReadOnly atom As Atom

    Public Property HetAtomColors As Dictionary(Of String, Color) = CPKColors _
        .LoadColors _
        .ToDictionary(Function(a) a.symbol,
                      Function(a)
                          Return a.color.TranslateColor
                      End Function)
    Public Property ViewPoint As New Drawing3D.Point3D
    Public Property DistanceCutoff As Double = 3.5
    Public Property TopRank As Integer = 1
    Public Property AtomSize As Single = 95
    Public Property AminoAcidSize As Single = 150

    Public Property TextNudge As Boolean = False
    Public Property ShowAtomLabel As Boolean = True

    Public Property FontSizeMin As Double = 12
    Public Property FontSizeMax As Double = 40

    Sub New(pdb As PDB, target As Het.HETRecord, theme As Theme)
        Call MyBase.New(theme)

        Me.pdb = pdb
        Me.target = target
        Me.hetAtoms = FindTarget(pdb, target, model:=atom)

        Call Build3DModel()
    End Sub

    Private Shared Function FindTarget(pdb As PDB, target As Het.HETRecord, ByRef model As Atom) As HETATM.HETATMRecord()
        For Each atom As Atom In pdb.AtomStructures
            Dim hetatom As HETATM = atom.HetAtoms
            Dim key As String = $"{target.ResidueType}-{target.SequenceNumber}"

            If hetatom Is Nothing Then
                ' find in atoms
                Dim filter = atom.Atoms _
                    .Where(Function(a)
                               Return target.SequenceNumber = a.AA_IDX AndAlso
                                   a.AA_ID = target.ResidueType
                           End Function) _
                    .ToArray

                If Not filter.IsNullOrEmpty Then
                    model = atom
                    Return filter _
                        .Select(Function(a) New HETATM.HETATMRecord(a)) _
                        .ToArray
                End If
            Else
                If Not hetatom(key).IsNullOrEmpty Then
                    model = atom
                    Return hetatom(key)
                End If
            End If
        Next

        Throw New InvalidProgramException($"missing {target.ToString} het atom model data!")
    End Function

    Private Function getAtomGroups() As IEnumerable(Of (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()))
        Dim hitSet As New List(Of (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()))

        For Each atom As HETATM.HETATMRecord In hetAtoms
            Dim filter = Me.atom.Atoms _
                .Select(Function(a) (aa:=a, dist:=a.Location.DistanceTo(atom.XCoord, atom.YCoord, atom.ZCoord))) _
                .Where(Function(a) a.dist < DistanceCutoff) _
                .OrderBy(Function(a) a.dist) _
                .Take(TopRank) _
                .ToArray

            If filter.IsNullOrEmpty Then
                ' there is no docking for this atom
                ' returns a place holder
                Call hitSet.Add((atom, {}))
            Else
                Call hitSet.Add((atom, filter))
            End If
        Next

        Return hitSet
    End Function

    Dim models As New List(Of Element3D)

    Public Function CalculateMaxPlainView() As Drawing3D.Point3D
        Dim pointCloud As Drawing3D.Point3D() = models.Select(Function(m) m.Location).ToArray
        Dim maxView As Drawing3D.Point3D = MaxPlainCameraView.CalculateOptimalCameraView(pointCloud)

        ViewPoint = maxView

        Return ViewPoint
    End Function

    Private Sub MakeEdgeModel()
        Dim atomIndex As Dictionary(Of String, HETATM.HETATMRecord) = hetAtoms _
            .GroupBy(Function(a) a.AtomNumber.ToString) _
            .ToDictionary(Function(a) a.Key,
                          Function(a)
                              Return a.First
                          End Function)
        Dim connect As Dictionary(Of String, Integer()) = pdb.Conect _
            .AsEnumerable _
            .ToDictionary(Function(a) a.name,
                          Function(a)
                              Return a.value
                          End Function)
        Dim linkStroke As New Pen(Color.Black, 30)

        For Each link In connect
            For Each t2 In link.Value.AsCharacter
                If atomIndex.ContainsKey(link.Key) AndAlso atomIndex.ContainsKey(t2) Then
                    Call models.Add(New Plot3D.Device.Line(atomIndex(link.Key), atomIndex(t2)) With {
                        .Stroke = linkStroke
                    })
                End If
            Next
        Next

        If models.IsNullOrEmpty Then
            ' needs calculated based on the covalent radius
            Call CalculateEdgeModels(linkStroke, atomIndex)
        End If
    End Sub

    Private Sub CalculateEdgeModels(linkStroke As Pen, atomIndex As Dictionary(Of String, HETATM.HETATMRecord))
        Dim connections = CovalentRadii.MeasureBonds(atomIndex.Values.ToArray).ToArray

        For Each link As ConnectBond In connections
            Call models.Add(New Plot3D.Device.Line(link.atom1, link.atom2) With {
                .Stroke = linkStroke
            })
        Next
    End Sub

    Public Sub Build3DModel()
        Dim ligandStroke As New Pen(Color.LightGray, 5) With {.DashStyle = DashStyle.Dash}
        Dim knn As (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)())() = getAtomGroups().ToArray

        Call models.Clear()
        Call MakeEdgeModel()

        For Each atom As (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()) In knn
            Dim ligand As HETATM.HETATMRecord = atom.atom
            Dim ligandColor As Brush = Brushes.Black

            If (HetAtomColors.ContainsKey(ligand.ElementSymbol)) Then
                ligandColor = New SolidBrush(HetAtomColors(ligand.ElementSymbol))
            Else
                Call $"missing color schema for atom: {ligand.ElementSymbol}".warning
            End If

            Call models.Add(New AtomModel With {
                .Fill = ligandColor,
                .IsResidue = False,
                .Label = ligand.ElementSymbol,
                .Size = New Size(AtomSize, AtomSize),
                .Style = LegendStyles.Circle,
                .Location = New Drawing3D.Point3D(ligand.XCoord, ligand.YCoord, ligand.ZCoord)
            })

            For Each aa As AtomUnit In atom.Item2.Select(Function(t) t.aa)
                Dim chr As Char = Polypeptide.Abbreviate.TryGetValue(aa.AA_ID)
                Dim color As SolidBrush = Brushes.Black

                If chr <> ASCII.NUL Then
                    color = New SolidBrush(Polypeptide.MEGASchema(chr))
                End If

                Call models.Add(New AtomModel With {
                    .Fill = color,
                    .IsResidue = True,
                    .Label = $"{aa.AA_ID} {aa.Index}({aa.ChianID})",
                    .Location = New Drawing3D.Point3D(aa.Location),
                    .Size = New Size(AminoAcidSize, AminoAcidSize),
                    .Style = LegendStyles.Circle
                })
                Call models.Add(New Plot3D.Device.Line(ligand, aa.Location) With {
                    .Stroke = ligandStroke
                })
            Next
        Next
    End Sub

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim camera As New Camera(canvas, ViewPoint) With {.fov = 10000000}
        Dim norm As New List(Of Element3D)

        For Each element As Element3D In models
            Call norm.Add(element.Transform(camera))
        Next

        Dim css As CSSEnvirnment = g.LoadEnvironment

        ' 进行投影之后只需要直接取出XY即可得到二维的坐标
        ' 然后生成多边形，进行画布的居中处理
        Dim plotRect As Rectangle = canvas.PlotRegion(css)
        Dim polygon As PointF() = norm _
            .Select(Function(element) element.EnumeratePath) _
            .IteratesALL _
            .Select(Function(pt) pt.PointXY(plotRect.Size)) _
            .ToArray
        Dim scaleX = d3js.scale.linear.domain(polygon.Select(Function(a) a.X)).range(values:=New Double() {plotRect.Left, plotRect.Right})
        Dim scaleY = d3js.scale.linear.domain(polygon.Select(Function(a) a.Y)).range(values:=New Double() {plotRect.Top, plotRect.Bottom})
        Dim orders = PainterAlgorithm _
            .OrderProvider(norm, Function(e) e.Location.Z) _
            .ToArray
        Dim fontSize As New DoubleRange(FontSizeMin, FontSizeMax)
        Dim offset As New DoubleRange(0, orders.Length)

        ' rendering line at first
        For Each line In norm.OfType(Of Plot3D.Device.Line)
            Call line.Draw(g, canvas, scaleX, scaleY)
        Next

        Dim text As New List(Of (AtomModel, PointF))

        ' 靠前的原子是比较远的原子
        For i As Integer = 0 To norm.Count - 1
            Dim index As Integer = orders(i)
            Dim model As Element3D = norm(index)

            If TypeOf model Is Plot3D.Device.Line Then
                Continue For
            End If

            If TypeOf model Is AtomModel Then
                Dim isResidue As Boolean = DirectCast(model, AtomModel).IsResidue

                If isResidue Then
                    DirectCast(model, AtomModel).fontSize = offset.ScaleMapping(i, fontSize)
                Else
                    DirectCast(model, AtomModel).fontSize = fontSize.Min
                End If

                DirectCast(model, AtomModel).IsResidue = False

                If isResidue Then
                    text.Add((DirectCast(model, AtomModel), DirectCast(model, AtomModel).TextLocation(g, canvas, scaleX, scaleY)))
                End If

                If ShowAtomLabel AndAlso Not isResidue Then
                    DirectCast(model, AtomModel).IsResidue = True
                    DirectCast(model, AtomModel).LabelColor = Color.White
                End If
            End If

            model.Draw(g, canvas, scaleX, scaleY)
        Next

        If text.Any Then
            Dim graphics As IGraphics = g
            Dim labels As List(Of d3js.Layout.Label) = text _
                .Select(Function(t) New d3js.Layout.Label(t.Item1.Label, t.Item2, t.Item1.TextSize(graphics))) _
                .ToList

            If TextNudge Then
                labels = SimpleNudge.ReduceOverlap(labels)
            End If

            For i As Integer = 0 To labels.Count - 1
                With labels(i)
                    Call text(i).Item1.DrawText(graphics, .X, .Y)
                End With
            Next
        End If
    End Sub
End Class

