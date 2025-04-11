#Region "Microsoft.VisualBasic::d8ef026215f1f30300fe66146ee1e311, visualize\PDB_canvas\DrawingPDB.vb"

' Author:
' 
'       asuka (amethyst.asuka@gcmodeller.org)
'       xie (genetics@smrucc.org)
'       xieguigang (xie.guigang@live.com)
' 
' Copyright (c) 2018 GPL3 Licensed
' 
' 
' GNU GENERAL PUBLIC LICENSE (GPL3)
' 
' 
' This program is free software: you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation, either version 3 of the License, or
' (at your option) any later version.
' 
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
' 
' You should have received a copy of the GNU General Public License
' along with this program. If not, see <http://www.gnu.org/licenses/>.



' /********************************************************************************/

' Summaries:

' Module DrawingPDB
' 
'     Function: MolDrawing
' 
'     Sub: __drawingOfAA
' 
' /********************************************************************************/

#End Region

Imports System.Drawing
Imports Microsoft.VisualBasic.Drawing
Imports Microsoft.VisualBasic.Imaging
Imports Microsoft.VisualBasic.Imaging.Drawing2D.Colors.Scaler
Imports Microsoft.VisualBasic.Imaging.Drawing3D.Math3D
Imports Microsoft.VisualBasic.Imaging.Driver
Imports SMRUCC.genomics.Data.RCSB.PDB

#If NET48 Then
Imports Font = System.Drawing.Font
Imports Pen = System.Drawing.Pen
Imports Brush = System.Drawing.Brush
Imports Image = System.Drawing.Image
Imports Pens = System.Drawing.Pens
Imports Brushes = System.Drawing.Brushes
#Else
Imports Font = Microsoft.VisualBasic.Imaging.Font
Imports Pen = Microsoft.VisualBasic.Imaging.Pen
Imports Brush = Microsoft.VisualBasic.Imaging.Brush
Imports Image = Microsoft.VisualBasic.Imaging.Image
Imports Pens = Microsoft.VisualBasic.Imaging.Pens
Imports Brushes = Microsoft.VisualBasic.Imaging.Brushes
#End If

''' <summary>
''' Visualize the protein 3D structure from the PDB file.
''' </summary>
Public Class DrawingPDB : Inherits plot

    Public Property XRotation As Double = 60
    Public Property ScaleFactor As Double = 20
    Public Property penWidth As Integer = 10
    Public Property hideAtoms As Boolean = True
    Public Property DisplayAAID As Boolean = True

    ReadOnly pdb As PDB

    Shared Sub New()
#If NET8_0_OR_GREATER Then
        Call Microsoft.VisualBasic.Drawing.SkiaDriver.Register()
#End If
    End Sub

    Sub New(pdb As PDB)
        Me.pdb = pdb
    End Sub

    ''' <summary>
    ''' Drawing a protein structure from its pdb data.
    ''' </summary>
    ''' <returns></returns>
    Public Function MolDrawing() As GraphicsData
        Dim Device As IGraphics = DriverLoad.CreateGraphicsDevice(New Size(3000, 3000))
        Dim offset As New Point(Device.Width / 2, Device.Height / 2)
        Dim AASequence As AminoAcid() = pdb.AminoAcidSequenceData
        Dim PreAA As AminoAcid = AASequence.First
        Dim PrePoint As PointF
        Dim aas As String() = (From AA In AASequence Select AA.AA_ID Distinct).ToArray
        Dim AAColors = New CategoryColorProfile(aas, "paper").GetTermColors.ToDictionary(Function(a) a.Name, Function(a) New Pen(a.Value, penWidth))
        Dim AAFont As New Font(FontFace.MicrosoftYaHei, 10)
        Dim pt2d As PointF

        Call __drawingOfAA(PreAA, PrePoint, offset, Device, DisplayAAID, AAFont, hideAtoms) ' 绘制第一个碳原子

        For Each Point As AminoAcid In AASequence


            Call __drawingOfAA(Point, pt2d, offset, Device, DisplayAAID, AAFont, hideAtoms)
            Call Device.DrawLine(AAColors(Point.AA_ID), pt2d, PrePoint)

            PrePoint = pt2d
        Next

        Dim Max = pdb.MaxSpace
        Dim Min = pdb.MinSpace

        Call Device.DrawLine(Pens.Black, New Drawing3D.Point3D(Max.X * ScaleFactor, 0, 0).SpaceToGrid(XRotation, offset), New Drawing3D.Point3D(Min.Y * ScaleFactor, 0, 0).SpaceToGrid(XRotation, offset)) 'X
        Call Device.DrawLine(Pens.Black, New Drawing3D.Point3D(0, Max.Y * ScaleFactor, 0).SpaceToGrid(XRotation, offset), New Drawing3D.Point3D(0, Min.Y * ScaleFactor, 0).SpaceToGrid(XRotation, offset)) 'Y
        Call Device.DrawLine(Pens.Black, New Drawing3D.Point3D(0, 0, Max.Z * ScaleFactor).SpaceToGrid(XRotation, offset), New Drawing3D.Point3D(0, 0, Min.Z * ScaleFactor).SpaceToGrid(XRotation, offset)) 'Z

        Return Device.ImageResource
    End Function

    Private Sub __drawingOfAA(AA As AminoAcid, ByRef pt2d As PointF, offset As Point, Device As Graphics2D, DisplayAAID As Boolean, AAFont As Font, hideAtoms As Boolean)
        Dim Carbon As Keywords.AtomUnit = AA.Carbon
        Dim pt3d As New Drawing3D.Point3D(Carbon.Location.X * ScaleFactor, Carbon.Location.Y * ScaleFactor, Carbon.Location.Z * ScaleFactor)
        pt2d = pt3d.SpaceToGrid(xRotate:=XRotation, offset:=offset)
        Call Device.FillEllipse(Brushes.Black, New RectangleF(pt2d, New Size(penWidth, penWidth)))

        If DisplayAAID Then
            Call Device.DrawString(AA.AA_ID, AAFont, Brushes.Gray, pt2d)
        End If

        If hideAtoms Then Return

        For Each Atom In AA.Atoms
            Dim pt = New Drawing3D.Point3D(Atom.Location.X * ScaleFactor, Atom.Location.Y * ScaleFactor, Atom.Location.Z * ScaleFactor)
            Dim pt22d = pt.SpaceToGrid(XRotation, offset)
            Call Device.DrawLine(Pens.Gray, pt22d, pt2d)
        Next
    End Sub
End Class
