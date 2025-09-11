Imports System.IO
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text.Parser

Namespace file

    Friend Class ReadCORAFile
        Public Const NONE As Integer = -1

        Public Const ALN_FILE As Integer = 0

        Public Const CORA_FILE As Integer = 1

        Public Const CAF_FILE As Integer = 2

        Public Const FASTA_FILE As Integer = 3

        Private pdbCodeListField As List(Of Object) = Nothing

        Public Sub New(fileName As String)
            Dim fileType = -1
            pdbCodeListField = New List(Of Object)()
            Dim inputStream As StreamReader = Nothing
            Try
                inputStream = New StreamReader(fileName)
                If fileName.Contains(".aln") Then
                    fileType = 0
                    readCoraFile(inputStream)
                End If
                If fileName.Contains(".cora") Then
                    fileType = 1
                    readCoraFile(inputStream)
                End If
                If fileName.Contains(".caf") Then
                    fileType = 2
                    readCafFile(inputStream)
                End If
                If fileName.Contains(".fasta") Then
                    fileType = 3
                    readFastaFile(inputStream)
                End If

            Finally
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
        End Sub

        Public Overridable ReadOnly Property PDBCodeList As List(Of Object)
            Get
                Return pdbCodeListField
            End Get
        End Property

        Private Sub readCoraFile(inputStream As StreamReader)
            Dim ok = False
            Dim done = False
            Dim line = 0
            Dim inputLine As Value(Of String) = ""
            While Not ((inputLine = inputStream.ReadLine()) Is Nothing) AndAlso Not done
                Dim line_str As String = inputLine

                If line_str(0) <> "#"c Then
                    line += 1
                End If
                If line_str.Length > 17 AndAlso line_str.Substring(0, 18).Equals("#FM CORA_FORMAT 1.", StringComparison.OrdinalIgnoreCase) Then
                    ok = True
                    Continue While
                End If
                If ok AndAlso line = 2 Then
                    Dim token As New Scanner(line_str)
                    token.UseDelimiter(" ")
                    Dim iToken = 0

                    While token.HasNext() = True
                        pdbCodeListField.Add(token.Next().ToLower())
                        iToken += 1
                    End While
                End If
            End While
        End Sub

        Private Sub readCafFile(inputStream As StreamReader)
            Dim done = False
            Dim skip = False
            Dim nPDB = 0
            Dim inputLine As Value(Of String) = ""
            While Not ((inputLine = inputStream.ReadLine()) Is Nothing) AndAlso Not done
                Dim line_str As String = inputLine

                If line_str.Length > 4 AndAlso line_str.Substring(0, 4).Equals("pdb|", StringComparison.OrdinalIgnoreCase) Then
                    If Not skip Then
                        Dim token As New Scanner(line_str)
                        token.UseDelimiter(" ")
                        If token.HasNext() Then
                            pdbCodeListField.Add(token.Next().Substring(4).ToLower())
                            nPDB += 1
                            skip = True
                        End If
                        Continue While
                    End If
                    skip = False
                    Continue While
                End If
                If nPDB > 0 Then
                    done = True
                End If
            End While
        End Sub

        Private Sub readFastaFile(inputStream As StreamReader)
            Dim inputLine As Value(Of String) = ""
            While Not ((inputLine = inputStream.ReadLine()) Is Nothing)
                Dim line_str As String = inputLine

                If line_str(0) = ">"c Then
                    Dim len = line_str.Length
                    Dim iPos = 1
                    If len > 4 AndAlso line_str.Substring(0, 4).Equals(">pdb", StringComparison.OrdinalIgnoreCase) Then
                        iPos = 4
                    End If
                    If line_str(iPos) = "|"c Then
                        iPos += 1
                    End If
                    If iPos + 4 <= len Then
                        Dim chain = " "c
                        Dim pdbCode As String = line_str.Substring(iPos, 4).ToLower()
                        iPos += 4
                        If iPos < len AndAlso (line_str(iPos) = ":"c OrElse line_str(iPos) = "|"c OrElse line_str(iPos) = "("c) Then
                            iPos += 1
                        End If
                        If iPos < len Then
                            chain = line_str(iPos)
                        End If
                        If chain <> " "c Then
                            pdbCode = pdbCode & chain.ToString()
                        End If
                        pdbCodeListField.Add(pdbCode)
                    End If
                End If
            End While
        End Sub
    End Class

End Namespace
