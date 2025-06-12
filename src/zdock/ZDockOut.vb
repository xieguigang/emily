
Imports System.IO
Imports Microsoft.VisualBasic.Language
Imports SMRUCC.genomics.Data.RCSB.PDB.Keywords

''' <summary>
''' # File Structure for Molecular Docking Configuration
''' 
''' ## Overview
''' 
''' This document describes the structure of a configuration file used in molecular docking
''' simulations, specifically employing the ZDock scoring system. The file format defines
''' the parameters and initial conditions for the interaction between a receptor and a ligand
''' molecule.
''' 
''' ## File Structure Details
''' 
''' ### Line 1: Grid and Simulation Parameters
''' 
''' The first line contains three essential parameters for the molecular docking simulation:
''' 
''' - **Maximum grid points**: Specifies the size of the grid used for spatial discretization of the molecular interaction space
''' - **Grid spacing**: Defines the step size for molecular movement in space, representing the distance between consecutive positions in the search space
''' - **Rigid molecule flag**: Set to 0 to indicate that the receptor molecule remains rigid during the docking process
''' 
''' These parameters establish the computational grid that will be used to sample possible
''' interactions between the receptor and ligand molecules.
''' 
''' ### Line 2: Receptor Initial Orientation
''' 
''' The second line contains the initial Euler angles of the receptor molecule. These three 
''' angles define the initial orientation of the receptor molecule in 3D space before the 
''' docking simulation begins.
''' 
''' ### Line 3: Ligand Initial Orientation
''' 
''' The third line contains the initial Euler angles of the ligand molecule. These angles 
''' establish the initial orientation of the ligand molecule relative to the receptor at the 
''' start of the simulation.
''' 
''' ### Line 4: Receptor Position
''' 
''' The fourth line includes:
''' 
''' - The filename of the receptor molecule file
''' - Position parameters that define the receptor's location relative to the coordinate system 
'''   center, typically represented as Cartesian coordinates (x, y, z)
'''   
''' These parameters establish the spatial position of the receptor molecule's center in the simulation space.
''' 
''' ### Line 5: Ligand Initial Position
''' 
''' The fifth line includes:
''' 
''' - The filename of the ligand molecule file
''' - Position parameters that define the ligand's initial location relative to the coordinate system center
''' 
''' These parameters establish the starting spatial position of the ligand molecule's center 
''' before the docking simulation begins.
''' 
''' ### Subsequent Lines: Docking Conformations and Scores
''' 
''' Each line after the fifth contains information describing a specific conformation and 
''' scoring result for the ligand relative to the receptor:
''' 
''' - **First three numbers**: Euler angles representing the rotation of the ligand relative to its initial position
''' - **Next three numbers**: Translation vector indicating the linear displacement of the ligand's center relative to its initial position
''' - **Last number**: ZDock scoring value, where a higher score indicates better docking quality and a more favorable interaction
''' 
''' These subsequent lines typically represent either:
''' 
''' 1. A series of possible docking poses that were evaluated during the simulation
''' 2. The trajectory of the ligand as it moves through the search space
''' 
''' ## Technical Notes
''' 
''' - The Euler angles are typically represented in radians or degrees, depending on the specific implementation of the docking software
''' - The coordinate system is usually Cartesian, with the origin at the center of the receptor binding site
''' - The ZDock scoring function combines various energetic and geometric components to evaluate the quality of molecular interactions, including hydrogen bonding, electrostatic interactions, and shape complementarity
''' - The grid spacing parameter directly affects the resolution of the search space and computational requirements of the docking simulation
''' 
''' This file format appears to be specific to ZDock or a similar molecular docking
''' software package, which is widely used for predicting protein-protein and 
''' protein-ligand interactions in computational biology and drug discovery. 
''' </summary>
Public Class ZDockOut

    ''' <summary>
    ''' **Maximum grid points**: Specifies the size of the grid used for spatial discretization of the molecular interaction space
    ''' </summary>
    ''' <returns></returns>
    Public Property MaximumGridPoints As Double
    ''' <summary>
    ''' **Grid spacing**: Defines the step size for molecular movement in space, representing the distance between consecutive positions in the search space
    ''' </summary>
    ''' <returns></returns>
    Public Property GridSpacing As Double
    ''' <summary>
    ''' **Rigid molecule flag**: Set to 0 to indicate that the receptor molecule remains rigid during the docking process
    ''' </summary>
    ''' <returns></returns>
    Public Property RigidMoleculeFlag As Double

    ''' <summary>
    ''' The second line contains the initial Euler angles of the receptor molecule. These three 
    ''' angles define the initial orientation of the receptor molecule in 3D space before the 
    ''' docking simulation begins.
    ''' </summary>
    ''' <returns></returns>
    Public Property ReceptorOrientation As Point3D
    ''' <summary>
    ''' The third line contains the initial Euler angles of the ligand molecule. These angles 
    ''' establish the initial orientation of the ligand molecule relative to the receptor at the 
    ''' start of the simulation.
    ''' </summary>
    ''' <returns></returns>
    Public Property LigandOrientation As Point3D

    ''' <summary>
    ''' The filename of the receptor molecule file
    ''' </summary>
    ''' <returns></returns>
    Public Property receptorMoleculeFile As String
    ''' <summary>
    ''' Position parameters that define the receptor's location relative to the coordinate system 
    ''' center, typically represented as Cartesian coordinates (x, y, z)
    ''' </summary>
    ''' <returns></returns>
    Public Property receptorLocation As Point3D

    ''' <summary>
    ''' The filename of the ligand molecule file
    ''' </summary>
    ''' <returns></returns>
    Public Property ligandMoleculeFile As String
    ''' <summary>
    ''' Position parameters that define the ligand's initial location relative to the coordinate system center
    ''' </summary>
    ''' <returns></returns>
    Public Property ligandLocation As Point3D

    Public Property complexes As Complex()

    Public ReadOnly Property topRankComplex As Complex
        Get
            Return complexes.ElementAtOrNull(Scan0)
        End Get
    End Property

    Public Shared Function Parse(s As Stream) As ZDockOut
        Return Parse(New StreamReader(s))
    End Function

    Public Shared Function Parse(s As String) As ZDockOut
        Return Parse(New StringReader(s))
    End Function

    Public Shared Function Parse(out As TextReader) As ZDockOut
        Dim zdock As New ZDockOut
        Dim line As String() = out.ReadLine.StringSplit("\t")
        Dim str As Value(Of String) = ""
        Dim ranking As New List(Of Complex)

        zdock.MaximumGridPoints = Val(line(0))
        zdock.GridSpacing = Val(line(1))
        zdock.RigidMoleculeFlag = Val(line(2))

        line = out.ReadLine.StringSplit("\t")
        zdock.ReceptorOrientation = New Point3D(Val(line(0)), Val(line(1)), Val(line(2)))

        line = out.ReadLine.StringSplit("\t")
        zdock.LigandOrientation = New Point3D(Val(line(0)), Val(line(1)), Val(line(2)))

        line = out.ReadLine.StringSplit("\t")
        zdock.receptorMoleculeFile = line(0)
        zdock.receptorLocation = New Point3D(Val(line(1)), Val(line(2)), Val(line(3)))

        line = out.ReadLine.StringSplit("\t")
        zdock.ligandMoleculeFile = line(0)
        zdock.ligandLocation = New Point3D(Val(line(1)), Val(line(2)), Val(line(3)))

        Do While (str = out.ReadLine) IsNot Nothing
            Dim complex As New Complex

            line = CStr(str).StringSplit("\t")
            complex.ligandRotation = New Point3D(Val(line(0)), Val(line(1)), Val(line(2)))
            complex.translationVector = New Point3D(Val(line(3)), Val(line(4)), Val(line(5)))
            complex.ZDockScore = Val(line(6))
            ranking.Add(complex)
        Loop

        zdock.complexes = ranking.ToArray

        Return zdock
    End Function

End Class

Public Class Complex

    ''' <summary>
    ''' Euler angles representing the rotation of the ligand relative to its initial position
    ''' </summary>
    ''' <returns></returns>
    Public Property ligandRotation As Point3D
    ''' <summary>
    ''' Translation vector indicating the linear displacement of the ligand's center relative to its initial position
    ''' </summary>
    ''' <returns></returns>
    Public Property translationVector As Point3D
    ''' <summary>
    ''' ZDock scoring value, where a higher score indicates better docking quality and a more favorable interaction
    ''' </summary>
    ''' <returns></returns>
    Public Property ZDockScore As Double

End Class