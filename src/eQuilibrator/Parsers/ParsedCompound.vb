Imports SMRUCC.genomics.ComponentModel.EquaionModel

Namespace EquilibratorApi.Core.Parsers

    ''' <summary>
    ''' Represents a parsed compound with its coefficient and optional phase information
    ''' </summary>
    Public Class ParsedCompound : Implements ICompoundSpecies
        ''' <summary>
        ''' The compound identifier (name or formula)
        ''' </summary>
        Public Property CompoundId As String = String.Empty Implements ICompoundSpecies.Key

        ''' <summary>
        ''' The stoichiometric coefficient (default is 1)
        ''' </summary>
        Public Property Coefficient As Double = 1.0 Implements ICompoundSpecies.Stoichiometry

        ''' <summary>
        ''' Optional phase information (e.g., "aqueous", "gas", "solid")
        ''' </summary>
        Public Property Phase As String

        ''' <summary>
        ''' Optional compartment information
        ''' </summary>
        Public Property Compartment As String

        Public Overrides Function ToString() As String
            Dim coeff = If(Math.Abs(Coefficient - 1.0) < 0.0000000001, "", $"{Coefficient} ")
            Dim phase = If(Not Equals(Me.Phase, Nothing), $" ({Me.Phase})", "")
            Return $"{coeff}{CompoundId}{phase}"
        End Function
    End Class


End Namespace