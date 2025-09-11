Imports ligplus.file
Imports ligplus.pdb

Namespace ligplus

    Public NotInheritable Class LigPlusFrame
        Public Const VERSION As String = "v.2.3.1"

        Public Const VERSION_DATE As String = "22 Jul 2025"

        Public Const MAX_WIDTH As Integer = 800

        Public Const MAX_HEIGHT As Integer = 600

        Public Const SCREEN_BORDER As Integer = 50

        Public Const RASMOL_LOGO_ICON As Integer = 0

        Public Const PYMOL_LOGO_ICON As Integer = 1

        Public Const NICONS As Integer = 2

        Public Shared ReadOnly ICON As String() = New String() {"rasmol_logo.gif", "pymol_logo.gif"}


        Public Const LIB_DIR As String = "lib"

        Public Const DOCS_DIR As String = "lib/docs"

        Public Const EXE_DIR As String = "lib/exe"

        Public Const EXE_LINUX_DIR As String = "lib/exe_linux"

        Public Const EXE_LINUX64_DIR As String = "lib/exe_linux64"

        Public Const EXE_MAC_DIR As String = "lib/exe_mac"

        Public Const EXE_MAC64_DIR As String = "lib/exe_mac64"

        Public Const EXE_WIN_DIR As String = "lib/exe_win"

        Public Const EXE_WIN32_DIR As String = "lib/exe_win32"

        Public Const IMAGE_DIR As String = "lib/images"

        Public Const PARAM_DIR As String = "lib/params"

        Public Const licenceFile As String = "licence.txt"

        Public Const MS_PER_DAY As Long = 86400000L

        Private filterWaters As Boolean = False

        Private orderFirstSeq As Boolean = False

        Private fitLigands As Boolean = False

        Private includeWaters As Boolean = False

        Private dimplotIncludeWaters As Boolean = False

        Private plotMetal As Boolean = False

        Private splitScreen As Boolean = False

        Private landscape As Boolean = True

        Private separatePages As Boolean = True

        Private allPlots As Boolean = True

        Private antibody As Boolean = False

        Private antibodyNumberingScheme As Integer = 3

        Private heavyChain As Char = Char.MinValue

        Private lightChain As Char = Char.MinValue

        Private antigenChain As Char = Char.MinValue

        Private fullHeavyChain As String = ""

        Private fullLightChain As String = ""

        Private fullAntigenChain As String = ""

        Private dimplotOrientation As Integer = RunExe.LANDSCAPE

        Private moveBy As Integer = 0

        Private prevInOut As Integer = -1

        Private Shared ensemble As Ensemble = Nothing


        Private Shared frame As LigPlusFrame

        Private Shared parameters As Params = Nothing

        Private Shared plotArea As PlotArea = Nothing

        Private Shared pdb As PDBEntry = Nothing

        Private Shared ligplusParams As Properties

        Private Shared run As RunExe = Nothing

        Private Shared currentDirectory As String = "."

        Private Shared docsPath As String = Nothing

        Private Shared exePath As String = Nothing

        Private Shared imgPath As String = Nothing

        Private Shared libPath As String = Nothing

        Private Shared paramPath As String = Nothing

        Private Shared pdbCodeField As String = Nothing

        Private Shared coraFileName As String = Nothing

        Private Shared pdbCodeList As List(Of Object) = Nothing


        Private Sub OpenPDBFileMenuItemActionPerformed()
            Dim cancelled = False
            Dim haveLigplot = False
            Dim haveDimplot = False
            Dim haveAntibodyPlot = False
            Console.WriteLine("Open PDB file item selected")
            pdbCodeField = Nothing
            If ensemble IsNot Nothing AndAlso ensemble.getnPDB() > 0 Then

                If ensemble IsNot Nothing Then
                    If ensemble.Program = Ensemble.LIGPLOT Then
                        haveLigplot = True
                    Else
                        haveDimplot = True
                    End If
                    If ensemble.Antibody Then
                        haveLigplot = False
                        haveAntibodyPlot = True

                        haveDimplot = False
                    End If
                End If
            End If
            If Not cancelled Then
                ' AlignmentPDB;
                If Not ReferenceEquals(coraFileName, Nothing) Then
                Else
                    addPDBEntry(haveLigplot, haveDimplot, haveAntibodyPlot)
                End If
            End If
        End Sub

        Private Sub addPDBEntry(haveLigplot As Boolean, haveDimplot As Boolean, haveAntibodyPlot As Boolean)
            Dim fileName = "Browse"
            Dim pdbDir = Params.getGlobalProperty(3, 0)
            If Not ReferenceEquals(pdbDir, Nothing) Then
                fileName = PDBCode
            End If
            If Not ReferenceEquals(fileName, Nothing) AndAlso fileName.Equals("Browse") Then
                fileName = browsePDB()
            End If
            If Not ReferenceEquals(fileName, Nothing) Then
                processPDB(fileName, haveLigplot, haveDimplot, haveAntibodyPlot)
            End If
        End Sub

        Private Function browsePDB() As String
            Dim fileName As String = Nothing

            Return fileName
        End Function

        Private Sub processPDB(fileName As String, haveLigplot As Boolean, haveDimplot As Boolean, haveAntibodyPlot As Boolean)
            Dim readFrom = 0

            Console.WriteLine("Reading .pdb file ...")
            Dim readFile As ReadPDBFile = New ReadPDBFile(fileName)

            If readFile IsNot Nothing Then
                pdb = readFile.PDBEntry
            End If
            If pdb IsNot Nothing AndAlso pdb.Natoms > 0 Then
                If ReferenceEquals(pdb.PDBCode, Nothing) AndAlso Not ReferenceEquals(pdbCodeField, Nothing) Then
                    pdb.PDBCode = pdbCodeField
                End If
                pdb.ReadFrom = readFrom
                Call pdb.interpretData()
            Else
                ' JOptionPane.showMessageDialog(this, "<html>No atom coordinates found in file:<P>" + fileName + "<P>Program cannot be run", "No atoms", 0);
            End If

            If readFile Is Nothing OrElse pdb.Natoms = 0 Then
                Return
            End If

        End Sub



        Public Function openDrwFile(file As String) As Boolean
            Dim fileOk = True
            If Not Equals(file, Nothing) AndAlso file.FileExists() Then
                If ensemble Is Nothing Then
                    ensemble = New Ensemble(ligplusParams, 1)
                End If
                Dim pdbCode As String = RunExe.stripExtension(file.FileName())
                Dim fileName = file
                Console.WriteLine("Reading .drw file: " & fileName)
                Dim drwFile As ReadDrwFile = New ReadDrwFile(ensemble, fileName, pdbCode, False, antibodyNumberingScheme, fullHeavyChain, fullLightChain, fullAntigenChain, RunExe.PORTRAIT, False, frame, plotArea)
                fileOk = True
                Dim program = drwFile.Program
                If program = RunExe.DIMPLOT Then
                    dimplotOrientation = drwFile.OrientationOption
                Else
                    dimplotOrientation = RunExe.LANDSCAPE
                End If
                antibody = drwFile.Antibody
                ensemble.Antibody = antibody
                ensemble.Program = program
                ensemble.OrientationOption = dimplotOrientation
                Call ensemble.finalProcessing()

                If fileOk Then
                    plotArea.newImage(ensemble)
                End If
            End If
            Return fileOk
        End Function

        Public Sub getAlignmentPDB()
            Dim ok = False
            Dim codeList = New String(pdbCodeList.Count - 1) {}
            Dim nItems = 0
            Dim [select] = -1
            Dim haveOnScreen = False
            Dim pdbEntryList As List(Of PDBEntry) = Nothing
            If ensemble IsNot Nothing Then
                pdbEntryList = ensemble.PDBEntryList
            End If
            For iPDB = 0 To pdbCodeList.Count - 1
                Dim code As String = pdbCodeList(iPDB)
                Dim [string] = code
                Dim onScreen = False
                If pdbEntryList IsNot Nothing Then
                    Dim iEntry = 0

                    While iEntry < pdbEntryList.Count AndAlso Not onScreen
                        Dim pdbOnScreen = pdbEntryList(iEntry)
                        If code.Equals(pdbOnScreen.PDBCode) Then
                            haveOnScreen = True
                            onScreen = True
                            [string] = [string] & " *"
                        End If

                        iEntry += 1
                    End While
                End If
                If Not onScreen AndAlso [select] = -1 Then
                    [select] = iPDB
                End If
                codeList(nItems) = [string]
                nItems += 1
            Next

            If [select] < 0 Then
                [select] = 0
            End If


            Dim haveLigplot = False
            Dim haveDimplot = False
            Dim haveAntibodyPlot = False
            If ensemble IsNot Nothing Then
                If ensemble.Antibody Then
                    haveAntibodyPlot = True
                ElseIf ensemble.Program = Ensemble.LIGPLOT Then
                    haveLigplot = True
                Else
                    haveDimplot = True
                End If
            End If


        End Sub


        Private ReadOnly Property PDBCode As String
            Get
                Return Nothing
            End Get
        End Property
    End Class

End Namespace
