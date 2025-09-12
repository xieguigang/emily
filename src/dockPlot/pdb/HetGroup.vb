Namespace pdb

    Public Class HetGroup

        Public Overridable ReadOnly Property HetDescription As String
        Public Overridable ReadOnly Property HetName As String

        Friend Sub New(hetName As String, hetDescription As String)
            _HetName = hetName
            _HetDescription = hetDescription
        End Sub

        Public Overridable Sub appendHetDescription(description As String)
            _HetDescription &= description
        End Sub

        Public Overrides Function ToString() As String
            Return $"[{HetName}] {HetDescription}"
        End Function
    End Class

End Namespace
