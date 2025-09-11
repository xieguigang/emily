Imports ligplus.pdb

Namespace ligplus

    Public Class SizeSelect
        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared CANCEL As Integer = 0

        Public Shared OK As Integer = 1

        Public Shared APPLY_TO_ALL As Integer = 2

        Public Shared ABODY_LOOP_LABEL_SIZE As Integer = 0

        Public Shared EXTBOND_WIDTH As Integer = 1

        Public Shared EXTBOND_DIM_WIDTH As Integer = 2

        Public Shared HBOND_WIDTH As Integer = 3

        Public Shared HBOND_DIM_WIDTH As Integer = 4

        Public Shared HPHOBIC_RADIUS As Integer = 5

        Public Shared HBTEXT_SIZE As Integer = 6

        Public Shared HBTEXT_DIM_SIZE As Integer = 7

        Public Shared HYDROPHNAME_SIZE As Integer = 8

        Public Shared HYDROPHOBIC_WIDTH As Integer = 9

        Public Shared HYDROPHOBIC_DIM_WIDTH As Integer = 10

        Public Shared IFACE_ATMNAME1_SIZE As Integer = 11

        Public Shared IFACE_ATMNAME2_SIZE As Integer = 12

        Public Shared IFACE_ATOM1_RADIUS As Integer = 13

        Public Shared IFACE_ATOM2_RADIUS As Integer = 14

        Public Shared IFACE_BOND1_WIDTH As Integer = 15

        Public Shared IFACE_BOND2_WIDTH As Integer = 16

        Public Shared IFACE_HPHOBIC1_RADIUS As Integer = 17

        Public Shared IFACE_HPHOBIC2_RADIUS As Integer = 18

        Public Shared IFACE_HYDROPHNAME1_SIZE As Integer = 19

        Public Shared IFACE_HYDROPHNAME2_SIZE As Integer = 20

        Public Shared IFACE_RESNAME1_SIZE As Integer = 21

        Public Shared IFACE_RESNAME2_SIZE As Integer = 22

        Public Shared LIGATMNAME_SIZE As Integer = 23

        Public Shared LIGATOM_RADIUS As Integer = 24

        Public Shared LIGBOND_WIDTH As Integer = 25

        Public Shared LIGRESNAME_SIZE As Integer = 26

        Public Shared NLIGATMNAME_SIZE As Integer = 27

        Public Shared NLIGATOM_RADIUS As Integer = 28

        Public Shared NLIGBOND_WIDTH As Integer = 29

        Public Shared NLIGRESNAME_SIZE As Integer = 30

        Public Shared TITLE_SIZE As Integer = 31

        Public Shared TITLE_DIM_SIZE As Integer = 32

        Public Shared WATERNAME_SIZE As Integer = 33

        Public Shared WATERNAME_DIM_SIZE As Integer = 34

        Public Shared WATER_RADIUS As Integer = 35

        Public Shared WATER_DIM_RADIUS As Integer = 36

        Public Shared NSIZES As Integer = 37

        Public Shared ReadOnly PARAM_NAME As String() = New String() {"ABODY_LOOP_LABEL_SIZE", "EXTBOND_WIDTH", "EXTBOND_DIM_WIDTH", "HBOND_WIDTH", "HBOND_DIM_WIDTH", "HPHOBIC_RADIUS", "HBTEXT_SIZE", "HBTEXT_DIM_SIZE", "HYDROPHNAME_SIZE", "HYDROPHOBIC_WIDTH", "HYDROPHOBIC_DIM_WIDTH", "IFACE_ATMNAME1_SIZE", "IFACE_ATMNAME2_SIZE", "IFACE_ATOM1_RADIUS", "IFACE_ATOM2_RADIUS", "IFACE_BOND1_WIDTH", "IFACE_BOND2_WIDTH", "IFACE_HPHOBIC1_RADIUS", "IFACE_HPHOBIC2_RADIUS", "IFACE_HYDROPHNAME1_SIZE", "IFACE_HYDROPHNAME2_SIZE", "IFACE_RESNAME1_SIZE", "IFACE_RESNAME2_SIZE", "LIGATMNAME_SIZE", "LIGATOM_RADIUS", "LIGBOND_WIDTH", "LIGRESNAME_SIZE", "NLIGATMNAME_SIZE", "NLIGATOM_RADIUS", "NLIGBOND_WIDTH", "NLIGRESNAME_SIZE", "TITLE_SIZE", "TITLE_DIM_SIZE", "WATERNAME_SIZE", "WATERNAME_DIM_SIZE", "WATER_RADIUS", "WATER_DIM_RADIUS"}

        Private fontNamesList As List(Of Object) = New List(Of Object)()

        Private fontFamilyList As List(Of Object) = New List(Of Object)()

        Public textFieldList As List(Of Object)

        Public applyToAll As Boolean = False

        Private ensemble As Ensemble = Nothing

        Private program As Integer

        Private plotArea As PlotArea



        Private parameters As Params = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private params As Dictionary(Of String, String) = Nothing

        Public valueString As String() = New String(NSIZES - 1) {}





        Public Sub New(ensemble As Ensemble, parameters As Params, ligplusParams As Dictionary(Of String, String), plotArea As PlotArea, program As Integer)

            Me.ensemble = ensemble
            Me.ligplusParams = ligplusParams
            Me.parameters = parameters
            Me.plotArea = plotArea
            Me.program = program


        End Sub

    End Class

End Namespace
