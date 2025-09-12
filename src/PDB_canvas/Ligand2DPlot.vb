Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D
Imports Microsoft.VisualBasic.Imaging.Math2D
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.genomics.Data.RCSB.PDB
Imports SMRUCC.genomics.Data.RCSB.PDB.Keywords

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
            .ToArray

        For Each atom As IGrouping(Of String, (atom As HETATM.HETATMRecord, aa As AtomUnit, dist As Double)) In knn.GroupBy(Function(a) a.atom.AtomName)

        Next
    End Sub
End Class
