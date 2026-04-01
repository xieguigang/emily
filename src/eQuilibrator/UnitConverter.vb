Namespace EquilibratorApi.Core.Models

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

End Namespace