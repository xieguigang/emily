Namespace EquilibratorApi.Core.Parsers

    ''' <summary>
    ''' Represents a parsed reaction with reactants, products, and arrow type
    ''' </summary>
    Public Class ParsedReaction
        ''' <summary>
        ''' The reactants (left side of the reaction)
        ''' </summary>
        Public Property Reactants As List(Of ParsedCompound) = New List(Of ParsedCompound)()

        ''' <summary>
        ''' The products (right side of the reaction)
        ''' </summary>
        Public Property Products As List(Of ParsedCompound) = New List(Of ParsedCompound)()

        ''' <summary>
        ''' The arrow type used in the reaction
        ''' </summary>
        Public Property Arrow As String = "=>"

        ''' <summary>
        ''' Indicates whether the reaction is reversible
        ''' </summary>
        Public Function IsReversible() As Boolean
            Return Equals(Arrow, "<=>") OrElse Equals(Arrow, "<==>") OrElse Equals(Arrow, "⇌") OrElse Equals(Arrow, "↔")
        End Function

        ''' <summary>
        ''' Gets all compounds in the reaction
        ''' </summary>
        Public Function AllCompounds() As IEnumerable(Of ParsedCompound)
            Return Reactants.Concat(Products)
        End Function

        ''' <summary>
        ''' Gets the stoichiometry as a dictionary (negative for reactants, positive for products)
        ''' </summary>
        Public Function GetStoichiometry() As Dictionary(Of String, Double)
            Dim stoichiometry = New Dictionary(Of String, Double)()
            Dim existing As Double = Nothing

            For Each compound In Reactants
                Dim key = compound.CompoundId
                stoichiometry.TryGetValue(key, existing)
                stoichiometry(key) = existing - compound.Coefficient
            Next
            existing = Nothing

            For Each compound In Products
                Dim key = compound.CompoundId
                stoichiometry.TryGetValue(key, existing)
                stoichiometry(key) = existing + compound.Coefficient
            Next

            Return stoichiometry
        End Function

        Public Overrides Function ToString() As String
            Dim left = String.Join(" + ", Reactants)
            Dim right = String.Join(" + ", Products)
            Return $"{left} {Arrow} {right}"
        End Function
    End Class

End Namespace