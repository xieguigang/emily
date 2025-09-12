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
Imports Microsoft.VisualBasic.Imaging.Drawing3D
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.MIME.Html.CSS
Imports Microsoft.VisualBasic.MIME.Html.Render
Imports Microsoft.VisualBasic.Scripting.Runtime
Imports SMRUCC.genomics.Data.RCSB.PDB
Imports SMRUCC.genomics.Data.RCSB.PDB.Keywords

#If NET48 Then
Imports Brushes = System.Drawing.Brushes
#Else
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
#End If

Public Class Ligand2DPlot : Inherits Plot

    ReadOnly pdb As PDB
    ReadOnly target As Het.HETRecord
    ReadOnly hetAtoms As HETATM.HETATMRecord()
    ReadOnly atom As Atom

    Sub New(pdb As PDB, target As Het.HETRecord, theme As Theme)
        Call MyBase.New(theme)

        Me.pdb = pdb
        Me.target = target
        Me.hetAtoms = FindTarget(pdb, target, model:=atom)
    End Sub

    Private Shared Function FindTarget(pdb As PDB, target As Het.HETRecord, ByRef model As Atom) As HETATM.HETATMRecord()
        For Each atom As Atom In pdb.AtomStructures
            Dim hetatom As HETATM = atom.HetAtoms
            Dim key As String = $"{target.ResidueType}-{target.SequenceNumber}"

            If Not hetatom(key).IsNullOrEmpty Then
                model = atom
                Return hetatom(key)
            End If
        Next

        Throw New InvalidProgramException($"missing {target.ToString} het atom model data!")
    End Function

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim knn = hetAtoms _
            .AsParallel _
            .Select(Function(atom)
                        Return Me.atom.Atoms _
                            .Select(Function(a) (atom, aa:=a, dist:=a.Location.DistanceTo(atom.XCoord, atom.YCoord, atom.ZCoord))) _
                            .OrderBy(Function(a) a.dist) _
                            .Take(5) _
                            .ToArray
                    End Function) _
            .IteratesALL _
            .Where(Function(a) a.dist < 5) _
            .OrderBy(Function(a) a.dist) _
            .ToArray
        Dim atoms As New List(Of Element3D)
        Dim camera As New Camera(canvas, New Drawing3D.Point3D())
        Dim connect = pdb.Conect.AsEnumerable.ToDictionary(Function(a) a.name, Function(a) a.value)
        Dim atomIndex = hetAtoms.ToDictionary(Function(a) a.AtomNumber.ToString)
        Dim linkStroke As New Pen(Color.Green, 5)

        For Each link In connect
            For Each t2 In link.Value.AsCharacter
                If atomIndex.ContainsKey(link.Key) AndAlso atomIndex.ContainsKey(t2) Then
                    Call atoms.Add(New Plot3D.Device.Line(atomIndex(link.Key), atomIndex(t2)) With {
                        .Stroke = linkStroke
                    })
                End If
            Next
        Next

        For Each atom As IGrouping(Of String, (atom As HETATM.HETATMRecord, aa As AtomUnit, dist As Double)) In knn.GroupBy(Function(a) a.atom.AtomName)
            Dim ligand = atom.First.atom

            Call atoms.Add(New AtomModel With {
                .Fill = Brushes.Black,
                .IsResidue = False,
                .Label = ligand.ElementSymbol,
                .Size = New Size(20, 20),
                .Style = LegendStyles.Circle,
                .Location = New Drawing3D.Point3D(ligand.XCoord, ligand.YCoord, ligand.ZCoord)
            })

            For Each aa As AtomUnit In atom.Select(Function(t) t.aa)
                Call atoms.Add(New AtomModel With {
                    .Fill = Brushes.Red,
                    .IsResidue = True,
                    .Label = aa.AA_ID,
                    .Location = New Drawing3D.Point3D(aa.Location),
                    .Size = New Size(30, 30),
                    .Style = LegendStyles.Circle
                })
            Next
        Next

        For Each element As Element3D In atoms
            Call element.Transform(camera)
        Next

        Dim css As CSSEnvirnment = g.LoadEnvironment

        ' 进行投影之后只需要直接取出XY即可得到二维的坐标
        ' 然后生成多边形，进行画布的居中处理
        Dim plotRect As Rectangle = canvas.PlotRegion(css)
        Dim polygon As PointF() = atoms _
            .Select(Function(element) element.EnumeratePath) _
            .IteratesALL _
            .Select(Function(pt) pt.PointXY(plotRect.Size)) _
            .ToArray
        Dim scaleX = d3js.scale.linear.domain(polygon.Select(Function(a) a.X)).range(values:=New Double() {plotRect.Left, plotRect.Right})
        Dim scaleY = d3js.scale.linear.domain(polygon.Select(Function(a) a.Y)).range(values:=New Double() {plotRect.Top, plotRect.Bottom})
        Dim orders = PainterAlgorithm _
            .OrderProvider(atoms, Function(e) e.Location.Z) _
            .ToArray
        Dim fontSize As New DoubleRange(12, 36)
        Dim offset As New DoubleRange(0, orders.Length)

        ' 靠前的原子是比较远的原子
        For i As Integer = 0 To atoms.Count - 1
            Dim index As Integer = orders(i)
            Dim model As Element3D = atoms(index)

            If TypeOf model Is AtomModel Then
                DirectCast(model, AtomModel).fontSize = offset.ScaleMapping(i, fontSize)
            End If

            model.Draw(g, canvas, scaleX, scaleY)
        Next
    End Sub
End Class

Public Class AtomModel : Inherits ShapePoint

    Public Property IsResidue As Boolean
    Public Property fontSize As Double

    Public Overrides Sub Draw(g As IGraphics, rect As GraphicsRegion, scaleX As LinearScale, scaleY As LinearScale)
        Dim praw As PointF = GetPosition(rect.Size)
        Dim pscale As New PointF(scaleX(praw.X), scaleY(praw.Y))
        Dim font As New Font(FontFace.SegoeUI, FontFace.PointSizeScale(fontSize, g.Dpi))

        Call g.DrawLegendShape(pscale, Size, Style, Fill)
        Call g.DrawString(Label, font, Brushes.Blue, pscale)
    End Sub

End Class
