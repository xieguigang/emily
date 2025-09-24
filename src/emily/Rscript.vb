
Imports Emily.PDB_Canvas
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports Microsoft.VisualBasic.Serialization.JSON
Imports Microsoft.VisualBasic.Text
Imports SMRUCC.genomics.Data.RCSB.PDB
Imports SMRUCC.genomics.Data.RCSB.PDB.Keywords
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Interop

<Package("Rscript")>
<RTypeExport("ligand2d_style", GetType(RenderArguments))>
Module Rscript

    <ExportAPI("draw_pdb")>
    Public Function draw_pdb(pdb As PDB, <RRawVectorArgument> Optional size As Object = "3000,2100") As Object
        Return DrawingPDB.MolDrawing(pdb,)
    End Function

    <ExportAPI("parse_style")>
    Public Function ParseStyle(jsonstr As String) As RenderArguments
        Return jsonstr.LoadJSON(Of RenderArguments)
    End Function

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="pdb"></param>
    ''' <param name="ligand">
    ''' this function will treated the given <paramref name="pdb"/> object as vina dock
    ''' pdbqt object if this ligand reference is missing.
    ''' </param>
    ''' <param name="style"></param>
    ''' <param name="size"></param>
    ''' <param name="dpi"></param>
    ''' <param name="env"></param>
    ''' <returns></returns>
    <ExportAPI("draw_ligand2D")>
    Public Function draw_ligand2d(pdb As PDB,
                                  Optional ligand As Het.HETRecord = Nothing,
                                  Optional style As RenderArguments = Nothing,
                                  <RRawVectorArgument>
                                  Optional size As Object = "3600,2400",
                                  Optional dpi As Integer = 120,
                                  Optional env As Environment = Nothing) As Object

        Dim theme As New Theme With {.padding = "padding: 10% 10% 10% 10%;"}
        Dim render As New Ligand2DPlot(pdb, If(ligand, CType(pdb.GetLigandReference, Het.HETRecord)), theme)
        Dim plotSize As String = InteropArgumentHelper.getSize(size, env, [default]:="3600,2400")
        Dim driver As Drivers = env.getDriver

        If style Is Nothing Then
            style = New RenderArguments
        End If

        Call render.CalculateMaxPlainView()

        If style.viewX <> 0.0 OrElse style.viewY <> 0.0 OrElse style.viewZ <> 0.0 Then
            render.ViewPoint = New Drawing3D.Point3D(style.viewX, style.viewY, style.viewZ)
        End If

        render.ShowAtomLabel = style.ShowLigandAtomLabel
        render.FontSizeMax = style.AminoAcidFontSizeMax
        render.FontSizeMin = style.AminoAcidFontSizeMin
        render.AminoAcidSize = style.AminoAcidSize
        render.AtomSize = style.AtomSize
        render.DistanceCutoff = style.DistanceCutoff
        render.TopRank = style.TopRank

        Call render.Build3DModel()

        Return render.Plot(plotSize, dpi, driver)
    End Function

    ''' <summary>
    ''' parse the zdock output text
    ''' </summary>
    ''' <param name="str"></param>
    ''' <returns></returns>
    <ExportAPI("parse_zdock")>
    Public Function parse_zdock(str As String) As ZDockOut
        Return ZDockOut.Parse(str)
    End Function

    <ExportAPI("vina_split")>
    Public Function vina_split(dock_pdbqt As String) As String()
        Return dock_pdbqt _
            .Matches("MODEL\s+\d+(.*?)ENDMDL") _
            .Select(Function(pdbqt)
                        Dim lines = pdbqt.Trim(" "c, ASCII.TAB, ASCII.CR, ASCII.LF).LineTokens
                        lines = lines.Skip(1).Take(lines.Length - 1).ToArray
                        Return lines.JoinBy(vbCrLf)
                    End Function) _
            .ToArray
    End Function
End Module
