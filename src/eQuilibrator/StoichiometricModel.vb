' The MIT License (MIT)
'
' Copyright (c) 2013 Weizmann Institute of Science
' Copyright (c) 2018-2020 Institute for Molecular Systems Biology, ETH Zurich
' Copyright (c) 2018-2020 Novo Nordisk Foundation Center for Biosustainability,
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


Imports System.Text

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' A basic stoichiometric model with thermodynamic constraints.
    ''' Represents a metabolic pathway or network with reactions and compounds.
    ''' </summary>
    Public Class StoichiometricModel
        ''' <summary>
        ''' The model identifier
        ''' </summary>
        Public Property Id As String

        ''' <summary>
        ''' The model name
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' The ComponentContribution instance for thermodynamic calculations
        ''' </summary>
        Public ReadOnly Property CompContrib As ComponentContribution

        ''' <summary>
        ''' The reactions in this model
        ''' </summary>
        Private ReadOnly _reactions As List(Of PhasedReaction) = New List(Of PhasedReaction)()

        ''' <summary>
        ''' The compounds in this model
        ''' </summary>
        Private ReadOnly _compounds As Dictionary(Of String, PhasedCompound) = New Dictionary(Of String, PhasedCompound)()

        ''' <summary>
        ''' The bounds for compound concentrations
        ''' </summary>
        Public Property Bounds As Bounds

        ''' <summary>
        ''' Gets the reactions in this model
        ''' </summary>
        Public ReadOnly Property Reactions As IReadOnlyList(Of PhasedReaction)
            Get
                Return _reactions
            End Get
        End Property

        ''' <summary>
        ''' Gets the compounds in this model
        ''' </summary>
        Public ReadOnly Property Compounds As IReadOnlyDictionary(Of String, PhasedCompound)
            Get
                Return _compounds
            End Get
        End Property

        ''' <summary>
        ''' Gets the compound IDs in this model
        ''' </summary>
        Public ReadOnly Property CompoundIds As IEnumerable(Of String)
            Get
                Return _compounds.Keys
            End Get
        End Property

        ''' <summary>
        ''' Creates a new StoichiometricModel
        ''' </summary>
        ''' <paramname="compContrib">The ComponentContribution instance</param>
        Public Sub New(compContrib As ComponentContribution)
            Me.CompContrib = compContrib
            Bounds = New Bounds()
        End Sub

        ''' <summary>
        ''' Creates a new StoichiometricModel with default settings
        ''' </summary>
        Public Sub New()
            Me.New(New ComponentContribution())
        End Sub

        ''' <summary>
        ''' Adds a reaction to the model
        ''' </summary>
        ''' <paramname="reaction">The reaction to add</param>
        Public Sub AddReaction(reaction As PhasedReaction)
            _reactions.Add(reaction)

            ' Add compounds from the reaction
            For Each compound In reaction.Compounds
                If Not _compounds.ContainsKey(compound.CompoundId) Then
                    _compounds(compound.CompoundId) = compound
                End If
            Next
        End Sub

        ''' <summary>
        ''' Adds a reaction from a formula string
        ''' </summary>
        ''' <paramname="formula">The reaction formula</param>
        ''' <paramname="reactionId">Optional reaction identifier</param>
        Public Sub AddReaction(formula As String, Optional reactionId As String = Nothing)
            Dim reaction = CompContrib.Reaction(formula)
            If reaction Is Nothing Then
                Throw New ArgumentException($"Could not parse reaction: {formula}")
            End If

            If Not Equals(reactionId, Nothing) Then
                reaction = New PhasedReaction(reaction.Sparse, reaction.Arrow, reactionId)
            End If

            Me.AddReaction(reaction)
        End Sub

        ''' <summary>
        ''' Adds multiple reactions to the model
        ''' </summary>
        ''' <paramname="reactions">The reactions to add</param>
        Public Sub AddReactions(reactions As IEnumerable(Of PhasedReaction))
            For Each reaction In reactions
                AddReaction(reaction)
            Next
        End Sub

        ''' <summary>
        ''' Gets a reaction by its identifier
        ''' </summary>
        ''' <paramname="reactionId">The reaction identifier</param>
        ''' <returns>The reaction, or null if not found</returns>
        Public Function GetReaction(reactionId As String) As PhasedReaction
            Return _reactions.FirstOrDefault(Function(r) Equals(r.ReactionId, reactionId))
        End Function

        ''' <summary>
        ''' Gets a compound by its identifier
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>The compound, or null if not found</returns>
        Public Function GetCompound(compoundId As String) As PhasedCompound
            Dim compound As PhasedCompound = Nothing
            Return If(_compounds.TryGetValue(compoundId, compound), compound, Nothing)
        End Function

        ''' <summary>
        ''' Creates the stoichiometric matrix for this model
        ''' </summary>
        ''' <returns>A 2D array representing the stoichiometric matrix</returns>
        Public Function GetStoichiometricMatrix() As Double(,)
            Dim compoundList = _compounds.Keys.ToList()
            Dim matrix = New Double(compoundList.Count - 1, _reactions.Count - 1) {}

            For j = 0 To _reactions.Count - 1
                For Each compoundCoeff In _reactions(j).Sparse
                    Dim compound = compoundCoeff.Key
                    Dim coeff = compoundCoeff.Value
                    Dim i = compoundList.IndexOf(compound.CompoundId)
                    If i >= 0 Then
                        matrix(i, j) = coeff
                    End If
                Next
            Next

            Return matrix
        End Function

        ''' <summary>
        ''' Gets the stoichiometric matrix with compound and reaction IDs
        ''' </summary>
        ''' <returns>A tuple of (matrix, compoundIds, reactionIds)</returns>
        Public Function GetStoichiometricMatrixWithLabels() As (Double(,), List(Of String), List(Of String))
            Dim compoundIds = _compounds.Keys.ToList()
            Dim reactionIds = _reactions.[Select](Function(r) If(r.ReactionId, $"R{_reactions.IndexOf(r) + 1}")).ToList()
            Dim matrix = GetStoichiometricMatrix()

            Return (matrix, compoundIds, reactionIds)
        End Function

        ''' <summary>
        ''' Calculates the standard Gibbs energies for all reactions
        ''' </summary>
        ''' <returns>A dictionary mapping reaction IDs to Gibbs energy results</returns>
        Public Function CalculateStandardDgPrimes() As Dictionary(Of String, GibbsEnergyResult)
            Dim results = New Dictionary(Of String, GibbsEnergyResult)()

            For Each reaction In _reactions
                Dim reactionId = If(reaction.ReactionId, $"R{_reactions.IndexOf(reaction) + 1}")
                results(reactionId) = CompContrib.StandardDgPrime(reaction)
            Next

            Return results
        End Function

        ''' <summary>
        ''' Gets the net stoichiometry for the model (sum of all reactions)
        ''' </summary>
        ''' <returns>A dictionary mapping compound IDs to net coefficients</returns>
        Public Function GetNetStoichiometry() As Dictionary(Of String, Double)
            Dim netStoichiometry = New Dictionary(Of String, Double)()
            Dim existing As Double = Nothing

            For Each reaction In _reactions
                For Each compoundCoeff In reaction.Sparse
                    Dim compound = compoundCoeff.Key
                    Dim coeff = compoundCoeff.Value
                    netStoichiometry.TryGetValue(compound.CompoundId, existing)
                    netStoichiometry(compound.CompoundId) = existing + coeff
                Next
            Next

            Return netStoichiometry
        End Function

        ''' <summary>
        ''' Gets the external metabolites (those with non-zero net stoichiometry)
        ''' </summary>
        ''' <returns>A dictionary mapping compound IDs to net coefficients</returns>
        Public Function GetExternalMetabolites() As Dictionary(Of String, Double)
            Return GetNetStoichiometry().Where(Function(kv) Math.Abs(kv.Value) > 0.0000000001).ToDictionary(Function(kv) kv.Key, Function(kv) kv.Value)
        End Function

        ''' <summary>
        ''' Gets the internal metabolites (those with zero net stoichiometry)
        ''' </summary>
        ''' <returns>A set of compound IDs</returns>
        Public Function GetInternalMetabolites() As HashSet(Of String)
            Dim netStoichiometry = GetNetStoichiometry()
            Return netStoichiometry.Where(Function(kv) Math.Abs(kv.Value) < 0.0000000001).[Select](Function(kv) kv.Key).ToHashSet()
        End Function

        ''' <summary>
        ''' Validates the model for mass balance
        ''' </summary>
        ''' <returns>A list of validation errors</returns>
        Public Function Validate() As List(Of String)
            Dim errors = New List(Of String)()

            ' Check for mass balance in each reaction
            For Each reaction In _reactions
                If Not reaction.IsBalanced() Then
                    Dim reactionId = If(reaction.ReactionId, $"R{_reactions.IndexOf(reaction) + 1}")
                    errors.Add($"Reaction {reactionId} is not mass-balanced")
                End If
            Next

            ' Check for charge balance in each reaction
            For Each reaction In _reactions
                Dim netCharge = reaction.GetNetCharge()
                If Math.Abs(netCharge) > 0.0000000001 Then
                    Dim reactionId = If(reaction.ReactionId, $"R{_reactions.IndexOf(reaction) + 1}")
                    errors.Add($"Reaction {reactionId} is not charge-balanced (net charge: {netCharge})")
                End If
            Next

            Return errors
        End Function

        ''' <summary>
        ''' Sets the concentration for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="concentration">The concentration in molar</param>
        Public Sub SetConcentration(compoundId As String, concentration As Double)
            Dim compound As PhasedCompound = Nothing

            If _compounds.TryGetValue(compoundId, compound) Then
                compound.SetConcentration(concentration)
            End If
        End Sub

        ''' <summary>
        ''' Sets concentrations for multiple compounds
        ''' </summary>
        ''' <paramname="concentrations">Dictionary mapping compound IDs to concentrations</param>
        Public Sub SetConcentrations(concentrations As Dictionary(Of String, Double))
            For Each compoundIdConcentration In concentrations
                Dim compoundId = compoundIdConcentration.Key
                Dim concentration = compoundIdConcentration.Value
                Me.SetConcentration(compoundId, concentration)
            Next
        End Sub

        ''' <summary>
        ''' Calculates the pathway Gibbs energy
        ''' </summary>
        ''' <returns>The total Gibbs energy for the pathway</returns>
        Public Function CalculatePathwayDgPrime() As Double
            Dim results = CalculateStandardDgPrimes()
            Return results.Values.Sum(Function(r) r.DgPrime.Value)
        End Function

        ''' <summary>
        ''' Gets a summary of the model
        ''' </summary>
        ''' <returns>A summary string</returns>
        Public Function GetSummary() As String
            Dim sb = New StringBuilder()
            sb.AppendLine($"Model: {If(Name, If(Id, "Unnamed"))}")
            sb.AppendLine($"Reactions: {_reactions.Count}")
            sb.AppendLine($"Compounds: {_compounds.Count}")
            sb.AppendLine($"Internal metabolites: {GetInternalMetabolites().Count}")
            sb.AppendLine($"External metabolites: {GetExternalMetabolites().Count}")

            Dim errors = Validate()
            If errors.Count > 0 Then
                sb.AppendLine($"Validation errors: {errors.Count}")
                For Each [error] As String In errors
                    sb.AppendLine($"  - {[error]}")
                Next
            End If

            Return sb.ToString()
        End Function

        Public Overrides Function ToString() As String
            Return $"StoichiometricModel: {If(Name, If(Id, "Unnamed"))} ({_reactions.Count} reactions, {_compounds.Count} compounds)"
        End Function
    End Class

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
