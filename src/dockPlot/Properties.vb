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
    End Class
End Namespace
