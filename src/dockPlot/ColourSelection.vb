Namespace ligplus

    Public Class ColourSelection
        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared CANCEL As Integer = 0

        Public Shared OK As Integer = 1

        Public Shared APPLY_TO_ALL As Integer = 2

        Public Shared BACKGROUND_COLOUR As Integer = 0

        Public Shared INACTIVE_COLOUR As Integer = 1

        Public Shared NCOLOURS As Integer = 37

        Public Shared ReadOnly paramName As String()() = New String()() {New String() {"BACKGROUND_COLOUR", "BACKGROUND_COLOUR"}, New String() {"INACTIVE_COLOUR", "INACTIVE_COLOUR"}, New String() {"TITLE_COLOUR", "TITLE_COLOUR"}, New String() {"LIGRESNAME_COLOUR", "NLIGRESNAME_COLOUR"}, New String() {"NLIGRESNAME_COLOUR", "NLIGRESNAME2_COLOUR"}, New String() {"HYDROPHNAME_COLOUR", "HYDROPHNAME_COLOUR"}, New String() {"HYDROPHNAME2_COLOUR", "HYDROPHNAME2_COLOUR"}, New String() {"WATERNAME_COLOUR", "WATERNAME_COLOUR"}, New String() {"LIGATMNAME_COLOUR", "NLIGATMNAME_COLOUR"}, New String() {"NLIGATMNAME_COLOUR", "NLIGATMNAME2_COLOUR"}, New String() {"HBTEXT_COLOUR", "HBTEXT_COLOUR"}, New String() {"NITROGEN_COLOUR", "NITROGEN_COLOUR"}, New String() {"OXYGEN_COLOUR", "OXYGEN_COLOUR"}, New String() {"CARBON_COLOUR", "CARBON_COLOUR"}, New String() {"SULPHUR_COLOUR", "SULPHUR_COLOUR"}, New String() {"WATER_COLOUR", "WATER_COLOUR"}, New String() {"PHOSPHORUS_COLOUR", "PHOSPHORUS_COLOUR"}, New String() {"METAL_COLOUR", "METAL_COLOUR"}, New String() {"OTHER_COLOUR", "OTHER_COLOUR"}, New String() {"HYDROPHOBIC_COLOUR", "HYDROPHOBIC_COLOUR"}, New String() {"HYDROPHOBIC2_COLOUR", "HYDROPHOBIC2_COLOUR"}, New String() {"LIGBOND_COLOUR", "NLIGBOND_COLOUR"}, New String() {"NLIGBOND_COLOUR", "NLIGBOND2_COLOUR"}, New String() {"HBOND_COLOUR", "HBOND_COLOUR"}, New String() {"SALT_BRIDGE_COLOUR", "SALT_BRIDGE_COLOUR"}, New String() {"DISULPHIDE_COLOUR", "DISULPHIDE_COLOUR"}, New String() {"EXTBOND_COLOUR", "EXTBOND_COLOUR"}, New String() {"HYDROPHOBIC_BOND_COLOUR", "HYDROPHOBIC_BOND_COLOUR"}, New String() {"HYDROPHOBIC2_BOND_COLOUR", "HYDROPHOBIC2_BOND_COLOUR"}, New String() {"ABODY_H1_RESNAME_COLOUR", "ABODY_H1_RESNAME_COLOUR"}, New String() {"ABODY_H2_RESNAME_COLOUR", "ABODY_H2_RESNAME_COLOUR"}, New String() {"ABODY_H3_RESNAME_COLOUR", "ABODY_H3_RESNAME_COLOUR"}, New String() {"ABODY_L1_RESNAME_COLOUR", "ABODY_L1_RESNAME_COLOUR"}, New String() {"ABODY_L2_RESNAME_COLOUR", "ABODY_L2_RESNAME_COLOUR"}, New String() {"ABODY_L3_RESNAME_COLOUR", "ABODY_L3_RESNAME_COLOUR"}, New String() {"ABODY_OTHER_RESNAME_COLOUR", "ABODY_OTHER_RESNAME_COLOUR"}, New String() {"ABODY_ANTIGEN_RESNAME_COLOUR", "ABODY_ANTIGEN_RESNAME_COLOUR"}}

        Public Shared ReadOnly abParamName As String()() = New String()() {New String() {"ABODY_H1_COLOUR", "ABODY_H1_ATNAME_COLOUR"}, New String() {"ABODY_H2_COLOUR", "ABODY_H2_ATNAME_COLOUR"}, New String() {"ABODY_H3_COLOUR", "ABODY_H3_ATNAME_COLOUR"}, New String() {"ABODY_L1_COLOUR", "ABODY_L1_ATNAME_COLOUR"}, New String() {"ABODY_L2_COLOUR", "ABODY_L2_ATNAME_COLOUR"}, New String() {"ABODY_L3_COLOUR", "ABODY_L3_ATNAME_COLOUR"}, New String() {"ABODY_OTHER_COLOUR", "ABODY_OTHER_ATNAME_COLOUR"}, New String() {"ABODY_ANTIGEN_COLOUR", "ABODY_ANTIGEN_ATNAME_COLOUR"}}

        Public applyToAll As Boolean = False

        Private ensemble As Ensemble = Nothing



        Private nColoursField As Integer = 0

        Private nGlobals As Integer = 0

        Private program As Integer

        Private parameters As Params = Nothing

        Private colourIndex As Dictionary(Of String, String) = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private params As Dictionary(Of String, String) = Nothing

        Public valueString As String() = New String(NCOLOURS - 1) {}

        Private colourList As List(Of Object) = Nothing

        Private colourNameList As List(Of Object) = Nothing

        Public comboBoxList As List(Of Object)

        Private globalList As List(Of Object) = Nothing



        Public Sub New(ensemble As Ensemble, parameters As Params, ligplusParams As Dictionary(Of String, String))

            Me.ensemble = ensemble
            Me.ligplusParams = ligplusParams
            Me.parameters = parameters

        End Sub



        Private Sub getColours()
            nColoursField = 0
            colourIndex = ligplus.Params.ColourIndex
            colourList = ligplus.Params.ColourList
            colourNameList = ligplus.Params.ColourNameList
            nColoursField = colourList.Count
        End Sub


    End Class

End Namespace
