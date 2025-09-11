
Namespace ligplus

    Public Class SelectPDBCode
        Public Shared CANCEL As Integer = 0

        Public Shared PDB_SELECTED As Integer = 1

        Private optionField As Integer = CANCEL

        Private codeListField As String()



        Public Sub New(codeList As String())
            codeListField = codeList

        End Sub







        Public Overridable ReadOnly Property CodeList As String()
            Get
                Return codeListField
            End Get
        End Property



        Public Overridable ReadOnly Property [Option] As Integer
            Get
                Return optionField
            End Get
        End Property







    End Class

End Namespace
