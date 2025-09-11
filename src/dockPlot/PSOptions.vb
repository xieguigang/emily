
Namespace ligplus

    Public Class PSOptions

        Private cancelled_Conflict As Boolean = False

        Private landscapeField As Boolean = True

        Private separatePagesField As Boolean = True

        Private allPlotsField As Boolean = True


        Public Sub New(modal As Boolean, landscape As Boolean, separatePages As Boolean, allPlots As Boolean)
            landscapeField = landscape
            separatePagesField = separatePages
            allPlotsField = allPlots
        End Sub

        Public Overridable Function cancelled() As Boolean
            Return cancelled_Conflict
        End Function

        Public Overridable ReadOnly Property Landscape As Boolean
            Get
                Return landscapeField
            End Get
        End Property

        Public Overridable ReadOnly Property AllPlots As Boolean
            Get
                Return allPlotsField
            End Get
        End Property

        Public Overridable ReadOnly Property SeparatePages As Boolean
            Get
                Return separatePagesField
            End Get
        End Property
    End Class

End Namespace
