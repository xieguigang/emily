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

    Public Property HetAtomColors As New Dictionary(Of String, Color) From {
        {"C", Color.Black},
        {"N", Color.Blue},
        {"O", Color.Red},
        {"S", Color.Orange}
    }

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

    Private Function getAtomGroups() As IEnumerable(Of (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()))
        Dim hitSet As New List(Of (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()))

        For Each atom As HETATM.HETATMRecord In hetAtoms
            Dim filter = Me.atom.Atoms _
                .Select(Function(a) (aa:=a, dist:=a.Location.DistanceTo(atom.XCoord, atom.YCoord, atom.ZCoord))) _
                .Where(Function(a) a.dist < 3) _
                .OrderBy(Function(a) a.dist) _
                .Take(1) _
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

    Protected Overrides Sub PlotInternal(ByRef g As IGraphics, canvas As GraphicsRegion)
        Dim atoms As New List(Of Element3D)
        Dim camera As New Camera(canvas, New Drawing3D.Point3D()) With {.fov = 10000000}
        Dim connect = pdb.Conect.AsEnumerable.ToDictionary(Function(a) a.name, Function(a) a.value)
        Dim atomIndex = hetAtoms.ToDictionary(Function(a) a.AtomNumber.ToString)
        Dim linkStroke As New Pen(Color.Black, 30)
        Dim ligandStroke As New Pen(Color.LightGray, 5) With {.DashStyle = DashStyle.Dash}
        Dim atomSize As Single = 95
        Dim aminoAcidSize As Single = 150

        For Each link In connect
            For Each t2 In link.Value.AsCharacter
                If atomIndex.ContainsKey(link.Key) AndAlso atomIndex.ContainsKey(t2) Then
                    Call atoms.Add(New Plot3D.Device.Line(atomIndex(link.Key), atomIndex(t2)) With {
                        .Stroke = linkStroke
                    })
                End If
            Next
        Next

        Dim knn As (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)())() = getAtomGroups().ToArray

        For Each atom As (atom As HETATM.HETATMRecord, (aa As AtomUnit, dist As Double)()) In knn
            Dim ligand As HETATM.HETATMRecord = atom.atom

            Call atoms.Add(New AtomModel With {
                .Fill = If(HetAtomColors.ContainsKey(ligand.ElementSymbol), New SolidBrush(HetAtomColors(ligand.ElementSymbol)), Brushes.Black),
                .IsResidue = False,
                .Label = ligand.ElementSymbol,
                .Size = New Size(atomSize, atomSize),
                .Style = LegendStyles.Circle,
                .Location = New Drawing3D.Point3D(ligand.XCoord, ligand.YCoord, ligand.ZCoord)
            })

            For Each aa As AtomUnit In atom.Item2.Select(Function(t) t.aa)
                Dim chr As Char = Polypeptide.Abbreviate.TryGetValue(aa.AA_ID)
                Dim color As SolidBrush = Brushes.Black

                If chr <> ASCII.NUL Then
                    color = New SolidBrush(Polypeptide.MEGASchema(chr))
                End If

                Call atoms.Add(New AtomModel With {
                    .Fill = color,
                    .IsResidue = True,
                    .Label = $"{aa.AA_ID} {aa.Index}({aa.ChianID})",
                    .Location = New Drawing3D.Point3D(aa.Location),
                    .Size = New Size(aminoAcidSize, aminoAcidSize),
                    .Style = LegendStyles.Circle
                })
                Call atoms.Add(New Plot3D.Device.Line(ligand, aa.Location) With {
                    .Stroke = ligandStroke
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
        Dim fontSize As New DoubleRange(12, 40)
        Dim offset As New DoubleRange(0, orders.Length)

        ' rendering line at first
        For Each line In atoms.OfType(Of Plot3D.Device.Line)
            line.Draw(g, canvas, scaleX, scaleY)
        Next

        ' 靠前的原子是比较远的原子
        For i As Integer = 0 To atoms.Count - 1
            Dim index As Integer = orders(i)
            Dim model As Element3D = atoms(index)

            If TypeOf model Is Plot3D.Device.Line Then
                Continue For
            End If

            If TypeOf model Is AtomModel Then
                If DirectCast(model, AtomModel).IsResidue Then
                    DirectCast(model, AtomModel).fontSize = offset.ScaleMapping(i, fontSize)
                Else
                    DirectCast(model, AtomModel).fontSize = fontSize.Max
                End If
            End If

            model.Draw(g, canvas, scaleX, scaleY)
        Next
    End Sub
End Class

