
Imports ligplus.pdb

Namespace ligplus

    Public Class RunParamsSelection
        Private ensemble As Ensemble = Nothing



        Private parameters As Params = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private params As Dictionary(Of String, String) = Nothing





        Friend Sub New(ensemble As Ensemble, parameters As Params, ligplusParams As Dictionary(Of String, String))

            Me.ensemble = ensemble
            Me.ligplusParams = ligplusParams
            Me.parameters = parameters

        End Sub









    End Class

End Namespace
