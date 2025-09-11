Imports ligplus.pdb

Namespace ligplus

    Public Class OnOffSelection
        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared CANCEL As Integer = 0

        Public Shared OK As Integer = 1

        Public Shared APPLY_TO_ALL As Integer = 2

        Public Shared TITLE_STATUS As Integer = 0

        Public Shared LIGRESNAME_STATUS As Integer = 1

        Public Shared NLIGRESNAME_STATUS As Integer = 2

        Public Shared HYDROPHNAME_STATUS As Integer = 3

        Public Shared WATERNAME_STATUS As Integer = 4

        Public Shared LIGATMNAME_STATUS As Integer = 5

        Public Shared NLIGATMNAME_STATUS As Integer = 6

        Public Shared HBTEXT_STATUS As Integer = 7

        Public Shared LIGATOM_STATUS As Integer = 8

        Public Shared NLIGATOM_STATUS As Integer = 9

        Public Shared WATER_STATUS As Integer = 10

        Public Shared ATOM_EDGES_STATUS As Integer = 11

        Public Shared HBOND_STATUS As Integer = 12

        Public Shared HYDROPHOBIC_STATUS As Integer = 13

        Public Shared SHOW_BLANK_TEXT_STATUS As Integer = 14

        Public Shared INACTIVE_PLOTS_STATUS As Integer = 15

        Public Shared DOUBLE_BONDS_STATUS As Integer = 16

        Public Shared EQUIV_ELLIPSES_STATUS As Integer = 17

        Public Shared EQUIV_SIDECHAINS_UNDERLAY_STATUS As Integer = 18

        Public Shared EQUIV_HGROUPS_UNDERLAY_STATUS As Integer = 19

        Public Shared SHOW_CHAIN_IDS As Integer = 20

        Public Shared HIGHLIGHT_NONEQUIVS As Integer = 21

        Public Shared SHORTEST_ONLY_STATUS As Integer = 22

        Public Shared NSTATUSES As Integer = 23

        Public Shared ReadOnly paramName As String()() = New String()() {New String() {"TITLE_STATUS", "TITLE_STATUS"}, New String() {"LIGRESNAME_STATUS", "NLIGRESNAME_STATUS"}, New String() {"NLIGRESNAME_STATUS", "NLIGRESNAME2_STATUS"}, New String() {"HYDROPHNAME_STATUS", "HYDROPHNAME_STATUS"}, New String() {"WATERNAME_STATUS", "WATERNAME_STATUS"}, New String() {"LIGATMNAME_STATUS", "NLIGATMNAME_STATUS"}, New String() {"NLIGATMNAME_STATUS", "NLIGATMNAME2_STATUS"}, New String() {"HBTEXT_STATUS", "HBTEXT_STATUS"}, New String() {"LIGATOM_STATUS", "NLIGATOM_STATUS"}, New String() {"NLIGATOM_STATUS", "NLIGATOM2_STATUS"}, New String() {"WATER_STATUS", "WATER_STATUS"}, New String() {"ATOM_EDGES_STATUS", "ATOM_EDGES_STATUS"}, New String() {"HBOND_STATUS", "HBOND_STATUS"}, New String() {"HYDROPHOBIC_STATUS", "HYDROPHOBIC_STATUS"}, New String() {"SHOW_BLANK_TEXT_STATUS", "SHOW_BLANK_TEXT_STATUS"}, New String() {"INACTIVE_PLOTS_STATUS", "INACTIVE_PLOTS_STATUS"}, New String() {"DOUBLE_BONDS_STATUS", "DOUBLE_BONDS_STATUS"}, New String() {"EQUIV_ELLIPSES_STATUS", "EQUIV_ELLIPSES_STATUS"}, New String() {"EQUIV_SIDECHAINS_UNDERLAY_STATUS", "EQUIV_SIDECHAINS_UNDERLAY_STATUS"}, New String() {"EQUIV_HGROUPS_UNDERLAY_STATUS", "EQUIV_HGROUPS_UNDERLAY_STATUS"}, New String() {"SHOW_CHAIN_IDS", "SHOW_CHAIN_IDS"}, New String() {"HIGHLIGHT_NONEQUIVS", "HIGHLIGHT_NONEQUIVS"}, New String() {"SHORTEST_ONLY_STATUS", "SHORTEST_ONLY_STATUS"}}

        Public Shared ReadOnly duplicatedParamName As String() = New String() {"HYDROPHNAME", "HYDROPHOBIC"}

        Public applyToAll As Boolean = False

        Public status As Boolean() = New Boolean(NSTATUSES - 1) {}

        Private ensemble As Ensemble = Nothing



        Private program As Integer

        Private parameters As Params = Nothing

        Private ligplusParams As Dictionary(Of String, String) = Nothing

        Private params As Dictionary(Of String, String) = Nothing

        Public checkBoxList As List(Of Object)





        Public Sub New(ensemble As Ensemble, parameters As Params, ligplusParams As Dictionary(Of String, String))


            Me.ensemble = ensemble
            Me.ligplusParams = ligplusParams
            Me.parameters = parameters

        End Sub





    End Class

End Namespace
