Imports System.ComponentModel

Public Class RenderArguments

    <Category("3D Camera")>
    <DisplayName("View X")>
    <Description("3D camera view point for make the 3d pdb model to 2d plot projection")>
    Public Property viewX As Double
    <Category("3D Camera")>
    <DisplayName("View Y")>
    <Description("3D camera view point for make the 3d pdb model to 2d plot projection")>
    Public Property viewY As Double
    <Category("3D Camera")>
    <DisplayName("View Z")>
    <Description("3D camera view point for make the 3d pdb model to 2d plot projection")>
    Public Property viewZ As Double

    <Category("Ligand Model")>
    <DisplayName("Distance Cutoff")>
    <Description("The distance cutoff value of ligand atom to the amino acid residue, in data unit Å, default value is 3.5Å")>
    Public Property DistanceCutoff As Double = 3.5
    <Category("Ligand Model")>
    <DisplayName("KNN")>
    <Description("Select closed top n ligand atom and amino acid residue for display the interaction. defult choose the top one closest.")>
    Public Property TopRank As Integer = 1
    <Category("Ligand Render")>
    <DisplayName("Ligand Atom Size")>
    <Description("The shape size of the ligand atom.")>
    Public Property AtomSize As Single = 95
    <Category("Ligand Render")>
    <DisplayName("Amino Acid Size")>
    <Description("The shape size of the amino acid residue.")>
    Public Property AminoAcidSize As Single = 150

    <Category("Ligand Render")>
    <DisplayName("Show Liagnd Atom Label")>
    <Description("Show or hide the ligand molecule atom label on the graphics plot?")>
    Public Property ShowLigandAtomLabel As Boolean = True
    <Category("Ligand Render")>
    <DisplayName("Amino Acid Min Font Size")>
    <Description("The min size of the font label for show the amino acid residue label")>
    Public Property AminoAcidFontSizeMin As Double = 12
    <Category("Ligand Render")>
    <DisplayName("Amino Acid Max Font Size")>
    <Description("The max size of the font label for show the amino acid residue label")>
    Public Property AminoAcidFontSizeMax As Double = 40

End Class
