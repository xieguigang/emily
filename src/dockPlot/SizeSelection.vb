Imports ligplus.pdb

Namespace ligplus

    Public Class SizeSelection
        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared CANCEL As Integer = 0

        Public Shared OK As Integer = 1

        Public Shared APPLY_TO_ALL As Integer = 2

        Public Shared TITLE_SIZE As Integer = 0

        Public Shared LIGRESNAME_SIZE As Integer = 1

        Public Shared NLIGRESNAME_SIZE As Integer = 2

        Public Shared HYDROPHNAME_SIZE As Integer = 3

        Public Shared WATERNAME_SIZE As Integer = 4

        Public Shared LIGATMNAME_SIZE As Integer = 5

        Public Shared NLIGATMNAME_SIZE As Integer = 6

        Public Shared HBTEXT_SIZE As Integer = 7

        Public Shared LIGATOM_RADIUS As Integer = 8

        Public Shared NLIGATOM_RADIUS As Integer = 9

        Public Shared WATER_RADIUS As Integer = 10

        Public Shared HPHOBIC_RADIUS As Integer = 11

        Public Shared SIMPLE_RADIUS As Integer = 12

        Public Shared LIGBOND_WIDTH As Integer = 13

        Public Shared NLIGBOND_WIDTH As Integer = 14

        Public Shared HBOND_WIDTH As Integer = 15

        Public Shared EXTBOND_WIDTH As Integer = 16

        Public Shared HYDROPHOBIC_WIDTH As Integer = 17

        Public Shared ANTIBODY_LOOP_LABEL_SIZE As Integer = 18

        Public Shared NSIZES As Integer = 19

        Public Shared ReadOnly paramName As String()() = New String()() {New String() {"TITLE_SIZE", "TITLE_SIZE"}, New String() {"LIGRESNAME_SIZE", "NLIGRESNAME_SIZE"}, New String() {"NLIGRESNAME_SIZE", "NLIGRESNAME2_SIZE"}, New String() {"HYDROPHNAME_SIZE", "HYDROPHNAME_SIZE"}, New String() {"WATERNAME_SIZE", "WATERNAME_SIZE"}, New String() {"LIGATMNAME_SIZE", "NLIGATMNAME_SIZE"}, New String() {"NLIGATMNAME_SIZE", "NLIGATMNAME2_SIZE"}, New String() {"HBTEXT_SIZE", "HBTEXT_SIZE"}, New String() {"LIGATOM_RADIUS", "NLIGATOM_RADIUS"}, New String() {"NLIGATOM_RADIUS", "NLIGATOM2_RADIUS"}, New String() {"WATER_RADIUS", "WATER_RADIUS"}, New String() {"HPHOBIC_RADIUS", "HPHOBIC_RADIUS"}, New String() {"SIMPLE_RADIUS", "SIMPLE_RADIUS"}, New String() {"LIGBOND_WIDTH", "NLIGBOND_WIDTH"}, New String() {"NLIGBOND_WIDTH", "NLIGBOND2_WIDTH"}, New String() {"HBOND_WIDTH", "HBOND_WIDTH"}, New String() {"EXTBOND_WIDTH", "EXTBOND_WIDTH"}, New String() {"HYDROPHOBIC_WIDTH", "HYDROPHOBIC_WIDTH"}, New String() {"ABODY_LOOP_LABEL_SIZE", "ABODY_LOOP_LABEL_SIZE"}}

        Public Shared ReadOnly duplicatedParamName As String()() = New String()() {New String() {"HYDROPHNAME_SIZE", "HYDROPHNAME2_SIZE"}, New String() {"HPHOBIC2_RADIUS", "HPHOBIC2_RADIUS"}, New String() {"HYDROPHOBIC_WIDTH", "HYDROPHOBIC2_WIDTH"}}

        Private fontNamesList As List(Of Object) = New List(Of Object)()

        Private fontFamilyList As List(Of Object) = New List(Of Object)()

        Public applyToAll As Boolean = False

        Private ensemble As Ensemble = Nothing

        Private program As Integer

        Private plotArea As PlotArea



        Private parameters As Params = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private params As Dictionary(Of String, String) = Nothing

        Public valueString As String() = New String(NSIZES - 1) {}

        Public textFieldList As List(Of Object)



        Public Sub New(ensemble As Ensemble, parameters As Params, ligplusParams As Dictionary(Of String, String), plotArea As PlotArea)


            Me.ensemble = ensemble
            Me.ligplusParams = ligplusParams
            Me.parameters = parameters
            Me.plotArea = plotArea

        End Sub



    End Class

End Namespace
