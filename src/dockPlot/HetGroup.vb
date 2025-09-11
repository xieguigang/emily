Namespace ligplus
    Public Class HetGroup
        Private hetDescriptionField As String

        Private hetNameField As String

        Friend Sub New(hetName As String, hetDescription As String)
            hetNameField = hetName
            hetDescriptionField = hetDescription
        End Sub

        Public Overridable Sub appendHetDescription(description As String)
            hetDescriptionField += description
        End Sub

        Public Overridable ReadOnly Property HetDescription As String
            Get
                Return hetDescriptionField
            End Get
        End Property

        Public Overridable ReadOnly Property HetName As String
            Get
                Return hetNameField
            End Get
        End Property

        Public Overridable ReadOnly Property [Object] As Object
            Get
                Return Me
            End Get
        End Property
    End Class

End Namespace
