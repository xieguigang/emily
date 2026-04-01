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

Namespace EquilibratorApi.Core.Exceptions

    ''' <summary>
    ''' Exception thrown when a reaction cannot be parsed
    ''' </summary>
    Public Class ReactionParseException
        Inherits Exception
        ''' <summary>
        ''' The formula that could not be parsed
        ''' </summary>
        Public ReadOnly Property Formula As String

        Public Sub New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

        Public Sub New(message As String, formula As String)
            MyBase.New(message)
            Me.Formula = formula
        End Sub
    End Class

    ''' <summary>
    ''' Exception thrown when a compound is not found in the cache
    ''' </summary>
    Public Class CompoundNotFoundException
        Inherits Exception
        ''' <summary>
        ''' The compound identifier that was not found
        ''' </summary>
        Public ReadOnly Property CompoundId As String

        Public Sub New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

        Public Sub New(message As String, compoundId As String)
            MyBase.New(message)
            Me.CompoundId = compoundId
        End Sub
    End Class

    ''' <summary>
    ''' Exception thrown when thermodynamic calculation fails
    ''' </summary>
    Public Class ThermodynamicCalculationException
        Inherits Exception
        ''' <summary>
        ''' The reaction that caused the error
        ''' </summary>
        Public ReadOnly Property ReactionId As String

        Public Sub New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

        Public Sub New(message As String, reactionId As String)
            MyBase.New(message)
            Me.ReactionId = reactionId
        End Sub
    End Class

    ''' <summary>
    ''' Exception thrown when a model is invalid
    ''' </summary>
    Public Class ModelValidationException
        Inherits Exception
        ''' <summary>
        ''' The validation errors
        ''' </summary>
        Public ReadOnly Property Errors As IReadOnlyList(Of String)

        Public Sub New()
            Errors = New List(Of String)().AsReadOnly()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
            Errors = New List(Of String) From {
                    message
                }.AsReadOnly()
        End Sub

        Public Sub New(errors As IEnumerable(Of String))
            MyBase.New(String.Join("; ", errors))
            Me.Errors = Enumerable.ToList(errors).AsReadOnly()
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
            Errors = New List(Of String) From {
                    message
                }.AsReadOnly()
        End Sub
    End Class

    ''' <summary>
    ''' Exception thrown when unit conversion fails
    ''' </summary>
    Public Class UnitConversionException
        Inherits Exception
        ''' <summary>
        ''' The source unit
        ''' </summary>
        Public ReadOnly Property FromUnit As String

        ''' <summary>
        ''' The target unit
        ''' </summary>
        Public ReadOnly Property ToUnit As String

        Public Sub New()
        End Sub

        Public Sub New(message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(message As String, innerException As Exception)
            MyBase.New(message, innerException)
        End Sub

        Public Sub New(message As String, fromUnit As String, toUnit As String)
            MyBase.New(message)
            Me.FromUnit = fromUnit
            Me.ToUnit = toUnit
        End Sub
    End Class
End Namespace
