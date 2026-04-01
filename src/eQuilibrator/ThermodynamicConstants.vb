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

Namespace EquilibratorApi.Core.Constants

    ''' <summary>
    ''' Thermodynamic constants used in Gibbs free energy calculations.
    ''' Contains fundamental physical constants and default physiological conditions.
    ''' </summary>
    Public Module ThermodynamicConstants
        ''' <summary>
        ''' Faraday constant in kJ/(V·mol)
        ''' F = 96485.33212 C/mol = 96.48533212 kJ/(V·mol)
        ''' </summary>
        Public Const FaradayConstant As Double = 96.48533212 ' kJ/(V·mol)

        ''' <summary>
        ''' Gas constant in kJ/(K·mol)
        ''' R = 8.314462618 J/(K·mol) = 0.008314462618 kJ/(K·mol)
        ''' </summary>
        Public Const R As Double = 0.008314462618 ' kJ/(K·mol)

        ''' <summary>
        ''' Default pH value for physiological conditions
        ''' </summary>
        Public Const DefaultPH As Double = 7.5

        ''' <summary>
        ''' Default pMg (negative log of Mg2+ concentration) for physiological conditions
        ''' </summary>
        Public Const DefaultPMg As Double = 3.0

        ''' <summary>
        ''' Default ionic strength in molar (M) for physiological conditions
        ''' </summary>
        Public Const DefaultIonicStrength As Double = 0.25 ' M

        ''' <summary>
        ''' Default temperature in Kelvin for physiological conditions
        ''' </summary>
        Public Const DefaultTemperature As Double = 298.15 ' K

        ''' <summary>
        ''' Standard concentration in molar (M)
        ''' </summary>
        Public Const StandardConcentration As Double = 1.0 ' M

        ''' <summary>
        ''' Physiological concentration in molar (M)
        ''' Typically 1 mM = 0.001 M
        ''' </summary>
        Public Const PhysiologicalConcentration As Double = 0.001 ' M (1 mM)

        ''' <summary>
        ''' Default lower bound for concentration in molar (M)
        ''' </summary>
        Public Const DefaultConcentrationLowerBound As Double = 1e-6 ' M (1 μM)

        ''' <summary>
        ''' Default upper bound for concentration in molar (M)
        ''' </summary>
        Public Const DefaultConcentrationUpperBound As Double = 1e-2 ' M (10 mM)

        ''' <summary>
        ''' Default electrical potential in volts
        ''' </summary>
        Public Const DefaultElectricalPotential As Double = 0.0 ' V

        ''' <summary>
        ''' Default RMSE infinity value in kJ/mol
        ''' Used for reactions with very high uncertainty
        ''' </summary>
        Public Const DefaultRmseInf As Double = 1e5 ' kJ/mol

        ''' <summary>
        ''' Default phase for compounds
        ''' </summary>
        Public Const DefaultPhase As String = "aqueous"

        ''' <summary>
        ''' Calculates the natural logarithm of the concentration ratio.
        ''' ln(Q) where Q is the reaction quotient
        ''' </summary>
        ''' <paramname="concentration">Concentration in molar</param>
        ''' <paramname="standardConcentration">Standard concentration (default 1 M)</param>
        ''' <returns>Natural logarithm of the concentration ratio</returns>
        Public Function LnConcentrationRatio(concentration As Double, Optional standardConcentration As Double = StandardConcentration) As Double
            Return Math.Log(concentration / standardConcentration)
        End Function

        ''' <summary>
        ''' Converts pH to hydrogen ion concentration
        ''' </summary>
        ''' <paramname="pH">pH value</param>
        ''' <returns>Hydrogen ion concentration in molar</returns>
        Public Function PHToHydrogenConcentration(pH As Double) As Double
            Return Math.Pow(10, -pH)
        End Function

        ''' <summary>
        ''' Converts pMg to magnesium ion concentration
        ''' </summary>
        ''' <paramname="pMg">pMg value</param>
        ''' <returns>Magnesium ion concentration in molar</returns>
        Public Function PMgToMagnesiumConcentration(pMg As Double) As Double
            Return Math.Pow(10, -pMg)
        End Function

        ''' <summary>
        ''' Calculates RT at a given temperature
        ''' </summary>
        ''' <paramname="temperatureK">Temperature in Kelvin</param>
        ''' <returns>RT in kJ/mol</returns>
        Public Function RT(Optional temperatureK As Double = DefaultTemperature) As Double
            Return R * temperatureK
        End Function

        ''' <summary>
        ''' Calculates 2.303 * RT at a given temperature (for pH calculations)
        ''' </summary>
        ''' <paramname="temperatureK">Temperature in Kelvin</param>
        ''' <returns>2.303 * RT in kJ/mol</returns>
        Public Function Ln10RT(Optional temperatureK As Double = DefaultTemperature) As Double
            Return Math.Log(10) * R * temperatureK
        End Function
    End Module
End Namespace
