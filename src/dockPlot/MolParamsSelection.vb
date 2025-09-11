Namespace ligplus

    Public Class MolParamsSelection
        Public Shared DELETE As Integer = 0

        Public Shared SAVE As Integer = 1

        Public Shared NCOLOURS As Integer = 3

        Public Shared ReadOnly paramName As String()() = New String()() {New String() {"RESIDUE_COLOUR", "NLIGBOND_COLOUR"}, New String() {"RESNAME_COLOUR", "NLIGRESNAME_COLOUR"}, New String() {"ATOMNAME_COLOUR", "NLIGATMNAME_COLOUR"}}

        Private nColoursField As Integer = 0

        Private selectedMoleculesList As List(Of Object) = Nothing

        Private ensemble As Ensemble

        Private colourIndex As Dictionary(Of String, String) = Nothing

        Private params As Properties

        Private comboBoxList As List(Of Object)

        Private colourList As List(Of Object) = Nothing

        Private colourNameList As List(Of Object) = Nothing



        Public Sub New(modal As Boolean, selectedMoleculeList As List(Of Object), ensemble As Ensemble)
            selectedMoleculesList = selectedMoleculeList
            Me.ensemble = ensemble
            params = ensemble.DefaultParams
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
