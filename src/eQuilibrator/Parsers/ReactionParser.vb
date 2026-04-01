' The MIT License (MIT)
'
' Copyright (c) 2018 Institute for Molecular Systems Biology, ETH Zurich.
' Copyright (c) 2018 Novo Nordisk Foundation Center for Biosustainability,
' Technical University of Denmark.
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

Imports System.Text.RegularExpressions
Imports System.Runtime.InteropServices
Imports System.Runtime.CompilerServices
Imports System.Text

Namespace EquilibratorApi.Core.Parsers

    ''' <summary>
    ''' A parser for chemical reaction formulae.
    ''' Supports various reaction arrow formats and compound notation styles.
    ''' </summary>
    Public Class ReactionParser
        ''' <summary>
        ''' Possible reaction arrows that can be used in reaction formulae
        ''' </summary>
        Public Shared ReadOnly PossibleReactionArrows As String() = {"<=>", "<==>", "⇌", "↔", "→", "=>", "->", "←", "<-", "<="}
        ' Reversible (equilibrium)
        ' Reversible (alternative)
        ' Reversible (Unicode)
        ' Reversible (Unicode alternative)
        ' Irreversible (Unicode)
        ' Irreversible
        ' Irreversible (alternative)
        ' Reverse (Unicode)
        ' Reverse
        ' Reverse

        ' Regex patterns for parsing
        Private Shared ReadOnly CoefficientPattern As Regex = New Regex("^(\d+(?:\.\d+)?)\s*", RegexOptions.Compiled)

        Private Shared ReadOnly PhasePattern As Regex = New Regex("\(([^)]+)\)\s*$", RegexOptions.Compiled)

        Private Shared ReadOnly CompartmentPattern As Regex = New Regex("\[([^\]]+)\]\s*$", RegexOptions.Compiled)

        Private Shared ReadOnly WhitespacePattern As Regex = New Regex("\s+", RegexOptions.Compiled)

        ''' <summary>
        ''' Parses a reaction formula string into a ParsedReaction object
        ''' </summary>
        ''' <paramname="formula">The reaction formula string (e.g., "glucose + ATP => glucose-6-phosphate + ADP")</param>
        ''' <returns>A ParsedReaction object containing the parsed reaction data</returns>
        ''' <exceptioncref="ArgumentException">Thrown when the formula cannot be parsed</exception>
        Public Function Parse(formula As String) As ParsedReaction
            If String.IsNullOrWhiteSpace(formula) Then
                Throw New ArgumentException("Reaction formula cannot be empty", NameOf(formula))
            End If

            ' Find the arrow in the formula
            Dim arrow As String = Nothing
            Dim arrowIndex = FindArrowIndex(formula, arrow)
            If arrowIndex < 0 Then
                Throw New ArgumentException($"Could not find a valid reaction arrow in formula: {formula}")
            End If

            Dim leftSide = formula.Substring(0, arrowIndex).Trim()
            Dim rightSide = formula.Substring(arrowIndex + arrow.Length).Trim()

            Dim reaction = New ParsedReaction With {
        .Arrow = arrow,
        .Reactants = ParseSide(leftSide),
        .Products = ParseSide(rightSide)
    }

            Return reaction
        End Function

        ''' <summary>
        ''' Tries to parse a reaction formula string
        ''' </summary>
        ''' <paramname="formula">The reaction formula string</param>
        ''' <paramname="result">The parsed reaction if successful</param>
        ''' <returns>True if parsing succeeded, false otherwise</returns>
        Public Function TryParse(formula As String, <Out> ByRef result As ParsedReaction) As Boolean
            Try
                result = Parse(formula)
                Return True
            Catch
                result = Nothing
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Parses a reaction formula and returns the stoichiometry as a dictionary
        ''' </summary>
        ''' <paramname="formula">The reaction formula string</param>
        ''' <returns>A dictionary mapping compound IDs to their stoichiometric coefficients</returns>
        Public Function ParseFormula(formula As String) As Dictionary(Of String, Double)
            Return Parse(formula).GetStoichiometry()
        End Function

        ''' <summary>
        ''' Finds the index of the reaction arrow in the formula
        ''' </summary>
        Private Function FindArrowIndex(formula As String, <Out> ByRef arrow As String) As Integer
            ' Sort arrows by length (longest first) to match multi-character arrows first
            For Each arr In PossibleReactionArrows.OrderByDescending(Function(a) a.Length)
                Dim index = formula.IndexOf(arr, StringComparison.Ordinal)
                If index >= 0 Then
                    arrow = arr
                    Return index
                End If
            Next

            arrow = String.Empty
            Return -1
        End Function

        ''' <summary>
        ''' Parses one side of a reaction (either reactants or products)
        ''' </summary>
        Private Function ParseSide(side As String) As List(Of ParsedCompound)
            Dim compounds = New List(Of ParsedCompound)()

            ' Split by '+' but be careful with charges like "Fe+2"
            Dim parts = SplitByPlus(side)

            For Each part In parts
                Dim trimmed = part.Trim()
                If String.IsNullOrEmpty(trimmed) Then Continue For

                Dim compound = ParseCompound(trimmed)
                If compound IsNot Nothing Then
                    compounds.Add(compound)
                End If
            Next

            Return compounds
        End Function

        ''' <summary>
        ''' Splits a string by '+' while respecting charges and other special cases
        ''' </summary>
        Private Function SplitByPlus(text As String) As List(Of String)
            Dim result = New List(Of String)()
            Dim current = New StringBuilder()
            Dim i = 0

            While i < text.Length
                ' Check if this is a separator '+'
                If text(i) = "+"c Then
                    ' Check if it's followed by whitespace (likely a separator)
                    ' or if it's at the end
                    ' Check if previous character suggests this is a separator
                    Dim isSeparator = i + 1 < text.Length AndAlso Char.IsWhiteSpace(text(i + 1)) OrElse i + 1 >= text.Length OrElse i > 0 AndAlso (Char.IsWhiteSpace(text(i - 1)) OrElse Char.IsLetterOrDigit(text(i - 1))) AndAlso (i + 1 >= text.Length OrElse Char.IsWhiteSpace(text(i + 1)) OrElse Char.IsLetter(text(i + 1)))

                    ' Additional check: if followed by a number without space, it's likely a charge
                    If i + 1 < text.Length AndAlso Char.IsDigit(text(i + 1)) AndAlso i > 0 AndAlso Not Char.IsWhiteSpace(text(i - 1)) Then
                        isSeparator = False
                    End If

                    If isSeparator Then
                        If current.Length > 0 Then
                            result.Add(current.ToString().Trim())
                            current.Clear()
                        End If
                        i += 1
                        Continue While
                    End If
                End If

                current.Append(text(i))
                i += 1
            End While

            If current.Length > 0 Then
                result.Add(current.ToString().Trim())
            End If

            Return result
        End Function

        ''' <summary>
        ''' Parses a single compound with optional coefficient and phase
        ''' </summary>
        Private Function ParseCompound(text As String) As ParsedCompound
            If String.IsNullOrWhiteSpace(text) Then Return Nothing

            Dim compound = New ParsedCompound()
            Dim remaining = text.Trim()

            ' Extract coefficient if present
            Dim coeffMatch = CoefficientPattern.Match(remaining)
            If coeffMatch.Success Then
                compound.Coefficient = Double.Parse(coeffMatch.Groups(1).Value)
                remaining = remaining.Substring(coeffMatch.Length).Trim()
            End If

            ' Extract phase if present (e.g., "(aq)", "(s)", "(g)", "(l)")
            Dim phaseMatch = PhasePattern.Match(remaining)
            If phaseMatch.Success Then
                compound.Phase = phaseMatch.Groups(1).Value
                remaining = remaining.Substring(0, remaining.Length - phaseMatch.Length).Trim()
            End If

            ' Extract compartment if present (e.g., "[c]", "[e]", "[m]")
            Dim compartmentMatch = CompartmentPattern.Match(remaining)
            If compartmentMatch.Success Then
                compound.Compartment = compartmentMatch.Groups(1).Value
                remaining = remaining.Substring(0, remaining.Length - compartmentMatch.Length).Trim()
            End If

            compound.CompoundId = remaining

            Return compound
        End Function

        ''' <summary>
        ''' Normalizes a reaction formula by standardizing whitespace and arrow format
        ''' </summary>
        ''' <paramname="formula">The reaction formula to normalize</param>
        ''' <returns>The normalized formula</returns>
        Public Function Normalize(formula As String) As String
            Dim reaction = Parse(formula)
            Dim left = String.Join(" + ", reaction.Reactants)
            Dim right = String.Join(" + ", reaction.Products)
            Return $"{left} {reaction.Arrow} {right}"
        End Function
    End Class

    ''' <summary>
    ''' Extension methods for reaction parsing
    ''' </summary>
    Public Module ReactionParserExtensions
        ''' <summary>
        ''' Parses a reaction formula string
        ''' </summary>
        <Extension()>
        Public Function ParseReaction(formula As String) As ParsedReaction
            Return New ReactionParser().Parse(formula)
        End Function

        ''' <summary>
        ''' Tries to parse a reaction formula string
        ''' </summary>
        <Extension()>
        Public Function TryParseReaction(formula As String, <Out> ByRef result As ParsedReaction) As Boolean
            Return New ReactionParser().TryParse(formula, result)
        End Function
    End Module
End Namespace
