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

Imports System.Globalization
Imports System.IO
Imports System.Runtime.CompilerServices
Imports eQuilibrator.EquilibratorApi.Core.Constants
Imports Microsoft.VisualBasic.Language

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Defines lower and upper bounds on compound concentrations.
    ''' Used in thermodynamic analysis to constrain metabolite concentrations.
    ''' </summary>
    Public Class Bounds
        ''' <summary>
        ''' The default lower bound for concentrations (in molar)
        ''' </summary>
        Public ReadOnly Property DefaultLowerBound As Double

        ''' <summary>
        ''' The default upper bound for concentrations (in molar)
        ''' </summary>
        Public ReadOnly Property DefaultUpperBound As Double

        ''' <summary>
        ''' Compound-specific lower bounds
        ''' </summary>
        Private ReadOnly _lowerBounds As Dictionary(Of String, Double) = New Dictionary(Of String, Double)()

        ''' <summary>
        ''' Compound-specific upper bounds
        ''' </summary>
        Private ReadOnly _upperBounds As Dictionary(Of String, Double) = New Dictionary(Of String, Double)()

        ''' <summary>
        ''' Creates a new Bounds instance with default values
        ''' </summary>
        ''' <paramname="defaultLower">Default lower bound in molar</param>
        ''' <paramname="defaultUpper">Default upper bound in molar</param>
        Public Sub New(Optional defaultLower As Double = DefaultConcentrationLowerBound, Optional defaultUpper As Double = ThermodynamicConstants.DefaultConcentrationUpperBound)
            If defaultLower < 0 Then Throw New ArgumentException("Lower bound cannot be negative", NameOf(defaultLower))
            If defaultUpper <= defaultLower Then Throw New ArgumentException("Upper bound must be greater than lower bound", NameOf(defaultUpper))

            DefaultLowerBound = defaultLower
            DefaultUpperBound = defaultUpper
        End Sub

        ''' <summary>
        ''' Gets the lower bound for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>The lower bound in molar</returns>
        Public Function GetLowerBound(compoundId As String) As Double
            Dim bound As Double = Nothing
            Return If(_lowerBounds.TryGetValue(compoundId, bound), bound, DefaultLowerBound)
        End Function

        ''' <summary>
        ''' Gets the upper bound for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>The upper bound in molar</returns>
        Public Function GetUpperBound(compoundId As String) As Double
            Dim bound As Double = Nothing
            Return If(_upperBounds.TryGetValue(compoundId, bound), bound, DefaultUpperBound)
        End Function

        ''' <summary>
        ''' Gets the bounds for a compound as a tuple
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>A tuple of (lower, upper) bounds in molar</returns>
        Public Function GetBounds(compoundId As String) As (Double, Double)
            Return (GetLowerBound(compoundId), GetUpperBound(compoundId))
        End Function

        ''' <summary>
        ''' Sets the lower bound for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="bound">The lower bound in molar</param>
        Public Sub SetLowerBound(compoundId As String, bound As Double)
            If bound < 0 Then Throw New ArgumentException("Lower bound cannot be negative", NameOf(bound))

            _lowerBounds(compoundId) = bound

            ' Ensure upper bound is at least as high as lower bound
            Dim upper As Double = Nothing

            If _upperBounds.TryGetValue(compoundId, upper) AndAlso upper < bound Then
                _upperBounds(compoundId) = bound
            End If
        End Sub

        ''' <summary>
        ''' Sets the upper bound for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="bound">The upper bound in molar</param>
        Public Sub SetUpperBound(compoundId As String, bound As Double)
            If bound <= 0 Then Throw New ArgumentException("Upper bound must be positive", NameOf(bound))

            _upperBounds(compoundId) = bound

            ' Ensure lower bound is at most as high as upper bound
            Dim lower As Double = Nothing

            If _lowerBounds.TryGetValue(compoundId, lower) AndAlso lower > bound Then
                _lowerBounds(compoundId) = bound
            End If
        End Sub

        ''' <summary>
        ''' Sets both bounds for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="lower">The lower bound in molar</param>
        ''' <paramname="upper">The upper bound in molar</param>
        Public Sub SetBounds(compoundId As String, lower As Double, upper As Double)
            If lower < 0 Then Throw New ArgumentException("Lower bound cannot be negative", NameOf(lower))
            If upper <= lower Then Throw New ArgumentException("Upper bound must be greater than lower bound", NameOf(upper))

            _lowerBounds(compoundId) = lower
            _upperBounds(compoundId) = upper
        End Sub

        ''' <summary>
        ''' Sets the same bounds for multiple compounds
        ''' </summary>
        ''' <paramname="compoundIds">The compound identifiers</param>
        ''' <paramname="lower">The lower bound in molar</param>
        ''' <paramname="upper">The upper bound in molar</param>
        Public Sub SetBoundsForAll(compoundIds As IEnumerable(Of String), lower As Double, upper As Double)
            For Each compoundId In compoundIds
                SetBounds(compoundId, lower, upper)
            Next
        End Sub

        ''' <summary>
        ''' Gets all compounds with custom bounds
        ''' </summary>
        ''' <returns>Set of compound IDs with custom bounds</returns>
        Public Function GetCustomBoundCompounds() As IReadOnlySet(Of String)
            Return _lowerBounds.Keys.Union(_upperBounds.Keys).ToHashSet()
        End Function

        ''' <summary>
        ''' Checks if a concentration is within bounds for a compound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="concentration">The concentration to check</param>
        ''' <returns>True if the concentration is within bounds</returns>
        Public Function IsWithinBounds(compoundId As String, concentration As Double) As Boolean
            Dim lowerUpper As (lower As Double, upper As Double) = Nothing
            lowerUpper = GetBounds(compoundId)
            Return concentration >= lowerUpper.lower AndAlso concentration <= lowerUpper.upper
        End Function

        ''' <summary>
        ''' Gets the logarithm of the bounds (useful for thermodynamic calculations)
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <returns>A tuple of (ln(lower), ln(upper))</returns>
        Public Function GetLogBounds(compoundId As String) As (Double, Double)
            Dim lowerUpper As (lower As Double, upper As Double) = Nothing
            lowerUpper = GetBounds(compoundId)
            Return (Math.Log(lowerUpper.lower), Math.Log(lowerUpper.upper))
        End Function

        ''' <summary>
        ''' Creates a copy of this Bounds instance
        ''' </summary>
        ''' <returns>A new Bounds instance with the same values</returns>
        Public Function Clone() As Bounds
            Dim lClone = New Bounds(DefaultLowerBound, DefaultUpperBound)
            For Each compoundIdLower In _lowerBounds
                Dim compoundId = compoundIdLower.Key
                Dim lower = compoundIdLower.Value
                lClone._lowerBounds(compoundId) = lower
            Next
            For Each compoundIdUpper In _upperBounds
                Dim compoundId = compoundIdUpper.Key
                Dim upper = compoundIdUpper.Value
                lClone._upperBounds(compoundId) = upper
            Next
            Return lClone
        End Function

        ''' <summary>
        ''' Creates Bounds from a CSV file
        ''' </summary>
        ''' <paramname="filePath">Path to the CSV file</param>
        ''' <returns>A new Bounds instance</returns>
        Public Shared Function FromCsv(filePath As String) As Bounds
            Dim bounds = New Bounds()

            Dim reader = New StreamReader(filePath)
            Dim headerLine As String = reader.ReadLine()

            If Equals(headerLine, Nothing) Then Return bounds

            ' Parse header to find column indices
            Dim headers = headerLine.Split(","c).[Select](Function(h) h.Trim().ToLower()).ToList()
            Dim compoundIndex = headers.FindIndex(Function(h) Equals(h, "compound") OrElse Equals(h, "compound_id") OrElse Equals(h, "id"))
            Dim lowerIndex = headers.FindIndex(Function(h) Equals(h, "lower") OrElse Equals(h, "min") OrElse Equals(h, "lower_bound"))
            Dim upperIndex = headers.FindIndex(Function(h) Equals(h, "upper") OrElse Equals(h, "max") OrElse Equals(h, "upper_bound"))

            If compoundIndex < 0 Then Throw New InvalidDataException("CSV file must have a 'compound' column")

            Dim line As Value(Of String) = ""
            Dim lower As Double = Nothing, upper As Double = Nothing
            While (line = reader.ReadLine) IsNot Nothing
                If String.IsNullOrWhiteSpace(line) Then Continue While

                Dim parts = line.Split(","c)
                If parts.Length <= compoundIndex Then Continue While

                Dim compoundId = parts(CInt(compoundIndex)).Trim()

                If lowerIndex >= 0 AndAlso lowerIndex < parts.Length Then
                    If Double.TryParse(parts(CInt(lowerIndex)).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, lower) Then
                        bounds.SetLowerBound(compoundId, lower)
                    End If
                End If

                If upperIndex >= 0 AndAlso upperIndex < parts.Length Then
                    If Double.TryParse(parts(CInt(upperIndex)).Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, upper) Then
                        bounds.SetUpperBound(compoundId, upper)
                    End If
                End If
            End While

            Return bounds
        End Function

        ''' <summary>
        ''' Saves bounds to a CSV file
        ''' </summary>
        ''' <paramname="filePath">Path to the CSV file</param>
        ''' <paramname="compoundIds">Compound IDs to include</param>
        Public Sub ToCsv(filePath As String, Optional compoundIds As IEnumerable(Of String) = Nothing)
            Dim writer = New StreamWriter(filePath)

            writer.WriteLine("Compound,Lower,Upper")

            Dim compounds = If(compoundIds, GetCustomBoundCompounds())
            Dim lowerUpper As (lower As Double, upper As Double) = Nothing
            For Each compoundId In compounds
                lowerUpper = GetBounds(compoundId)
                writer.WriteLine($"{compoundId},{ lowerUpper.lower},{ lowerUpper.upper}")
            Next
        End Sub

        ''' <summary>
        ''' Gets the default bounds for common cofactors
        ''' </summary>
        ''' <returns>A Bounds instance with cofactor-specific bounds</returns>
        Public Shared Function GetDefaultCofactorBounds() As Bounds
            Dim bounds = New Bounds()

            ' Common cofactors with typical physiological concentration ranges
            ' ATP/ADP ratio is typically high (10:1 to 100:1)
            bounds.SetBounds("ATP", 0.001, 0.01)    ' 1-10 mM
            bounds.SetBounds("ADP", 0.0001, 0.001)    ' 0.1-1 mM
            bounds.SetBounds("AMP", 0.00001, 0.0001)    ' 0.01-0.1 mM

            ' NAD+/NADH ratio is typically high
            bounds.SetBounds("NAD", 0.001, 0.01)    ' 1-10 mM
            bounds.SetBounds("NADH", 0.00001, 0.0001)   ' 0.01-0.1 mM

            ' NADP+/NADPH ratio is typically low (NADPH is high)
            bounds.SetBounds("NADP", 0.00001, 0.0001)   ' 0.01-0.1 mM
            bounds.SetBounds("NADPH", 0.001, 0.01)  ' 1-10 mM

            ' CoA and acetyl-CoA
            bounds.SetBounds("CoA", 0.0001, 0.001)    ' 0.1-1 mM
            bounds.SetBounds("acetyl-CoA", 0.00001, 0.001) ' 0.01-1 mM

            ' Inorganic phosphate
            bounds.SetBounds("Pi", 0.001, 0.01)     ' 1-10 mM

            ' Protons (pH range)
            bounds.SetBounds("H+", 0.00000001, 0.000001)     ' pH 6-8

            ' Water
            bounds.SetBounds("H2O", 55.0, 55.5)    ' ~55.5 M (pure water)

            Return bounds
        End Function

        Public Overrides Function ToString() As String
            Return $"Bounds: [{DefaultLowerBound} - {DefaultUpperBound}] M"
        End Function
    End Class

    ''' <summary>
    ''' Extension methods for Bounds
    ''' </summary>
    Public Module BoundsExtensions
        ''' <summary>
        ''' Gets bounds for a PhasedCompound
        ''' </summary>
        <Extension()>
        Public Function GetBounds(bounds As Bounds, compound As PhasedCompound) As (Double, Double)
            Return bounds.GetBounds(compound.CompoundId)
        End Function

        ''' <summary>
        ''' Sets bounds for a PhasedCompound
        ''' </summary>
        <Extension()>
        Public Sub SetBounds(bounds As Bounds, compound As PhasedCompound, lower As Double, upper As Double)
            bounds.SetBounds(compound.CompoundId, lower, upper)
        End Sub
    End Module
End Namespace
