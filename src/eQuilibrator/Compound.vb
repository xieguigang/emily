Imports eQuilibrator.EquilibratorApi.Core.Constants

Namespace EquilibratorApi.Core.Models
    ''' <summary>
    ''' Represents a compound with additional metadata from a database
    ''' </summary>
    Public Class Compound
        ''' <summary>
        ''' The unique identifier for this compound
        ''' </summary>
        Public Property Id As String = String.Empty

        ''' <summary>
        ''' The common name of this compound
        ''' </summary>
        Public Property Name As String

        ''' <summary>
        ''' The InChI key
        ''' </summary>
        Public Property InchiKey As String

        ''' <summary>
        ''' The InChI string
        ''' </summary>
        Public Property Inchi As String

        ''' <summary>
        ''' The SMILES representation
        ''' </summary>
        Public Property Smiles As String

        ''' <summary>
        ''' The molecular formula
        ''' </summary>
        Public Property Formula As String

        ''' <summary>
        ''' The molecular weight in g/mol
        ''' </summary>
        Public Property MolecularWeight As Double

        ''' <summary>
        ''' The charge of the compound
        ''' </summary>
        Public Property Charge As Integer

        ''' <summary>
        ''' The number of protons
        ''' </summary>
        Public Property ProtonCount As Integer

        ''' <summary>
        ''' The atom composition
        ''' </summary>
        Public Property AtomBag As Dictionary(Of String, Integer) = New Dictionary(Of String, Integer)()

        ''' <summary>
        ''' The standard formation Gibbs energy in kJ/mol
        ''' </summary>
        Public Property StandardFormationEnergy As Double

        ''' <summary>
        ''' Whether this compound is a proton
        ''' </summary>
        Public Property IsProton As Boolean

        ''' <summary>
        ''' Whether this compound is water
        ''' </summary>
        Public Property IsWater As Boolean

        ''' <summary>
        ''' Creates a PhasedCompound from this Compound
        ''' </summary>
        ''' <paramname="phase">The phase for the compound</param>
        ''' <returns>A new PhasedCompound</returns>
        Public Function ToPhasedCompound(Optional phase As String = ThermodynamicConstants.DefaultPhase) As PhasedCompound
            Return New PhasedCompound(Id, phase) With {
        .Name = Name,
        .InchiKey = InchiKey,
        .Inchi = Inchi,
        .Smiles = Smiles,
        .MolecularWeight = MolecularWeight,
        .Charge = Charge,
        .ProtonCount = ProtonCount,
        .AtomBag = New Dictionary(Of String, Integer)(AtomBag),
        .StandardFormationEnergy = StandardFormationEnergy,
        .IsProton = IsProton,
        .IsWater = IsWater
    }
        End Function
    End Class
End Namespace