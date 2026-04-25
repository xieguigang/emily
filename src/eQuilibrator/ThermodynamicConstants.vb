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

Imports eQuilibrator.EquilibratorApi.Core.Models

Namespace EquilibratorApi.Core.Constants

    ''' <summary>
    ''' Thermodynamic constants used in Gibbs free energy calculations.
    ''' Contains fundamental physical constants and default physiological conditions.
    ''' </summary>
    Public Module ThermodynamicConstants

        ' 热力学常数和通用计算模块。
        ' 包含所有热力学常数、Debye-Hückel 方程和 Legendre 变换。

        ' =========================================================================
        ' 常数定义
        ' =========================================================================

        ''' <summary>默认温度 298.15 K</summary>
        Public ReadOnly default_T As Quantity = Q_(298.15, "K")

        ''' <summary>默认温度（开尔文）</summary>
        Public ReadOnly default_T_in_K As Double = 298.15

        ''' <summary>默认离子强度 0.25 M</summary>
        Public ReadOnly default_I As Quantity = Q_(0.25, "M")

        ''' <summary>默认离子强度（摩尔/升）</summary>
        Public ReadOnly default_I_in_M As Double = 0.25

        ''' <summary>默认 pH 值 7.0</summary>
        Public ReadOnly default_pH As Double = 7.0

        ''' <summary>默认 pMg 值 14.0</summary>
        Public ReadOnly default_pMg As Double = 14.0

        ''' <summary>默认 pMg（Quantity 形式）</summary>
        Public ReadOnly default_pMg_Quantity As Quantity = Q_(14.0, "")

        ''' <summary>气体常数 R = 8.314462618 J/(mol*K) = 8.314462618e-3 kJ/(mol*K)</summary>
        Public ReadOnly R As Quantity = Q_(0.008314462618, "kJ/mol/K")

        ''' <summary>气体常数（kJ/(mol*K)）</summary>
        Public ReadOnly default_R_in_kJ_per_mol_per_K As Double = 0.008314462618

        ''' <summary>法拉第常数 96485.33212 C/mol</summary>
        Public ReadOnly FARADAY As Quantity = Q_(96.48533212, "kJ/mol/V")

        ''' <summary>
        ''' LOG10 = ln(10) = 2.302585...
        ''' 等价于 Python LOG10 = np.log(10)。
        ''' 注意：这是自然对数 ln(10)，不是 log10(e)。
        ''' </summary>
        Public ReadOnly LOG10 As Double = Math.Log(10.0)

        ''' <summary>Mg2+ 的标准生成 Gibbs 自由能 (kJ/mol)</summary>
        Public ReadOnly standard_dg_formation_mg As Double = -455.3

        ''' <summary>Mg2+ 的标准生成焓 (kJ/mol)</summary>
        Public ReadOnly standard_dh_formation_mg As Double = -466.9

        ' =========================================================================
        ' Debye-Hückel 方程
        ' =========================================================================

        ''' <summary>
        ''' 计算 Debye-Hückel 修正因子。
        ''' 等价于 Python debye_hueckel(ionic_strength_in_M, T_in_K)。
        ''' 
        ''' 该函数基于扩展 Debye-Hückel 方程计算活度系数修正因子，
        ''' 用于校正离子强度对标准 Gibbs 自由能的影响。
        ''' 修正因子的单位为 kJ/mol。
        ''' </summary>
        ''' <param name="ionic_strength_in_M">离子强度（摩尔/升）</param>
        ''' <param name="T_in_K">温度（开尔文）</param>
        ''' <returns>Debye-Hückel 修正因子 (kJ/mol)</returns>
        Public Function DebyeHueckel(ionic_strength_in_M As Double, T_in_K As Double) As Double
            ' Debye-Hückel 常数
            Dim _a1 As Double = 0.00920483   ' kJ / mol / M^0.5 / K
            Dim _a2 As Double = 0.00001284668   ' kJ / mol / M^0.5 / K^2
            Dim _a3 As Double = 0.0000000495199    ' kJ / mol / M^0.5 / K^3
            Dim B As Double = 1.6             ' 1 / M^0.5

            ' 计算 alpha 系数
            Dim alpha As Double = _a1 * T_in_K - _a2 * T_in_K ^ 2 + _a3 * T_in_K ^ 3

            ' 计算 Debye-Hückel 修正因子
            Dim sqrtI As Double = Math.Sqrt(ionic_strength_in_M)
            Return -alpha * sqrtI / (1.0 + B * sqrtI)
        End Function

        ''' <summary>
        ''' 计算 Debye-Hückel 修正因子对离子强度的导数。
        ''' 等价于 Python _debye_hueckel_derivative(ionic_strength, T_in_K)。
        ''' </summary>
        ''' <param name="ionic_strength">离子强度（摩尔/升）</param>
        ''' <param name="T_in_K">温度（开尔文）</param>
        ''' <returns>Debye-Hückel 修正因子对离子强度的导数 (kJ/mol)</returns>
        Public Function DebyeHueckelDerivative(ionic_strength As Double, T_in_K As Double) As Double
            Dim _a1 As Double = 0.00920483
            Dim _a2 As Double = 0.00001284668
            Dim _a3 As Double = 0.0000000495199
            Dim B As Double = 1.6

            Dim alpha As Double = _a1 * T_in_K - _a2 * T_in_K ^ 2 + _a3 * T_in_K ^ 3
            Return 0.5 * alpha * ionic_strength ^ (-1.5) / (ionic_strength ^ (-0.5) + B) ^ 2
        End Function

        ' =========================================================================
        ' Legendre 变换
        ' =========================================================================

        ''' <summary>
        ''' 计算 Legendre 变换值。
        ''' 等价于 Python _legendre_transform(pH, pMg, ionic_strength_M, T_in_K, charge, num_protons, num_magnesiums)。
        ''' 
        ''' Legendre 变换用于将标准 Gibbs 自由能转换为指定 pH、pMg 和离子强度条件下的
        ''' 变换 Gibbs 自由能。这是 Alberty 热力学框架的核心计算。
        ''' </summary>
        ''' <param name="pH">pH 值（-log10[H+])</param>
        ''' <param name="pMg">pMg 值（-log10[Mg2+])</param>
        ''' <param name="ionic_strength_M">离子强度（摩尔/升）</param>
        ''' <param name="T_in_K">温度（开尔文）</param>
        ''' <param name="charge">微粒子的电荷数</param>
        ''' <param name="num_protons">质子数</param>
        ''' <param name="num_magnesiums">镁离子数</param>
        ''' <returns>Legendre 变换值（单位为 RT）</returns>
        Public Function LegendreTransform(pH As Double, pMg As Double,
                                       ionic_strength_M As Double, T_in_K As Double,
                                       charge As Double, num_protons As Double,
                                       num_magnesiums As Double) As Double
            Dim RT As Double = default_R_in_kJ_per_mol_per_K * T_in_K

            ' 质子项
            Dim proton_term As Double = num_protons * RT * LOG10 * pH

            ' 镁离子项
            Dim _dg_mg As Double = (T_in_K / default_T_in_K) * standard_dg_formation_mg +
                               (1.0 - T_in_K / default_T_in_K) * standard_dh_formation_mg
            Dim magnesium_term As Double = num_magnesiums * (RT * LOG10 * pMg - _dg_mg)

            ' 离子强度项
            Dim is_term As Double
            If ionic_strength_M > 0 Then
                Dim dh_factor As Double = DebyeHueckel(ionic_strength_M, T_in_K)
                is_term = dh_factor * (charge ^ 2 - num_protons - 4 * num_magnesiums)
            Else
                is_term = 0.0
            End If

            Return (proton_term + magnesium_term - is_term) / RT
        End Function

        ''' <summary>
        ''' Faraday constant in kJ/(V·mol)
        ''' F = 96485.33212 C/mol = 96.48533212 kJ/(V·mol)
        ''' </summary>
        Public Const FaradayConstant As Double = 96.48533212 ' kJ/(V·mol)

        ''' <summary>
        ''' Gas constant in kJ/(K·mol)
        ''' R = 8.314462618 J/(K·mol) = 0.008314462618 kJ/(K·mol)
        ''' </summary>
       ' Public Const R As Double = 0.008314462618 ' kJ/(K·mol)

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
        Public Const DefaultConcentrationLowerBound As Double = 0.000001 ' M (1 μM)

        ''' <summary>
        ''' Default upper bound for concentration in molar (M)
        ''' </summary>
        Public Const DefaultConcentrationUpperBound As Double = 0.01 ' M (10 mM)

        ''' <summary>
        ''' Default electrical potential in volts
        ''' </summary>
        Public Const DefaultElectricalPotential As Double = 0.0 ' V

        ''' <summary>
        ''' Default RMSE infinity value in kJ/mol
        ''' Used for reactions with very high uncertainty
        ''' </summary>
        Public Const DefaultRmseInf As Double = 100000.0 ' kJ/mol

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
