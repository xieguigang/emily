Imports System.Runtime.CompilerServices

Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' Extension methods for Bounds
    ''' </summary>
    Public Module BoundsExtensions
        ''' <summary>
        ''' Gets bounds for a PhasedCompound
        ''' </summary>
        <Extension()>
        Public Function GetBounds(bounds As Bounds, compound As PhasedCompound) As (Double, Double)
            Return bounds.GetBounds(compound.CompoundId)
        End Function

        ''' <summary>
        ''' Sets bounds for a PhasedCompound
        ''' </summary>
        <Extension()>
        Public Sub SetBounds(bounds As Bounds, compound As PhasedCompound, lower As Double, upper As Double)
            bounds.SetBounds(compound.CompoundId, lower, upper)
        End Sub
    End Module
End Namespace