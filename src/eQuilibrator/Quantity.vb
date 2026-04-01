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
