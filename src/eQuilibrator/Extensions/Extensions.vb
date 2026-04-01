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

Imports System.Runtime.CompilerServices
Imports System.Text
Imports eQuilibrator.EquilibratorApi.Core.Constants

Namespace EquilibratorApi.Core.Extensions

    ''' <summary>
    ''' Extension methods for thermodynamic calculations
    ''' </summary>
    Public Module ThermodynamicExtensions
        ''' <summary>
        ''' Converts Gibbs energy from kJ/mol to kcal/mol
        ''' </summary>
        <Extension()>
        Public Function KjToKcal(kj As Double) As Double
            Return kj / 4.184
        End Function

        ''' <summary>
        ''' Converts Gibbs energy from kcal/mol to kJ/mol
        ''' </summary>
        <Extension()>
        Public Function KcalToKj(kcal As Double) As Double
            Return kcal * 4.184
        End Function

        ''' <summary>
        ''' Calculates the equilibrium constant from Gibbs energy
        ''' </summary>
        ''' <paramname="dg">Gibbs energy in kJ/mol</param>
        ''' <paramname="temperature">Temperature in Kelvin</param>
        ''' <returns>The equilibrium constant</returns>
        <Extension()>
        Public Function ToEquilibriumConstant(dg As Double, Optional temperature As Double = ThermodynamicConstants.DefaultTemperature) As Double
            Return Math.Exp(-dg / RT(temperature))
        End Function

        ''' <summary>
        ''' Calculates Gibbs energy from equilibrium constant
        ''' </summary>
        ''' <paramname="keq">Equilibrium constant</param>
        ''' <paramname="temperature">Temperature in Kelvin</param>
        ''' <returns>Gibbs energy in kJ/mol</returns>
        <Extension()>
        Public Function ToGibbsEnergy(keq As Double, Optional temperature As Double = DefaultTemperature) As Double
            Return -RT(temperature) * Math.Log(keq)
        End Function

        ''' <summary>
        ''' Calculates the reduction potential from Gibbs energy
        ''' </summary>
        ''' <paramname="dg">Gibbs energy in kJ/mol</param>
        ''' <paramname="nElectrons">Number of electrons transferred</param>
        ''' <returns>Reduction potential in volts</returns>
        <Extension()>
        Public Function ToReductionPotential(dg As Double, nElectrons As Integer) As Double
            Return -dg / (nElectrons * FaradayConstant)
        End Function

        ''' <summary>
        ''' Calculates Gibbs energy from reduction potential
        ''' </summary>
        ''' <paramname="e0">Reduction potential in volts</param>
        ''' <paramname="nElectrons">Number of electrons transferred</param>
        ''' <returns>Gibbs energy in kJ/mol</returns>
        <Extension()>
        Public Function ToGibbsEnergyFromPotential(e0 As Double, nElectrons As Integer) As Double
            Return -nElectrons * FaradayConstant * e0
        End Function
    End Module

    ''' <summary>
    ''' Extension methods for collections
    ''' </summary>
    Public Module CollectionExtensions
        ''' <summary>
        ''' Creates a dictionary from a sequence of key-value pairs
        ''' </summary>
        <Extension()>
        Public Function ToDictionary(Of TKey, TValue)(source As IEnumerable(Of (key As TKey, value As TValue))) As Dictionary(Of TKey, TValue)
            Return source.ToDictionary(Function(kv) kv.key, Function(kv) kv.value)
        End Function

        ''' <summary>
        ''' Adds all items from another collection
        ''' </summary>
        <Extension()>
        Public Sub AddRange(Of T)(collection As ICollection(Of T), items As IEnumerable(Of T))
            For Each item In items
                collection.Add(item)
            Next
        End Sub

        ''' <summary>
        ''' Gets a value or default if not found
        ''' </summary>
        <Extension()>
        Public Function GetValueOrDefault(Of TKey, TValue)(dictionary As IReadOnlyDictionary(Of TKey, TValue), key As TKey, Optional defaultValue As TValue = Nothing) As TValue
            Dim value As TValue = Nothing
            Return If(dictionary.TryGetValue(key, value), value, defaultValue)
        End Function
    End Module

    ''' <summary>
    ''' Extension methods for string manipulation
    ''' </summary>
    Public Module StringExtensions
        ''' <summary>
        ''' Normalizes a compound identifier by removing common prefixes
        ''' </summary>
        <Extension()>
        Public Function NormalizeCompoundId(compoundId As String) As String
            ' Remove common prefixes like "M_", "cpd:", etc.
            Dim normalized = compoundId.Trim()

            If normalized.StartsWith("M_", StringComparison.OrdinalIgnoreCase) Then
                normalized = normalized.Substring(2)
            ElseIf normalized.StartsWith("cpd:", StringComparison.OrdinalIgnoreCase) Then
                normalized = normalized.Substring(4)
            ElseIf normalized.StartsWith("CHEBI:", StringComparison.OrdinalIgnoreCase) Then
                normalized = normalized.Substring(6)
            End If

            Return normalized
        End Function

        ''' <summary>
        ''' Checks if a string is a valid compound identifier
        ''' </summary>
        <Extension()>
        Public Function IsValidCompoundId(compoundId As String) As Boolean
            Return Not String.IsNullOrWhiteSpace(compoundId) AndAlso compoundId.Length >= 1 AndAlso Char.IsLetter(compoundId(0))
        End Function
    End Module

    ''' <summary>
    ''' Extension methods for matrix operations
    ''' </summary>
    Public Module MatrixExtensions
        ''' <summary>
        ''' Gets a row from a 2D array
        ''' </summary>
        <Extension()>
        Public Function GetRow(Of T)(matrix As T(,), rowIndex As Integer) As T()
            Dim cols = matrix.GetLength(1)
            Dim row = New T(cols - 1) {}
            For i = 0 To cols - 1
                row(i) = matrix(rowIndex, i)
            Next
            Return row
        End Function

        ''' <summary>
        ''' Gets a column from a 2D array
        ''' </summary>
        <Extension()>
        Public Function GetColumn(Of T)(matrix As T(,), colIndex As Integer) As T()
            Dim rows = matrix.GetLength(0)
            Dim col = New T(rows - 1) {}
            For i = 0 To rows - 1
                col(i) = matrix(i, colIndex)
            Next
            Return col
        End Function

        ''' <summary>
        ''' Converts a 2D array to a jagged array
        ''' </summary>
        <Extension()>
        Public Function ToJaggedArray(Of T)(matrix As T(,)) As T()()
            Dim rows = matrix.GetLength(0)
            Dim cols = matrix.GetLength(1)
            Dim result = New T(rows - 1)() {}

            For i = 0 To rows - 1
                result(i) = New T(cols - 1) {}
                For j = 0 To cols - 1
                    result(i)(j) = matrix(i, j)
                Next
            Next

            Return result
        End Function

        ''' <summary>
        ''' Prints a matrix to a string
        ''' </summary>
        <Extension()>
        Public Function ToString(Of T)(matrix As T(,), Optional format As String = "F4") As String
            Dim sb = New StringBuilder()
            Dim rows = matrix.GetLength(0)
            Dim cols = matrix.GetLength(1)
            Dim formattable As IFormattable = Nothing

            For i = 0 To rows - 1
                For j = 0 To cols - 1
                    Dim value = matrix(i, j)

                    formattable = TryCast(value, IFormattable)

                    If formattable IsNot Nothing Then
                        sb.Append(formattable.ToString(CStr(format), Nothing).PadLeft(12))
                    Else
                        sb.Append(If(value?.ToString()?.PadLeft(12), "null".PadLeft(12)))
                    End If
                Next
                sb.AppendLine()
            Next

            Return sb.ToString()
        End Function
    End Module
End Namespace
