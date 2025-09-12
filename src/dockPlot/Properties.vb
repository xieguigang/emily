Namespace ligplus

    Public Class Properties

        Public Property data As New Dictionary(Of String, String)

        Default Public Property Item(key As String) As String
            Get
                Return data.TryGetValue(key)
            End Get
            Set(value As String)
                data(key) = value
            End Set
        End Property

        Public Property antibodyLoopName As String()
        Public Property antibodyLoopID As String()

        Public ReadOnly Property Keys As IEnumerable(Of String)
            Get
                Return data.Keys
            End Get
        End Property

        Public Function ContainsKey(key As String) As Boolean
            Return data.ContainsKey(key)
        End Function

        Public Shared Function Load(filepath As String) As Properties
            Dim data As New Dictionary(Of String, String)

            For Each line As String In filepath.IterateAllLines
                If line.StringEmpty(, True) Then
                    Continue For
                End If
                If line.StartsWith("#"c) Then
                    Continue For
                End If

                With line.GetTagValue("=")
                    data(.Name) = .Value
                End With
            Next

            Return New Properties With {
                .data = data
            }
        End Function
    End Class
End Namespace
