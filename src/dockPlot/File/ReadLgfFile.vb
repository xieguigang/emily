Imports System.IO
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text.Parser

Namespace file

    Public Class ReadLgfFile
        Public Const NONE As Integer = -1

        Public Const FAILED As Integer = 0

        Public Const ALIGNMENT As Integer = 1

        Public Const LIGAND As Integer = 2

        Public Const CORA As Integer = 3

        Private fitTypeField As Integer = 0

        Private nEquivalences As Integer = 0

        Private equivalence As Integer() = New Integer(1) {}

        Private alignedChainsField As String = Nothing

        Public Sub New(ensemble As Ensemble, pdb As PDBEntry, fileName As String)
            Dim inputStream As StreamReader = Nothing
            Try
                inputStream = New StreamReader(fileName)
                Dim inputLine As Value(Of String) = ""
                While Not ((inputLine = inputStream.ReadLine()) Is Nothing)
                    Dim line_str As String = inputLine

                    If line_str.Substring(0, 8).Equals("FIT_TYPE", StringComparison.OrdinalIgnoreCase) Then
                        If line_str.Substring(9).Equals("ALIGNMENT", StringComparison.OrdinalIgnoreCase) Then
                            fitTypeField = 1
                            Continue While
                        End If
                        If line_str.Substring(9).Equals("LIGAND", StringComparison.OrdinalIgnoreCase) Then
                            fitTypeField = 2
                            Continue While
                        End If
                        If line_str.Substring(9).Equals("CORA", StringComparison.OrdinalIgnoreCase) Then
                            fitTypeField = 3
                        End If
                        Continue While
                    End If
                    If line_str.Substring(0, 8).Equals("ALIGNED=", StringComparison.OrdinalIgnoreCase) Then
                        alignedChainsField = line_str.Substring(8)
                        Continue While
                    End If
                    Dim token As New Scanner(line_str)
                    token.UseDelimiter(vbTab)
                    Dim value = New String(2) {}
                    For i = 0 To 2
                        value(i) = ""
                    Next
                    Dim iToken = 0

                    While token.HasNext() = True AndAlso iToken < 3
                        value(iToken) = token.Next()
                        iToken += 1
                    End While
                    Dim PDBId1 = Integer.Parse(value(0))
                    Dim residueId1 = value(1)
                    Dim residueId2 = value(2)
                    Dim pdb1 = ensemble.identifyPDBEntry(PDBId1)
                    Dim residue1 = pdb1.identifyResidue(residueId1)
                    Dim residue2 = pdb.identifyResidue(residueId2)
                    If residue1 IsNot Nothing AndAlso residue2 IsNot Nothing Then
                        residue1.storeResidueEquivalence(pdb, residue2)
                        residue2.storeResidueEquivalence(pdb1, residue1)
                    End If
                End While

            Finally
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
        End Sub

        Public Overridable ReadOnly Property FitType As Integer
            Get
                Return fitTypeField
            End Get
        End Property

        Public Overridable ReadOnly Property AlignedChains As String
            Get
                Return alignedChainsField
            End Get
        End Property
    End Class

End Namespace
