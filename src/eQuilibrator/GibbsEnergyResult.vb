Imports eQuilibrator.EquilibratorApi.Core.Constants

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Represents the result of a Gibbs energy calculation
    ''' </summary>
    Public Class GibbsEnergyResult
        ''' <summary>
        ''' The standard transformed Gibbs energy in kJ/mol
        ''' </summary>
        Public Property StandardDgPrime As Quantity

        ''' <summary>
        ''' The uncertainty (standard error) in kJ/mol
        ''' </summary>
        Public Property Uncertainty As Quantity

        ''' <summary>
        ''' The physiological Gibbs energy in kJ/mol
        ''' </summary>
        Public Property PhysiologicalDgPrime As Quantity

        ''' <summary>
        ''' The actual Gibbs energy at given concentrations in kJ/mol
        ''' </summary>
        Public Property DgPrime As Quantity

        ''' <summary>
        ''' The p-value for the reaction (probability of being thermodynamically feasible)
        ''' </summary>
        Public Property PValue As Double

        ''' <summary>
        ''' Indicates whether the reaction is thermodynamically feasible
        ''' </summary>
        Public ReadOnly Property IsFeasible As Boolean
            Get
                Return DgPrime.Value < 0
            End Get
        End Property

        ''' <summary>
        ''' The equilibrium constant
        ''' </summary>
        Public Property EquilibriumConstant As Double

        ''' <summary>
        ''' Creates a new GibbsEnergyResult
        ''' </summary>
        Public Sub New(standardDgPrime As Double, uncertainty As Double, physiologicalDgPrime As Double, dgPrime As Double, Optional pValue As Double = 0.5, Optional temperature As Double = ThermodynamicConstants.DefaultTemperature)
            Me.StandardDgPrime = New Quantity(standardDgPrime, "kJ/mol")
            Me.Uncertainty = New Quantity(uncertainty, "kJ/mol")
            Me.PhysiologicalDgPrime = New Quantity(physiologicalDgPrime, "kJ/mol")
            Me.DgPrime = New Quantity(dgPrime, "kJ/mol")
            Me.PValue = pValue

            ' Calculate equilibrium constant: K = exp(-ΔG'°/RT)
            EquilibriumConstant = Math.Exp(-standardDgPrime / RT(temperature))
        End Sub

        Public Overrides Function ToString() As String
            Return $"ΔG'° = {StandardDgPrime}, ΔG' = {DgPrime} ± {Uncertainty}"
        End Function
    End Class
End Namespace