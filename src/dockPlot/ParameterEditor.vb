
Namespace ligplus

    Public Class ParameterEditor
        Private nDir As Integer

        Private defaultParams As Dictionary(Of String, String) = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private parameters As Params = Nothing



        Public haveTmpDir As Boolean = True





        Public Sub New(parameters As Params, ligplusParams As Dictionary(Of String, String))


            Me.parameters = parameters
            Me.ligplusParams = ligplusParams

        End Sub






    End Class

End Namespace
