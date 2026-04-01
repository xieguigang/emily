' The MIT License (MIT)
'
' Copyright (c) 2013 Weizmann Institute of Science
' Copyright (c) 2018 Institute for Molecular Systems Biology, ETH Zurich
' Copyright (c) 2018 Novo Nordisk Foundation Center for Biosustainability,
' Technical University of Denmark
'
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
'
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
'
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.

Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorApi.Core.Parsers

Namespace EquilibratorApi.Core

    ''' <summary>
    ''' Main class for predicting Gibbs free energies of biochemical reactions.
    ''' Implements the Component Contribution method for thermodynamic calculations.
    ''' </summary>
    Public Class ComponentContribution
        ''' <summary>
        ''' The compound cache for looking up compound properties
        ''' </summary>
        Public ReadOnly Property Cache As CompoundCache

        ''' <summary>
        ''' The pH value for calculations
        ''' </summary>
        Public Property PH As Double = ThermodynamicConstants.DefaultPH

        ''' <summary>
        ''' The pMg value for calculations
        ''' </summary>
        Public Property PMg As Double = DefaultPMg

        ''' <summary>
        ''' The ionic strength in molar
        ''' </summary>
        Public Property IonicStrength As Double = DefaultIonicStrength

        ''' <summary>
        ''' The temperature in Kelvin
        ''' </summary>
        Public Property Temperature As Double = DefaultTemperature

        ''' <summary>
        ''' The reaction parser
        ''' </summary>
        Private ReadOnly _parser As ReactionParser = New ReactionParser()

        ''' <summary>
        ''' Creates a new ComponentContribution instance
        ''' </summary>
        Public Sub New()
            Cache = New CompoundCache()
        End Sub

        ''' <summary>
        ''' Creates a new ComponentContribution instance with a custom cache
        ''' </summary>
        ''' <paramname="cache">The compound cache to use</param>
        Public Sub New(cache As CompoundCache)
            Me.Cache = cache
        End Sub

        ''' <summary>
        ''' Gets a compound by its identifier
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>The compound, or null if not found</returns>
        Public Function GetCompound(compoundId As String) As Compound
            Return Cache.GetCompound(compoundId)
        End Function

        ''' <summary>
        ''' Searches for compounds by name or identifier
        ''' </summary>
        ''' <paramname="query">The search query</param>
        ''' <returns>A list of matching compounds</returns>
        Public Function SearchCompounds(query As String) As List(Of Compound)
            Return Cache.SearchCompounds(query)
        End Function

        ''' <summary>
        ''' Parses a reaction formula string
        ''' </summary>
        ''' <paramname="formula">The reaction formula</param>
        ''' <returns>A ParsedReaction object</returns>
        Public Function ParseFormula(formula As String) As ParsedReaction
            Return _parser.Parse(formula)
        End Function

        ''' <summary>
        ''' Creates a PhasedReaction from a formula string
        ''' </summary>
        ''' <paramname="formula">The reaction formula</param>
        ''' <returns>A PhasedReaction object</returns>
        Public Function Reaction(formula As String) As PhasedReaction
            Dim parsed = _parser.Parse(formula)
            Dim sparse = New Dictionary(Of PhasedCompound, Double)()

            Dim existing As Double = Nothing

            For Each reactant In parsed.Reactants
                Dim compound = Cache.GetPhasedCompound(reactant.CompoundId)
                If compound Is Nothing Then
                    ' Create a placeholder compound
                    compound = New PhasedCompound(reactant.CompoundId)
                End If

                sparse.TryGetValue(compound, existing)
                sparse(compound) = existing - reactant.Coefficient
            Next

            existing = Nothing

            For Each product In parsed.Products
                Dim compound = Cache.GetPhasedCompound(product.CompoundId)
                If compound Is Nothing Then
                    ' Create a placeholder compound
                    compound = New PhasedCompound(product.CompoundId)
                End If

                sparse.TryGetValue(compound, existing)
                sparse(compound) = existing + product.Coefficient
            Next

            Return New PhasedReaction(sparse, parsed.Arrow)
        End Function

        ''' <summary>
        ''' Calculates the standard transformed Gibbs energy for a reaction
        ''' </summary>
        ''' <paramname="reaction">The reaction</param>
        ''' <returns>The Gibbs energy result</returns>
        Public Function StandardDgPrime(reaction As PhasedReaction) As GibbsEnergyResult
            ' Calculate standard ΔG'° using component contribution method
            ' This is a simplified implementation
            Dim standardDg = 0.0
            Dim uncertainty = 0.0

            For Each compoundCoeff In reaction.Sparse
                Dim compound = compoundCoeff.Key
                Dim coeff = compoundCoeff.Value
                Dim compoundData = Cache.GetCompound(compound.CompoundId)
                If compoundData?.StandardFormationEnergy IsNot Nothing Then
                    standardDg += coeff * compoundData.StandardFormationEnergy.Value
                    uncertainty += Math.Abs(coeff) * 5.0 ' Simplified uncertainty estimation
                Else
                    ' Use a large uncertainty for unknown compounds
                    uncertainty += Math.Abs(coeff) * DefaultRmseInf
                End If
            Next

            ' Apply pH correction for reactions involving protons
            Dim netProtons = reaction.GetNetProtons()
            Dim phCorrection = netProtons * Ln10RT(Temperature) * PH
            standardDg += phCorrection

            ' Calculate physiological ΔG'
            Dim physiologicalCorrection = reaction.CalculatePhysiologicalCorrection()
            Dim physiologicalDg = standardDg + RT(Temperature) * physiologicalCorrection

            ' Calculate actual ΔG' with current concentrations
            Dim concentrationCorrection = reaction.CalculateConcentrationCorrection()
            Dim dgPrime = standardDg + RT(Temperature) * concentrationCorrection

            ' Calculate p-value (simplified)
            Dim pValue = CalculatePValue(dgPrime, uncertainty)

            Return New GibbsEnergyResult(standardDg, uncertainty, physiologicalDg, dgPrime, pValue, Temperature)
        End Function

        ''' <summary>
        ''' Calculates the standard transformed Gibbs energy for a reaction formula
        ''' </summary>
        ''' <paramname="formula">The reaction formula</param>
        ''' <returns>The Gibbs energy result</returns>
        Public Function StandardDgPrime(formula As String) As GibbsEnergyResult
            Dim reaction = Me.Reaction(formula)
            If reaction Is Nothing Then
                Throw New ArgumentException($"Could not parse reaction: {formula}")
            End If

            Return Me.StandardDgPrime(reaction)
        End Function

        ''' <summary>
        ''' Calculates the direction of a reaction at given conditions
        ''' </summary>
        ''' <paramname="reaction">The reaction</param>
        ''' <returns>The reaction direction (forward, reverse, or equilibrium)</returns>
        Public Function GetReactionDirection(reaction As PhasedReaction) As ReactionDirection
            Dim result = StandardDgPrime(reaction)

            If result.DgPrime.Value < -RT(Temperature) Then
                Return ReactionDirection.Forward
            ElseIf result.DgPrime.Value > RT(Temperature) Then
                Return ReactionDirection.Reverse
            Else
                Return ReactionDirection.Equilibrium
            End If
        End Function

        ''' <summary>
        ''' Calculates the p-value for a Gibbs energy estimate
        ''' </summary>
        Private Function CalculatePValue(dg As Double, uncertainty As Double) As Double
            ' Simplified p-value calculation using normal distribution
            If uncertainty <= 0 Then Return If(dg < 0, 1.0, 0.0)

            Dim z = -dg / uncertainty
            Return NormalCdf(z)
        End Function

        ''' <summary>
        ''' Standard normal cumulative distribution function
        ''' </summary>
        Private Shared Function NormalCdf(x As Double) As Double
            ' Approximation of the standard normal CDF
            Const a1 = 0.254829592
            Const a2 = -0.284496736
            Const a3 = 1.421413741
            Const a4 = -1.453152027
            Const a5 = 1.061405429
            Const p = 0.3275911

            Dim sign = If(x < 0, -1, 1)
            x = Math.Abs(x) / Math.Sqrt(2)

            Dim t = 1.0 / (1.0 + p * x)
            Dim y = 1.0 - ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t * Math.Exp(-x * x)

            Return 0.5 * (1.0 + sign * y)
        End Function

        ''' <summary>
        ''' Creates a stoichiometric matrix from reaction formulas
        ''' </summary>
        ''' <paramname="formulas">The reaction formulas</param>
        ''' <returns>A stoichiometric matrix as a 2D array</returns>
        Public Function CreateStoichiometricMatrix(formulas As IEnumerable(Of String)) As Double(,)
            Dim parsedReactions = formulas.[Select](Function(f) _parser.Parse(f)).ToList()
            Return CreateStoichiometricMatrix(parsedReactions)
        End Function

        ''' <summary>
        ''' Creates a stoichiometric matrix from parsed reactions
        ''' </summary>
        ''' <paramname="reactions">The parsed reactions</param>
        ''' <returns>A stoichiometric matrix as a 2D array</returns>
        Public Function CreateStoichiometricMatrix(reactions As IEnumerable(Of ParsedReaction)) As Double(,)
            Dim reactionList As List(Of ParsedReaction) = reactions.ToList()
            Dim allCompounds = New HashSet(Of String)()

            ' Collect all unique compounds
            For Each reaction As ParsedReaction In reactionList
                For Each compound In reaction.AllCompounds()
                    allCompounds.Add(compound.CompoundId)
                Next
            Next

            Dim compoundList = allCompounds.ToList()
            Dim matrix = New Double(compoundList.Count - 1, reactionList.Count - 1) {}

            For j = 0 To reactionList.Count - 1
                Dim stoichiometry = reactionList(j).GetStoichiometry()
                For Each compoundIdCoeff In stoichiometry
                    Dim compoundId = compoundIdCoeff.Key
                    Dim coeff = compoundIdCoeff.Value
                    Dim i = compoundList.IndexOf(compoundId)
                    If i >= 0 Then
                        matrix(i, j) = coeff
                    End If
                Next
            Next

            Return matrix
        End Function

        ''' <summary>
        ''' Gets the compound IDs used in the stoichiometric matrix
        ''' </summary>
        ''' <paramname="formulas">The reaction formulas</param>
        ''' <returns>List of compound IDs in order</returns>
        Public Function GetCompoundIds(formulas As IEnumerable(Of String)) As List(Of String)
            Dim allCompounds = New HashSet(Of String)()

            For Each formula In formulas
                Dim reaction = _parser.Parse(formula)
                For Each compound In reaction.AllCompounds()
                    allCompounds.Add(compound.CompoundId)
                Next
            Next

            Return allCompounds.ToList()
        End Function
    End Class

    ''' <summary>
    ''' Represents the direction of a reaction
    ''' </summary>
    Public Enum ReactionDirection
        ''' <summary>
        ''' Reaction proceeds in the forward direction
        ''' </summary>
        Forward

        ''' <summary>
        ''' Reaction proceeds in the reverse direction
        ''' </summary>
        Reverse

        ''' <summary>
        ''' Reaction is at or near equilibrium
        ''' </summary>
        Equilibrium
    End Enum


End Namespace
