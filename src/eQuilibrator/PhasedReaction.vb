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
Imports eQuilibrator.EquilibratorApi.Core.Parsers

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Represents a chemical reaction with phase information for thermodynamic calculations.
    ''' Contains stoichiometry, reaction direction, and methods for Gibbs energy calculations.
    ''' </summary>
    Public Class PhasedReaction
        ''' <summary>
        ''' The stoichiometry of the reaction as a sparse vector.
        ''' Negative values indicate reactants, positive values indicate products.
        ''' </summary>
        Public ReadOnly Property Sparse As Dictionary(Of PhasedCompound, Double)

        ''' <summary>
        ''' The arrow type used in the reaction formula
        ''' </summary>
        Public ReadOnly Property Arrow As String

        ''' <summary>
        ''' The reaction identifier
        ''' </summary>
        Public Property ReactionId As String

        ''' <summary>
        ''' The reaction name
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' Indicates whether the reaction is reversible
        ''' </summary>
        Public Function IsReversible() As Boolean
            Return Equals(Arrow, "<=>") OrElse Equals(Arrow, "<==>") OrElse Equals(Arrow, "⇌") OrElse Equals(Arrow, "↔")
        End Function

        ''' <summary>
        ''' Gets all compounds in this reaction
        ''' </summary>
        Public ReadOnly Property Compounds As IEnumerable(Of PhasedCompound)
            Get
                Return Sparse.Keys
            End Get
        End Property

        ''' <summary>
        ''' Gets the reactants (compounds with negative coefficients)
        ''' </summary>
        Public ReadOnly Property Reactants As IEnumerable(Of (compound As PhasedCompound, coefficient As Double))
            Get
                Return Sparse.Where(Function(kv) kv.Value < 0).Select(Function(kv) (kv.Key, -kv.Value))
            End Get
        End Property

        ''' <summary>
        ''' Gets the products (compounds with positive coefficients)
        ''' </summary>
        Public ReadOnly Property Products As IEnumerable(Of (compound As PhasedCompound, coefficient As Double))
            Get
                Return Sparse.Where(Function(kv) kv.Value > 0).Select(Function(kv) (kv.Key, kv.Value))
            End Get
        End Property

        ''' <summary>
        ''' Creates a new PhasedReaction
        ''' </summary>
        ''' <paramname="sparse">The stoichiometry dictionary</param>
        ''' <paramname="arrow">The arrow type</param>
        ''' <paramname="rid">Optional reaction identifier</param>
        Public Sub New(sparse As Dictionary(Of PhasedCompound, Double), Optional arrow As String = "=>", Optional rid As String = Nothing)
            Me.Sparse = sparse
            Me.Arrow = If(arrow, "=>")
            ReactionId = rid
        End Sub

        ''' <summary>
        ''' Creates a PhasedReaction from a stoichiometry dictionary with compound IDs
        ''' </summary>
        ''' <paramname="sparse">Dictionary mapping compound IDs to coefficients</param>
        ''' <paramname="compoundLookup">Function to lookup compounds by ID</param>
        ''' <paramname="arrow">The arrow type</param>
        ''' <paramname="rid">Optional reaction identifier</param>
        Public Shared Function FromSparse(sparse As Dictionary(Of String, Double), compoundLookup As Func(Of String, PhasedCompound), Optional arrow As String = "=>", Optional rid As String = Nothing) As PhasedReaction
            Dim phasedSparse = sparse.ToDictionary(Function(kv) compoundLookup(kv.Key), Function(kv) kv.Value)

            Return New PhasedReaction(phasedSparse, arrow, rid)
        End Function

        ''' <summary>
        ''' Parses a reaction formula string and creates a PhasedReaction
        ''' </summary>
        ''' <paramname="formula">The reaction formula</param>
        ''' <paramname="compoundLookup">Function to lookup compounds by ID</param>
        ''' <returns>A new PhasedReaction</returns>
        Public Shared Function Parse(formula As String, compoundLookup As Func(Of String, PhasedCompound)) As PhasedReaction
            Dim parser = New ReactionParser()
            Dim parsed = parser.Parse(formula)

            Dim sparse = New Dictionary(Of PhasedCompound, Double)()
            Dim existing As Double = Nothing

            For Each reactant In parsed.Reactants
                Dim compound = compoundLookup(reactant.CompoundId)
                sparse(compound) = If(sparse.TryGetValue(compound, existing), existing - reactant.Coefficient, -reactant.Coefficient)
            Next
            existing = Nothing

            For Each product In parsed.Products
                Dim compound = compoundLookup(product.CompoundId)
                sparse(compound) = If(sparse.TryGetValue(compound, existing), existing + product.Coefficient, product.Coefficient)
            Next

            Return New PhasedReaction(sparse, parsed.Arrow)
        End Function

        ''' <summary>
        ''' Gets the coefficient for a specific compound
        ''' </summary>
        ''' <paramname="compound">The compound to look up</param>
        ''' <returns>The stoichiometric coefficient</returns>
        Public Function GetCoefficient(compound As PhasedCompound) As Double
            Dim coeff As Double = Nothing
            Return If(Sparse.TryGetValue(compound, coeff), coeff, 0.0)
        End Function

        ''' <summary>
        ''' Checks if the reaction contains a specific compound
        ''' </summary>
        ''' <paramname="compound">The compound to check</param>
        ''' <returns>True if the compound is in the reaction</returns>
        Public Function ContainsCompound(compound As PhasedCompound) As Boolean
            Return Sparse.ContainsKey(compound)
        End Function

        ''' <summary>
        ''' Gets the net atom balance for this reaction
        ''' </summary>
        ''' <returns>Dictionary mapping element symbols to net counts</returns>
        Public Function GetAtomBalance() As Dictionary(Of String, Double)
            Dim balance = New Dictionary(Of String, Double)()
            Dim existing As Double = Nothing

            For Each compoundCoeff In Sparse
                Dim compound = compoundCoeff.Key
                Dim coeff = compoundCoeff.Value
                For Each elementCount In compound.AtomBag
                    Dim element = elementCount.Key
                    Dim count = elementCount.Value
                    balance.TryGetValue(element, existing)
                    balance(element) = existing + coeff * count
                Next
            Next

            Return balance
        End Function

        ''' <summary>
        ''' Checks if the reaction is atom-balanced
        ''' </summary>
        ''' <paramname="tolerance">Tolerance for floating point comparison</param>
        ''' <returns>True if the reaction is balanced</returns>
        Public Function IsBalanced(Optional tolerance As Double = 0.000001) As Boolean
            Return GetAtomBalance().Values.All(Function(v) Math.Abs(v) < tolerance)
        End Function

        ''' <summary>
        ''' Gets the net charge of this reaction
        ''' </summary>
        ''' <returns>The net charge</returns>
        Public Function GetNetCharge() As Double
            Return Sparse.Sum(Function(kv) kv.Value * kv.Key.Charge)
        End Function

        ''' <summary>
        ''' Gets the net proton count for this reaction
        ''' </summary>
        ''' <returns>The net proton count</returns>
        Public Function GetNetProtons() As Double
            Return Sparse.Sum(Function(kv) kv.Value * kv.Key.ProtonCount)
        End Function

        ''' <summary>
        ''' Calculates the concentration correction for Gibbs energy
        ''' </summary>
        ''' <returns>The correction in RT units</returns>
        Public Function CalculateConcentrationCorrection() As Double
            Return Sparse.Sum(Function(kv) kv.Value * kv.Key.LnAbundance)
        End Function

        ''' <summary>
        ''' Calculates the physiological concentration correction
        ''' </summary>
        ''' <returns>The correction in RT units</returns>
        Public Function CalculatePhysiologicalCorrection() As Double
            Return Sparse.Sum(Function(kv) kv.Value * kv.Key.LnPhysiologicalAbundance)
        End Function

        ''' <summary>
        ''' Sets the concentration for a compound in this reaction
        ''' </summary>
        ''' <paramname="compound">The compound</param>
        ''' <paramname="concentration">The concentration in molar</param>
        Public Sub SetCompoundConcentration(compound As PhasedCompound, concentration As Double)
            If Sparse.ContainsKey(compound) Then
                compound.SetConcentration(concentration)
            End If
        End Sub

        ''' <summary>
        ''' Sets concentrations for multiple compounds
        ''' </summary>
        ''' <paramname="concentrations">Dictionary mapping compounds to concentrations</param>
        Public Sub SetConcentrations(concentrations As Dictionary(Of PhasedCompound, Double))
            For Each compoundConc In concentrations
                Dim compound = compoundConc.Key
                Dim conc = compoundConc.Value
                Me.SetCompoundConcentration(compound, conc)
            Next
        End Sub

        ''' <summary>
        ''' Creates a reversed version of this reaction
        ''' </summary>
        ''' <returns>A new PhasedReaction with reversed stoichiometry</returns>
        Public Function Reverse() As PhasedReaction
            Dim reversedSparse = Sparse.ToDictionary(Function(kv) kv.Key, Function(kv) -kv.Value)
            Dim reverseMap = New Dictionary(Of String, String)() From {
                {"=>", "<="},
                {"->", "<-"},
                {"→", "←"},
                {"<=", "=>"},
                {"<-", "->"},
                {"←", "→"}
            }

            Dim reversedArrow = If(reverseMap.ContainsKey(Arrow), reverseMap(Arrow), Arrow) ' Reversible arrows stay the same

            Return New PhasedReaction(reversedSparse, reversedArrow, ReactionId)
        End Function

        ''' <summary>
        ''' Scales the reaction by a factor
        ''' </summary>
        ''' <paramname="factor">The scaling factor</param>
        ''' <returns>A new scaled PhasedReaction</returns>
        Public Function Scale(factor As Double) As PhasedReaction
            Dim scaledSparse = Sparse.ToDictionary(Function(kv) kv.Key, Function(kv) kv.Value * factor)

            Return New PhasedReaction(scaledSparse, Arrow, ReactionId)
        End Function

        ''' <summary>
        ''' Combines this reaction with another reaction
        ''' </summary>
        ''' <paramname="other">The other reaction</param>
        ''' <returns>A new combined PhasedReaction</returns>
        Public Function Combine(other As PhasedReaction) As PhasedReaction
            Dim combinedSparse = New Dictionary(Of PhasedCompound, Double)(Sparse)
            Dim existing As Double = Nothing

            For Each compoundCoeff In other.Sparse
                Dim compound = compoundCoeff.Key
                Dim coeff = compoundCoeff.Value
                combinedSparse.TryGetValue(compound, existing)
                Dim newCoeff = existing + coeff
                If Math.Abs(newCoeff) < 0.0000000001 Then
                    combinedSparse.Remove(compound)
                Else
                    combinedSparse(compound) = newCoeff
                End If
            Next

            Return New PhasedReaction(combinedSparse, Arrow, ReactionId)
        End Function

        ''' <summary>
        ''' Serializes this reaction to a list of dictionaries
        ''' </summary>
        ''' <returns>A list of compound dictionaries with coefficients</returns>
        Public Function Serialize() As List(Of Dictionary(Of String, Object))
            Return Sparse.[Select](Function(kv)
                                       Dim dict = kv.Key.Serialize()
                                       dict("coefficient") = kv.Value
                                       Return dict
                                   End Function).ToList()
        End Function

        ''' <summary>
        ''' Formats this reaction as a formula string
        ''' </summary>
        ''' <returns>The reaction formula string</returns>
        Public Overrides Function ToString() As String
            Dim reactants = String.Join(" + ", Me.Reactants.Select(Function(r)
                                                                       Dim coeff = If(Math.Abs(r.coefficient - 1.0) < 0.0000000001, "", $"{r.coefficient} ")
                                                                       Return $"{coeff}{r.compound}"
                                                                   End Function))

            Dim products = String.Join(" + ", Me.Products.Select(Function(p)
                                                                     Dim coeff = If(Math.Abs(p.coefficient - 1.0) < 0.0000000001, "", $"{p.coefficient} ")
                                                                     Return $"{coeff}{p.compound}"
                                                                 End Function))

            Return $"{reactants} {Arrow} {products}"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As PhasedReaction = TryCast(obj, PhasedReaction)
            Return other IsNot Nothing AndAlso Sparse.Count = other.Sparse.Count AndAlso Sparse.All(Function(kv)
                                                                                                        Dim otherCoeff As Double = Nothing
                                                                                                        Return other.Sparse.TryGetValue(kv.Key, otherCoeff) AndAlso Math.Abs(kv.Value - otherCoeff) < 0.0000000001
                                                                                                    End Function)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Dim hash = New HashCode()
            For Each kv In Sparse.OrderBy(Function(x) x.Key.CompoundId)
                hash.Add(kv.Key)
                hash.Add(kv.Value)
            Next
            Return hash.ToHashCode()
        End Function
    End Class

End Namespace
