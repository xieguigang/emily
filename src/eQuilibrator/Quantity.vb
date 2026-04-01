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

Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Represents a quantity with a unit of measurement.
    ''' This is a simplified version of the pint library's Quantity class.
    ''' </summary>
    Public Structure Quantity
        Implements IComparable(Of Quantity), IEquatable(Of Quantity)
        ''' <summary>
        ''' The numerical value in the specified unit
        ''' </summary>
        Public ReadOnly Property Value As Double

        ''' <summary>
        ''' The unit of measurement
        ''' </summary>
        Public ReadOnly Property Unit As String

        ''' <summary>
        ''' Creates a new Quantity with the specified value and unit
        ''' </summary>
        ''' <paramname="value">The numerical value</param>
        ''' <paramname="unit">The unit of measurement</param>
        Public Sub New(value As Double, unit As String)
            Me.Value = value
            Me.Unit = unit
        End Sub

        ''' <summary>
        ''' Creates a dimensionless quantity
        ''' </summary>
        ''' <paramname="value">The numerical value</param>
        Public Sub New(value As Double)
            Me.Value = value
            Unit = ""
        End Sub

        ''' <summary>
        ''' Gets a value indicating whether this quantity is dimensionless
        ''' </summary>
        Public Function IsDimensionless() As Boolean
            Return String.IsNullOrEmpty(Unit)
        End Function

        ''' <summary>
        ''' Converts this quantity to the specified unit
        ''' </summary>
        ''' <paramname="targetUnit">The target unit</param>
        ''' <returns>A new Quantity in the target unit</returns>
        Public Function [To](targetUnit As String) As Quantity
            Dim conversionFactor = GetConversionFactor(Unit, targetUnit)
            Return New Quantity(Value * conversionFactor, targetUnit)
        End Function

        ''' <summary>
        ''' Gets the value in the specified unit
        ''' </summary>
        ''' <paramname="targetUnit">The target unit</param>
        ''' <returns>The value converted to the target unit</returns>
        Public Function GetValueIn(targetUnit As String) As Double
            Return [To](targetUnit).Value
        End Function

        ''' <summary>
        ''' Gets the magnitude (value) in the specified unit (alias for GetValueIn)
        ''' </summary>
        ''' <paramname="targetUnit">The target unit</param>
        ''' <returns>The value converted to the target unit</returns>
        Public Function MagnitudeAs(targetUnit As String) As Double
            Return GetValueIn(targetUnit)
        End Function

#Region "Operators"

        Public Shared Operator +(a As Quantity, b As Quantity) As Quantity
            If Not Equals(a.Unit, b.Unit) Then
                b = b.To(a.Unit)
            End If
            Return New Quantity(a.Value + b.Value, a.Unit)
        End Operator

        Public Shared Operator -(a As Quantity, b As Quantity) As Quantity
            If Not Equals(a.Unit, b.Unit) Then
                b = b.To(a.Unit)
            End If
            Return New Quantity(a.Value - b.Value, a.Unit)
        End Operator

        Public Shared Operator *(a As Quantity, scalar As Double) As Quantity
            Return New Quantity(a.Value * scalar, a.Unit)
        End Operator

        Public Shared Operator *(scalar As Double, a As Quantity) As Quantity
            Return a * scalar
        End Operator

        Public Shared Operator *(a As Quantity, b As Quantity) As Quantity
            ' Simplified multiplication - just multiply values
            ' In a full implementation, this would handle unit combinations
            Return New Quantity(a.Value * b.Value, CombineUnits(a.Unit, b.Unit, "*"))
        End Operator

        Public Shared Operator /(a As Quantity, scalar As Double) As Quantity
            Return New Quantity(a.Value / scalar, a.Unit)
        End Operator

        Public Shared Operator /(a As Quantity, b As Quantity) As Quantity
            Return New Quantity(a.Value / b.Value, CombineUnits(a.Unit, b.Unit, "/"))
        End Operator

        Public Shared Operator -(a As Quantity) As Quantity
            Return New Quantity(-a.Value, a.Unit)
        End Operator

        Public Shared Operator <(a As Quantity, b As Quantity) As Boolean
            If Not Equals(a.Unit, b.Unit) Then
                b = b.To(a.Unit)
            End If
            Return a.Value < b.Value
        End Operator

        Public Shared Operator >(a As Quantity, b As Quantity) As Boolean
            If Not Equals(a.Unit, b.Unit) Then
                b = b.To(a.Unit)
            End If
            Return a.Value > b.Value
        End Operator

        Public Shared Operator <=(a As Quantity, b As Quantity) As Boolean
            Return a < b OrElse a = b
        End Operator

        Public Shared Operator >=(a As Quantity, b As Quantity) As Boolean
            Return a > b OrElse a = b
        End Operator

        Public Shared Operator =(a As Quantity, b As Quantity) As Boolean
            Return a.Equals(b)
        End Operator

        Public Shared Operator <>(a As Quantity, b As Quantity) As Boolean
            Return Not a.Equals(b)
        End Operator

#End Region

#Region "IComparable<Quantity>"

        Public Function CompareTo(other As Quantity) As Integer Implements IComparable(Of Quantity).CompareTo
            If Not Equals(Unit, other.Unit) Then
                other = other.To(Unit)
            End If
            Return Value.CompareTo(other.Value)
        End Function

#End Region

#Region "IEquatable<Quantity>"

        Public Overloads Function Equals(other As Quantity) As Boolean Implements IEquatable(Of Quantity).Equals
            If Not Equals(Unit, other.Unit) Then
                Try
                    other = other.To(Unit)
                Catch
                    Return False
                End Try
            End If
            Return Math.Abs(Value - other.Value) < 0.0000000001
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Return Equals(DirectCast(obj, Quantity))
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(Value, Unit)
        End Function

#End Region

#Region "Object Overrides"

        Public Overrides Function ToString() As String
            If String.IsNullOrEmpty(Unit) Then
                Return Value.ToString("G")
            End If
            Return $"{Value:G} {Unit}"
        End Function

#End Region

#Region "Helper Methods"

        Private Shared Function CombineUnits(unit1 As String, unit2 As String, operation As String) As String
            If String.IsNullOrEmpty(unit1) AndAlso String.IsNullOrEmpty(unit2) Then Return ""
            If String.IsNullOrEmpty(unit1) Then Return If(Equals(operation, "*"), unit2, $"1/{unit2}")
            If String.IsNullOrEmpty(unit2) Then Return unit1

            Return If(Equals(operation, "*"), $"{unit1}*{unit2}", $"{unit1}/{unit2}")
        End Function

#End Region

#Region "Static Factory Methods"

        ''' <summary>
        ''' Creates a quantity from a string representation (e.g., "1.0 M", "298.15 K")
        ''' </summary>
        ''' <paramname="quantityString">The string representation</param>
        ''' <returns>A new Quantity</returns>
        Public Shared Function Parse(quantityString As String) As Quantity
            Dim parts = quantityString.Trim().Split({" "c}, 2, StringSplitOptions.RemoveEmptyEntries)
            If parts.Length = 0 Then Throw New FormatException($"Cannot parse quantity from '{quantityString}'")

            Dim value = Double.Parse(parts(0))
            Dim unit = If(parts.Length > 1, parts(1), "")

            Return New Quantity(value, unit)
        End Function

        ''' <summary>
        ''' Tries to parse a quantity from a string representation
        ''' </summary>
        ''' <paramname="quantityString">The string representation</param>
        ''' <paramname="result">The parsed quantity</param>
        ''' <returns>True if parsing succeeded</returns>
        Public Shared Function TryParse(quantityString As String, <Out> ByRef result As Quantity) As Boolean
            Try
                result = Parse(quantityString)
                Return True
            Catch
                result = Nothing
                Return False
            End Try
        End Function

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class

#End Region
    End Structure

    ''' <summary>
    ''' Provides unit conversion functionality
    ''' </summary>
    Public Module UnitConverter
        Private ReadOnly ConversionFactors As Dictionary(Of (String, String), Double) = New Dictionary(Of (String, String), Double)() From {
                                                                                                                                            _
        {("m", "cm"), 100},  ' Length
        {("cm", "m"), 0.01},
        {("m", "mm"), 1000},
        {("mm", "m"), 0.001},
                             _ ' Temperature (for differences, not absolute values)
        {("K", "C"), 1}, ' For differences only
        {("C", "K"), 1}, ' For differences only
                         _        ' Energy
        {("kJ", "J"), 1000},
        {("J", "kJ"), 0.001},
        {("kJ/mol", "J/mol"), 1000},
        {("J/mol", "kJ/mol"), 0.001},
        {("kcal", "kJ"), 4.184},
        {("kJ", "kcal"), 0.239006},
        {("kcal/mol", "kJ/mol"), 4.184},
        {("kJ/mol", "kcal/mol"), 0.239006},
                                           _         ' Concentration
        {("M", "mM"), 1000},
        {("mM", "M"), 0.001},
        {("M", "μM"), 1000000.0},
        {("μM", "M"), 0.000001},
        {("M", "uM"), 1000000.0},
        {("uM", "M"), 0.000001},
        {("M", "nM"), 1000000000.0},
        {("nM", "M"), 0.000000001},
        {("mM", "μM"), 1000},
        {("μM", "mM"), 0.001},
        {("mM", "uM"), 1000},
        {("uM", "mM"), 0.001},
                              _         ' Volume
        {("L", "mL"), 1000},
        {("mL", "L"), 0.001},
        {("L", "l"), 1},
        {("l", "L"), 1},
                        _         ' Pressure
        {("atm", "Pa"), 101325},
        {("Pa", "atm"), 0.00000986923},
        {("bar", "Pa"), 100000},
        {("Pa", "bar"), 0.00001},
                                 _         ' Electrical potential
        {("V", "mV"), 1000},
        {("mV", "V"), 0.001},
                             _         ' Time
        {("s", "ms"), 1000},
        {("ms", "s"), 0.001},
        {("min", "s"), 60},
        {("s", "min"), 1.0 / 60},
        {("h", "s"), 3600},
        {("s", "h"), 1.0 / 3600}
    }

        ''' <summary>
        ''' Gets the conversion factor from one unit to another
        ''' </summary>
        ''' <paramname="fromUnit">Source unit</param>
        ''' <paramname="toUnit">Target unit</param>
        ''' <returns>Conversion factor</returns>
        Public Function GetConversionFactor(fromUnit As String, toUnit As String) As Double
            If String.IsNullOrEmpty(fromUnit) AndAlso String.IsNullOrEmpty(toUnit) Then Return 1.0

            If Equals(fromUnit, toUnit) Then Return 1.0

            Dim key = (fromUnit, toUnit)
            Dim factor As Double = Nothing
            If ConversionFactors.TryGetValue(key, factor) Then Return factor

            ' Try inverse conversion
            Dim inverseKey = (toUnit, fromUnit)
            Dim inverseFactor As Double = Nothing
            If ConversionFactors.TryGetValue(inverseKey, inverseFactor) Then Return 1.0 / inverseFactor

            Throw New NotSupportedException($"Cannot convert from '{fromUnit}' to '{toUnit}'")
        End Function

        ''' <summary>
        ''' Checks if conversion between two units is supported
        ''' </summary>
        ''' <paramname="fromUnit">Source unit</param>
        ''' <paramname="toUnit">Target unit</param>
        ''' <returns>True if conversion is supported</returns>
        Public Function CanConvert(fromUnit As String, toUnit As String) As Boolean
            If Equals(fromUnit, toUnit) Then Return True

            Return ConversionFactors.ContainsKey((fromUnit, toUnit)) OrElse ConversionFactors.ContainsKey((toUnit, fromUnit))
        End Function

        ''' <summary>
        ''' Registers a new conversion factor
        ''' </summary>
        ''' <paramname="fromUnit">Source unit</param>
        ''' <paramname="toUnit">Target unit</param>
        ''' <paramname="factor">Conversion factor</param>
        Public Sub RegisterConversion(fromUnit As String, toUnit As String, factor As Double)
            ConversionFactors((fromUnit, toUnit)) = factor
        End Sub
    End Module

    ''' <summary>
    ''' Extension methods for creating quantities
    ''' </summary>
    Public Module QuantityExtensions
        ''' <summary>
        ''' Creates a quantity from a double value with the specified unit
        ''' </summary>
        <Extension()>
        Public Function WithUnit(value As Double, unit As String) As Quantity
            Return New Quantity(value, unit)
        End Function

        ''' <summary>
        ''' Creates a dimensionless quantity from a double value
        ''' </summary>
        <Extension()>
        Public Function AsQuantity(value As Double) As Quantity
            Return New Quantity(value)
        End Function
    End Module
End Namespace
