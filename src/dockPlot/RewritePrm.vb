Imports System.IO
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class RewritePrm
        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared ReadOnly PARAM_MAP As String()() = New String()() {New String() {"<- Radius: Ligand atoms", "LIGATOM_RADIUS", "IFACE_ATOM1_RADIUS"}, New String() {"<- Radius: Non-ligand atoms", "NLIGATOM_RADIUS", "IFACE_ATOM2_RADIUS"}, New String() {"<- Radius: Water molecules", "WATER_RADIUS", "WATER_DIM_RADIUS"}, New String() {"<- Radius: Hydrophobic contact residues", "HPHOBIC_RADIUS", "IFACE_HPHOBIC1_RADIUS"}, New String() {"<- Line-thickness: Ligand bonds", "LIGBOND_WIDTH", "IFACE_BOND1_WIDTH"}, New String() {"<- Line-thickness: Non-ligand bonds", "NLIGBOND_WIDTH", "IFACE_BOND2_WIDTH"}, New String() {"<- Line-thickness: Hydrogen bonds", "HBOND_WIDTH", "HBOND_DIM_WIDTH"}, New String() {"<- Line-thickness: External covalent bonds", "EXTBOND_WIDTH", "EXTBOND_DIM_WIDTH"}, New String() {"<- Residue names: Ligand residues", "LIGRESNAME_SIZE", "IFACE_RESNAME1_SIZE"}, New String() {"<- Residue names: Non-ligand residues", "NLIGRESNAME_SIZE", "IFACE_RESNAME2_SIZE"}, New String() {"<- Residue names: Water molecule IDs", "WATERNAME_SIZE", "WATERNAME_DIM_SIZE"}, New String() {"<- Residue names: Hydrophobic-interaction residues", "HYDROPHNAME_SIZE", "IFACE_HYDROPHNAME1_SIZE"}, New String() {"<- Atom names: Ligand atoms", "LIGATMNAME_SIZE", "IFACE_ATMNAME1_SIZE"}, New String() {"<- Atom names: Non-ligand atoms", "NLIGATMNAME_SIZE", "IFACE_ATMNAME2_SIZE"}, New String() {"<- Hydrogen-bond lengths", "HBTEXT_SIZE", "HBTEXT_DIM_SIZE"}}

        Private program As Integer = LIGPLOT

        Private params As Properties

        Public Sub New(ensemble As Ensemble, fileNameIn As String, fileNameOut As String, program As Integer, params As Properties)
            Dim wanted = False
            Dim inputStream As StreamReader = Nothing

            Me.params = params
            Me.program = program

            Using out = New PrintStream(fileNameOut)
                Try
                    inputStream = New StreamReader(fileNameIn)
                    Dim inputLine As Value(Of String) = ""
                    While Not (inputLine = inputStream.ReadLine()) Is Nothing
                        Dim line_str As String = inputLine

                        If Not wanted AndAlso line_str.Contains("SIZES") Then
                            wanted = True
                        ElseIf wanted AndAlso line_str.Contains("COLOURS") Then
                            wanted = False
                        End If
                        Dim [string] = modifyValue(line_str, wanted)
                        out.format("%s" & vbLf, New Object() {[string]})
                    End While

                Finally
                    If inputStream IsNot Nothing Then
                        inputStream.Close()
                    End If
                End Try

            End Using
        End Sub

        Private Function modifyValue(inputLine As String, wanted As Boolean) As String
            Dim newLine = ""
            If Not wanted Then
                Return inputLine
            End If
            Dim found = False
            newLine = inputLine
            Dim item = 0

            While item < PARAM_MAP.Length AndAlso Not found
                Dim search = PARAM_MAP(item)(0)
                Dim start = inputLine.IndexOf(search, StringComparison.Ordinal)
                If start > -1 Then
                    Dim paramString = PARAM_MAP(item)(program + 1)
                    Dim numberString = params(paramString)
                    If Not ReferenceEquals(numberString, Nothing) Then
                        newLine = numberString & " " & search
                        found = True
                    End If
                End If

                item += 1
            End While
            Return newLine
        End Function
    End Class

End Namespace
