Imports System.IO
Imports Microsoft.VisualBasic.Language

Namespace ligplus

    Public Class ReadOSFile

        Private OSName_Conflict As String = Nothing

        Public Sub New(fileName As String)
            Dim inputStream As StreamReader = Nothing
            Console.WriteLine("... Reading " & fileName)
            Try
                inputStream = New StreamReader(fileName)
                Console.WriteLine("FILE OPENED")
                Dim inputLine As Value(Of String) = ""
                While Not (inputLine = inputStream.ReadLine()) Is Nothing
                    Dim line_str As String = inputLine

                    Console.WriteLine("LINE: " & line_str)
                    Console.WriteLine("  TO UPPER: " & line_str.ToUpper())
                    If line_str.ToUpper().Contains("WINDOWS") Then
                        OSName_Conflict = "Windows"
                        Console.WriteLine("  OSName: " & OSName_Conflict)
                    ElseIf line_str.ToUpper().Contains("LINUX") Then
                        OSName_Conflict = "linux"
                        If line_str.Contains("64") Then
                            OSName_Conflict = "linux64"
                        End If
                    ElseIf line_str.ToUpper().Contains("UNIX") Then
                        OSName_Conflict = "unix"
                    ElseIf line_str.ToUpper().Contains("MAC") Then
                        OSName_Conflict = "Mac"
                        If line_str.Contains("64") Then
                            OSName_Conflict = "Mac64"
                        End If
                    End If
                    Console.WriteLine("  Final OSName: " & OSName_Conflict)
                End While

            Finally
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
        End Sub

        Public Overridable ReadOnly Property OSName As String
            Get
                Console.WriteLine("  Returning OSName: " & OSName_Conflict)
                Return OSName_Conflict
            End Get
        End Property
    End Class

End Namespace
