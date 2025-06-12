
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports PDB_canvas
Imports SMRUCC.genomics.Data.RCSB.PDB

<Package("Rscript")>
Module Rscript

    <ExportAPI("draw_pdb")>
    Public Function draw_pdb(pdb As PDB, Optional size As Object = "3000,2100") As Object
        Return DrawingPDB.MolDrawing(pdb,)
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
End Module
