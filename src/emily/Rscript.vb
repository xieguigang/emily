
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
End Module
