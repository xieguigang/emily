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

Imports eQuilibrator.EquilibratorApi.Core
Imports eQuilibrator.EquilibratorApi.Core.Extensions
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.EquilibratorApi.Core.Parsers
Imports test.EquilibratorThermodynamics

Namespace EquilibratorApi.Demo

    ''' <summary>
    ''' Demo program showing how to use the Equilibrator API
    ''' </summary>
    Friend Class Program
        Public Shared Sub Main()
            Call CompleteExample.Main2(Nothing)

            Console.WriteLine("========================================")
            Console.WriteLine("  Equilibrator API - C# Demo")
            Console.WriteLine("========================================")
            Console.WriteLine()

            ' Demo 1: Parse a reaction
            Console.WriteLine("Demo 1: Reaction Parsing")
            Console.WriteLine("------------------------")
            Call DemoReactionParsing()

            ' Demo 2: Thermodynamic calculations
            Console.WriteLine(vbLf & "Demo 2: Thermodynamic Calculations")
            Console.WriteLine("----------------------------------")
            Call DemoThermodynamicCalculations()

            ' Demo 3: Stoichiometric Model
            Console.WriteLine(vbLf & "Demo 3: Stoichiometric Model")
            Console.WriteLine("----------------------------")
            Call DemoStoichiometricModel()

            ' Demo 4: Bounds and Concentrations
            Console.WriteLine(vbLf & "Demo 4: Bounds and Concentrations")
            Console.WriteLine("---------------------------------")
            Call DemoBoundsAndConcentrations()

            ' Demo 5: Extension Methods
            Console.WriteLine(vbLf & "Demo 5: Extension Methods")
            Console.WriteLine("-------------------------")
            Call DemoExtensionMethods()

            Console.WriteLine(vbLf & "========================================")
            Console.WriteLine("  Demo Complete!")
            Console.WriteLine("========================================")
        End Sub

        Private Shared Sub DemoReactionParsing()
            Dim parser = New ReactionParser()

            ' Parse a simple reaction
            Dim formula1 = "glucose + ATP => glucose-6-phosphate + ADP"
            Console.WriteLine($"Formula: {formula1}")

            Dim reaction1 As ParsedReaction = Nothing

            If parser.TryParse(formula1, reaction1) Then
                Console.WriteLine($"  Arrow: {reaction1.Arrow}")
                Console.WriteLine($"  Reactants: {String.Join(", ", reaction1.Reactants)}")
                Console.WriteLine($"  Products: {String.Join(", ", reaction1.Products)}")
                Console.WriteLine($"  Is reversible: { reaction1.IsReversible}")
            End If

            ' Parse a reaction with coefficients
            Dim formula2 = "2 H2 + O2 <=> 2 H2O"
            Console.WriteLine($"
Formula: {formula2}")

            Dim reaction2 As ParsedReaction = Nothing

            If parser.TryParse(formula2, reaction2) Then
                Console.WriteLine($"  Arrow: {reaction2.Arrow}")
                Console.WriteLine($"  Stoichiometry:")
                For Each compoundIdCoeff In reaction2.GetStoichiometry()
                    Dim compoundId = compoundIdCoeff.Key
                    Dim coeff = compoundIdCoeff.Value
                    Console.WriteLine($"    {compoundId}: {coeff:F2}")
                Next
            End If

            ' Parse a reaction with compartments
            Dim formula3 = "ATP[c] + H2O[c] => ADP[c] + Pi[c] + H+[c]"
            Console.WriteLine($"
Formula: {formula3}")

            Dim reaction3 As ParsedReaction = Nothing

            If parser.TryParse(formula3, reaction3) Then
                Console.WriteLine($"  Compounds with compartments:")
                For Each compound In reaction3.AllCompounds()
                    Console.WriteLine($"    {compound.CompoundId} [{compound.Compartment}] (coeff: {compound.Coefficient})")
                Next
            End If
        End Sub

        Private Shared Sub DemoThermodynamicCalculations()
            Dim compContrib = New ComponentContribution With {
        .PH = 7.0,
        .PMg = 3.0,
        .IonicStrength = 0.25,
        .Temperature = 298.15
    }

            ' Calculate Gibbs energy for ATP hydrolysis
            Dim formula = "ATP + H2O => ADP + Pi"
            Console.WriteLine($"Reaction: {formula}")

            Dim reaction = compContrib.Reaction(formula)
            If reaction IsNot Nothing Then
                Console.WriteLine($"  Reaction: {reaction}")

                Dim result = compContrib.StandardDgPrime(reaction)
                Console.WriteLine($"  Standard ΔG'°: {result.StandardDgPrime}")
                Console.WriteLine($"  Uncertainty: {result.Uncertainty}")
                Console.WriteLine($"  Physiological ΔG': {result.PhysiologicalDgPrime}")
                Console.WriteLine($"  Actual ΔG': {result.DgPrime}")
                Console.WriteLine($"  Equilibrium constant: {result.EquilibriumConstant:F2}")
                Console.WriteLine($"  Is feasible: {result.IsFeasible}")

                Dim direction = compContrib.GetReactionDirection(reaction)
                Console.WriteLine($"  Direction: {direction}")
            End If

            ' Calculate for another reaction
            Console.WriteLine(vbLf & "--- Glucose to Pyruvate (Glycolysis simplified) ---")
            Dim glycolysis = "glucose + 2 ADP + 2 Pi + 2 NAD => 2 pyruvate + 2 ATP + 2 NADH + 2 H+ + 2 H2O"
            Console.WriteLine($"Reaction: {glycolysis}")

            Dim glycolysisReaction = compContrib.Reaction(glycolysis)
            If glycolysisReaction IsNot Nothing Then
                Dim result = compContrib.StandardDgPrime(glycolysisReaction)
                Console.WriteLine($"  Standard ΔG'°: {result.StandardDgPrime}")
                Console.WriteLine($"  Is feasible: {result.IsFeasible}")
            End If
        End Sub

        Private Shared Sub DemoStoichiometricModel()
            Dim compContrib = New ComponentContribution()
            Dim model = New StoichiometricModel(compContrib) With {
        .Id = "glycolysis",
        .Name = "Glycolysis Pathway"
    }

            ' Add reactions
            model.AddReaction("glucose + ATP => glucose-6-phosphate + ADP", "HEX1")
            model.AddReaction("glucose-6-phosphate <=> fructose-6-phosphate", "PGI")
            model.AddReaction("fructose-6-phosphate + ATP => fructose-1,6-bisphosphate + ADP", "PFK")
            model.AddReaction("fructose-1,6-bisphosphate => dihydroxyacetone-phosphate + glyceraldehyde-3-phosphate", "FBA")
            model.AddReaction("dihydroxyacetone-phosphate <=> glyceraldehyde-3-phosphate", "TPI")

            Console.WriteLine(model.GetSummary())
            Dim t As (matrix As Double(,), compoundIds As List(Of String), reactionIds As List(Of String)) = Nothing

            ' Get stoichiometric matrix
            t = model.GetStoichiometricMatrixWithLabels()
            Console.WriteLine(vbLf & "Stoichiometric Matrix:")
            Console.WriteLine($"  Compounds: {String.Join(", ", t.compoundIds.Take(5))}...")
            Console.WriteLine($"  Reactions: {String.Join(", ", t.reactionIds)}")

            ' Calculate Gibbs energies
            Dim dgResults = model.CalculateStandardDgPrimes()
            Console.WriteLine(vbLf & "Gibbs Energy Results:")
            For Each rxnIdResult In dgResults
                Dim rxnId = rxnIdResult.Key
                Dim result = rxnIdResult.Value
                Console.WriteLine($"  {rxnId}: ΔG'° = {result.StandardDgPrime.Value:F2} kJ/mol")
            Next

            ' Get external metabolites
            Dim external = model.GetExternalMetabolites()
            Console.WriteLine($"
External metabolites: {String.Join(", ", external.Keys)}")
        End Sub

        Private Shared Sub DemoBoundsAndConcentrations()
            ' Create default physiological bounds
            Dim bounds As Bounds = Bounds.GetDefaultCofactorBounds
            Console.WriteLine($"Default bounds: {bounds.DefaultLowerBound} - {bounds.DefaultUpperBound} M")

            ' Set custom bounds
            bounds.SetBounds("ATP", 0.001, 0.01)  ' 1-10 mM
            bounds.SetBounds("ADP", 0.0001, 0.001)  ' 0.1-1 mM

            Console.WriteLine($"
ATP bounds: {bounds.GetLowerBound("ATP")} - {bounds.GetUpperBound("ATP")} M")
            Console.WriteLine($"ADP bounds: {bounds.GetLowerBound("ADP")} - {bounds.GetUpperBound("ADP")} M")

            ' Create bounds from CSV-like data
            Dim csvData = New List(Of (String, Double, Double)) From {
        ("glucose", 0.000001, 0.01),
        ("pyruvate", 0.000001, 0.01),
        ("lactate", 0.000001, 0.01)
    }

            Dim customBounds = New Bounds(0.000001, 0.01)
            For Each compoundIdLowerUpper In csvData
                Dim compoundId = compoundIdLowerUpper.Item1
                Dim lower = compoundIdLowerUpper.Item2
                Dim upper = compoundIdLowerUpper.Item3
                customBounds.SetBounds(compoundId, lower, upper)
            Next

            Console.WriteLine(vbLf & "Custom bounds:")
            Dim lowerUpper As (lower As Double, upper As Double) = Nothing
            For Each compoundId In {"glucose", "pyruvate", "lactate"}
                lowerUpper = customBounds.GetBounds(compoundId)
                Console.WriteLine($"  {compoundId}: { lowerUpper.lower:E2} - { lowerUpper.upper:E2} M")
            Next
        End Sub

        Private Shared Sub DemoExtensionMethods()
            ' Thermodynamic conversions
            Dim dgKj = -30.5  ' kJ/mol
            Dim dgKcal = dgKj.KjToKcal()
            Console.WriteLine($"Gibbs energy: {dgKj} kJ/mol = {dgKcal:F2} kcal/mol")

            ' Equilibrium constant
            Dim keq = dgKj.ToEquilibriumConstant()
            Console.WriteLine($"Equilibrium constant: K_eq = {keq:E4}")

            ' Back to Gibbs energy
            Dim dgBack = keq.ToGibbsEnergy()
            Console.WriteLine($"Back to Gibbs energy: {dgBack:F2} kJ/mol")

            ' Reduction potential
            Dim e0 = dgKj.ToReductionPotential(2)
            Console.WriteLine($"Reduction potential (2 e-): E°' = {e0:F4} V")

            ' String extensions
            Dim compoundId = "M_glc__D_c"
            Dim normalized = compoundId.NormalizeCompoundId()
            Console.WriteLine($"
Normalized compound ID: '{compoundId}' -> '{normalized}'")

            ' Matrix operations
            Dim matrix = New Double(,) {
        {1.0, -1.0, 0.0},
        {0.0, 1.0, -1.0},
                {-1.0, 0.0, 1.0}}

            Console.WriteLine(vbLf & "Matrix:")
            Console.WriteLine(matrix.ToString("F2"))
        End Sub
    End Class
End Namespace
