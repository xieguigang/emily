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

Imports eQuilibrator.EquilibratorApi.Core.Constants

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Represents a compound with phase information for thermodynamic calculations.
    ''' This class wraps a base compound and adds phase-specific properties.
    ''' </summary>
    Public Class PhasedCompound

        ''' <summary>
        ''' Phase names for different compound states
        ''' </summary>
        Public Const AqueousPhase As String = "aqueous"
        Public Const GasPhase As String = "gas"
        Public Const LiquidPhase As String = "liquid"
        Public Const SolidPhase As String = "solid"
        Public Const RedoxPhase As String = "redox"

        ''' <summary>
        ''' Gets the compound identifier
        ''' </summary>
        Public ReadOnly Property CompoundId As String

        ''' <summary>
        ''' Gets the compound name
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' Gets the phase of this compound (e.g., "aqueous", "gas", "solid")
        ''' </summary>
        Public ReadOnly Property Phase As String

        ''' <summary>
        ''' Gets the concentration of this compound in molar
        ''' </summary>
        Public Property Concentration As Double
        ''' <summary>
        ''' Gets the electrical potential for redox compounds in volts
        ''' </summary>
        Public Property ElectricalPotential As Double
        ''' <summary>
        ''' Gets the partial pressure for gas compounds in atm
        ''' </summary>
        Public Property PartialPressure As Double

        ''' <summary>
        ''' Gets the standard formation Gibbs energy in kJ/mol
        ''' </summary>
        Public Property StandardFormationEnergy As Double

        ''' <summary>
        ''' Gets the atom composition of this compound
        ''' </summary>
        Public Property AtomBag As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()

        ''' <summary>
        ''' Gets the number of protons (H+) in this compound
        ''' </summary>
        Public Property ProtonCount As Integer

        ''' <summary>
        ''' Gets the number of electrons in this compound
        ''' </summary>
        Public Property ElectronCount As Integer

        ''' <summary>
        ''' Gets the charge of this compound
        ''' </summary>
        Public Property Charge As Integer

        ''' <summary>
        ''' Gets the molecular weight in g/mol
        ''' </summary>
        Public Property MolecularWeight As Double

        ''' <summary>
        ''' Gets the inchi key for this compound
        ''' </summary>
        Public Property InchiKey As String

        ''' <summary>
        ''' Gets the inchi string for this compound
        ''' </summary>
        Public Property Inchi As String

        ''' <summary>
        ''' Gets the SMILES representation of this compound
        ''' </summary>
        Public Property Smiles As String

        ''' <summary>
        ''' Gets the abundance (concentration relative to standard state)
        ''' </summary>
        Public Property Abundance As Double = 1.0

        ''' <summary>
        ''' Gets whether this compound is a proton (H+)
        ''' </summary>
        Public Property IsProton As Boolean

        ''' <summary>
        ''' Gets whether this compound is water (H2O)
        ''' </summary>
        Public Property IsWater As Boolean

        ''' <summary>
        ''' Creates a new PhasedCompound
        ''' </summary>
        ''' <paramname="compoundId">The compound identifier</param>
        ''' <paramname="phase">The phase of the compound</param>
        Public Sub New(compoundId As String, Optional phase As String = ThermodynamicConstants.DefaultPhase)
            Me.CompoundId = compoundId
            Me.Phase = If(phase, DefaultPhase)
            Concentration = PhysiologicalConcentration
        End Sub

        ''' <summary>
        ''' Creates a copy of this compound with a different phase
        ''' </summary>
        ''' <paramname="newPhase">The new phase</param>
        ''' <returns>A new PhasedCompound with the specified phase</returns>
        Public Function WithPhase(newPhase As String) As PhasedCompound
            Return New PhasedCompound(CompoundId, newPhase) With {
        .Name = Name,
        .Concentration = Concentration,
        .ElectricalPotential = ElectricalPotential,
        .PartialPressure = PartialPressure,
        .StandardFormationEnergy = StandardFormationEnergy,
        .AtomBag = New Dictionary(Of String, Integer)(AtomBag),
        .ProtonCount = ProtonCount,
        .ElectronCount = ElectronCount,
        .Charge = Charge,
        .MolecularWeight = MolecularWeight,
        .InchiKey = InchiKey,
        .Inchi = Inchi,
        .Smiles = Smiles,
        .Abundance = Abundance,
        .IsProton = IsProton,
        .IsWater = IsWater
    }
        End Function

        ''' <summary>
        ''' Sets the concentration for this compound
        ''' </summary>
        ''' <paramname="concentration">Concentration in molar</param>
        ''' <returns>This compound for method chaining</returns>
        Public Function SetConcentration(concentration As Double) As PhasedCompound
            Me.Concentration = concentration
            Abundance = concentration / StandardConcentration
            Return Me
        End Function

        ''' <summary>
        ''' Sets the concentration using a Quantity
        ''' </summary>
        ''' <paramname="concentration">Concentration as a Quantity</param>
        ''' <returns>This compound for method chaining</returns>
        Public Function SetConcentration(concentration As Quantity) As PhasedCompound
            Return SetConcentration(concentration.GetValueIn("M"))
        End Function

        ''' <summary>
        ''' Sets the electrical potential for redox compounds
        ''' </summary>
        ''' <paramname="potential">Electrical potential in volts</param>
        ''' <returns>This compound for method chaining</returns>
        Public Function SetElectricalPotential(potential As Double) As PhasedCompound
            ElectricalPotential = potential
            Return Me
        End Function

        ''' <summary>
        ''' Sets the partial pressure for gas compounds
        ''' </summary>
        ''' <paramname="pressure">Partial pressure in atm</param>
        ''' <returns>This compound for method chaining</returns>
        Public Function SetPartialPressure(pressure As Double) As PhasedCompound
            PartialPressure = pressure
            Abundance = pressure ' For gases, abundance is relative to 1 atm
            Return Me
        End Function

        ''' <summary>
        ''' Gets the natural logarithm of the abundance for thermodynamic calculations
        ''' </summary>
        Public ReadOnly Property LnAbundance As Double
            Get
                If Equals(Phase, GasPhase) Then
                    Return Math.Log(PartialPressure)
                End If
                Return Math.Log(Concentration / StandardConcentration)
            End Get
        End Property

        ''' <summary>
        ''' Gets the natural logarithm of the physiological abundance
        ''' </summary>
        Public ReadOnly Property LnPhysiologicalAbundance As Double
            Get
                If Equals(Phase, GasPhase) Then
                    Return Math.Log(1.0) ' 1 atm
                End If
                Return Math.Log(PhysiologicalConcentration / StandardConcentration)
            End Get
        End Property

        ''' <summary>
        ''' Checks if the abundance is at physiological levels
        ''' </summary>
        Public ReadOnly Property IsPhysiological As Boolean
            Get
                If Equals(Phase, GasPhase) Then
                    Return Math.Abs(If(PartialPressure = 0, 1.0, PartialPressure) - 1.0) < 0.0000000001
                End If
                Return Math.Abs(Concentration - PhysiologicalConcentration) < 0.0000000001
            End Get
        End Property

        ''' <summary>
        ''' Gets the standard formation Gibbs energy at the given conditions
        ''' </summary>
        ''' <paramname="pH">The pH value</param>
        ''' <paramname="ionicStrength">The ionic strength in molar</param>
        ''' <paramname="temperature">The temperature in Kelvin</param>
        ''' <paramname="pMg">The pMg value</param>
        ''' <returns>The standard formation Gibbs energy in kJ/mol</returns>
        Public Function GetStandardFormationEnergyPrime(Optional pH As Double = DefaultPH, Optional ionicStrength As Double = DefaultIonicStrength, Optional temperature As Double = DefaultTemperature, Optional pMg As Double = DefaultPMg) As Double
            ' For now, return the standard formation energy
            ' A full implementation would include pH, ionic strength, and temperature corrections
            Return StandardFormationEnergy
        End Function

        ''' <summary>
        ''' Serializes this compound to a dictionary
        ''' </summary>
        ''' <returns>A dictionary representation of this compound</returns>
        Public Function Serialize() As Dictionary(Of String, Object)
            Dim result = New Dictionary(Of String, Object) From {
        {"compound_id", CompoundId},
        {"phase", Phase},
        {"concentration", Concentration}
    }

            If Not String.IsNullOrEmpty(Name) Then result("name") = Name

            result("electrical_potential") = ElectricalPotential

            result("partial_pressure") = PartialPressure

            Return result
        End Function

        Public Overrides Function ToString() As String
            Dim phase = If(Not Equals(Me.Phase, DefaultPhase), $" ({Me.Phase})", "")
            Return $"{CompoundId}{phase}"
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            Dim other As PhasedCompound = TryCast(obj, PhasedCompound)
            Return other IsNot Nothing AndAlso Equals(CompoundId, other.CompoundId) AndAlso Equals(Phase, other.Phase)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(CompoundId, Phase)
        End Function

        ''' <summary>
        ''' Creates a proton (H+) compound
        ''' </summary>
        Public Shared Function CreateProton() As PhasedCompound
            Return New PhasedCompound("H+", AqueousPhase) With {
        .Name = "H+",
        .Charge = 1,
        .ProtonCount = 1,
        .IsProton = True,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"H", 1}
            }
    }
        End Function

        ''' <summary>
        ''' Creates a water (H2O) compound
        ''' </summary>
        Public Shared Function CreateWater() As PhasedCompound
            Return New PhasedCompound("H2O", LiquidPhase) With {
        .Name = "H2O",
        .Charge = 0,
        .ProtonCount = 2,
        .IsWater = True,
        .MolecularWeight = 18.015,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"H", 2},
                {"O", 1}
            }
    }
        End Function

        ''' <summary>
        ''' Creates an electron compound for redox reactions
        ''' </summary>
        Public Shared Function CreateElectron() As PhasedCompound
            Return New PhasedCompound("e-", RedoxPhase) With {
        .Name = "e-",
        .Charge = -1,
        .ElectronCount = 1,
        .AtomBag = New Dictionary(Of String, Integer) From {
                {"e-", 1}
            }
    }
        End Function
    End Class

End Namespace
