Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Extension methods for StoichiometricModel
    ''' </summary>
    Public Module StoichiometricModelExtensions
        ''' <summary>
        ''' Creates a StoichiometricModel from a list of reaction formulas
        ''' </summary>
        ''' <paramname="formulas">The reaction formulas</param>
        ''' <paramname="compContrib">Optional ComponentContribution instance</param>
        ''' <returns>A new StoichiometricModel</returns>
        Public Function FromFormulas(formulas As IEnumerable(Of String), Optional compContrib As ComponentContribution = Nothing) As StoichiometricModel
            Dim model = New StoichiometricModel(If(compContrib, New ComponentContribution()))
            For Each formula In formulas
                model.AddReaction(formula)
            Next
            Return model
        End Function
    End Module
End Namespace