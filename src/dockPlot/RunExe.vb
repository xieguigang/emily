Imports System.IO
Imports ligplus.file
Imports ligplus.models
Imports ligplus.pdb
Imports Microsoft.VisualBasic.Text
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class RunExe

        Public Shared LIGPLOT As Integer = 0

        Public Shared DIMPLOT As Integer = 1

        Public Shared RASMOL As Integer = 2

        Public Shared PYMOL As Integer = 3

        Public Const INPUT_PDB As Integer = 0

        Public Const INPUT_DRW As Integer = 1

        Public Const OUTPUT_DRW As Integer = 2

        Public Const OUTPUT_PS As Integer = 3

        Public Const INPUT_CORA As Integer = 4

        Public Const OUTPUT_TXT As Integer = 5

        Public Shared ReadOnly progName As String() = New String() {"LIGPLOT", "DIMPLOT"}

        Public Shared [CONTINUE] As Integer = 0

        Public Shared CANCEL As Integer = 1

        Public Shared START As Integer = 0

        Public Shared RUNNING_HBADD As Integer = 1

        Public Shared RUNNING_HBPLUS1 As Integer = 2

        Public Shared RUNNING_HBPLUS2 As Integer = 3

        Public Shared RUNNING_DIMER As Integer = 4

        Public Shared RUNNING_LIGFIT As Integer = 5

        Public Shared RUNNING_DIMHTML As Integer = 6

        Public Shared RUNNING_LIGPLOT As Integer = 7

        Public Shared NSTAGES As Integer = 8

        Public Shared progressPercent As Integer()() = New Integer()() {New Integer() {0, 5, 20, 50, 50, 55, 60, 60}, New Integer() {0, 5, 5, 10, 10, 10, 10, 10}}

        Public Shared NO_MONITOR As Integer = 0

        Public Shared LIGPLOT_LOOPS As Integer = 1000

        Public Shared DIMHTML_LOOPS As Integer = 50

        Public Shared DIMPLOT_LOOPS As Integer = 3000

        Public Shared PORTRAIT As Integer = 0

        Public Shared LANDSCAPE As Integer = 1

        Public Const MIN_RMSD As Single = 2.0F

        Public Shared DEFAULT_MINOFF As Integer = 2

        Public Shared ReadOnly runMessage As String() = New String() {"Initialising ...", "Running HBADD ...", "Running HBPLUS: H-bonds ...", "Running HBPLUS: contacts ...", "Running DIMER ...", "Running LIGFIT ...", "Running DIMHTML ...", "Running LIGPLOT ..."}



        Private Shared tmpDirNameField As String = Nothing



        Private filterWaters As Boolean

        Private orderFirstSeq As Boolean

        Private fitLigands As Boolean

        Private includeWaters As Boolean

        Private plotMetal As Boolean

        Private ensemble As Ensemble = Nothing

        Private program As Integer

        Private progress As Integer = 0

        Private readFrom As Integer

        Private status As Integer = [CONTINUE]

        Private currentBar As Integer = 1

        Private antibody As Boolean = False

        Private antibodyNumberingScheme As Integer = 3

        Private heavyChain As Char

        Private lightChain As Char

        Private antigenChain As Char

        Private fullAntigenChain As String

        Private fullHeavyChain As String

        Private fullLightChain As String

        Private dimplotOrientation As Integer



        Private frame As LigPlusFrame

        Private pdb As PDBEntry

        Private plotArea As PlotArea = Nothing



        Private params As Dictionary(Of String, String)

        Private errorMessage As String = ""

        Private coraFileName As String = Nothing

        Public Sub New(frame As LigPlusFrame, prefix As String)

            createTmpDir(frame, prefix)
        End Sub





        Public Overridable Sub controlLigPlot()
            SyncLock Me
                Dim fitting = False
                Dim ok = True
                Dim haveDomains = False
                Dim ranLigfit = False
                Dim status = 0
                Dim prog = WritePDBFile.LIGPLOT
                Dim fitType = -1
                ' this.errorMessage = "";
                '  this.progress = RunExe.START;
                If ensemble IsNot Nothing AndAlso ensemble.PDBEntryList.Count > 0 Then
                    fitting = True
                End If
                Dim fileName = "ligplus.pdb"
                Dim domFileName = "ligplus.dom"
                If program = DIMPLOT Then
                    prog = WritePDBFile.DIMPLOT
                End If
                Dim progLabel = "Running " & progName(program)
                Dim tmpPDBFile As String = tmpDirNameField & "/" & fileName
                Dim allResid = False
                If fitting Then
                    allResid = True
                End If
                If pdb.FromMmcif Then
                End If
                Dim lFileOK = writeTmpPDB(pdb, tmpPDBFile, prog, allResid)
                If Not lFileOK Then
                    Return
                End If
                If fitting Then
                    Dim refPDB As PDBEntry = ensemble.PDBEntryList(0)
                    Dim originalPDB = refPDB.OriginalPDB
                    Dim usePDB = originalPDB
                    If usePDB Is Nothing Then
                        Dim message = "<html>Original coordinates of the reference structure no longer<BR>available for fitting. Will use coordinates from just the residues in the reference plot.<P>&nbsp;<p>For most cases, this should be OK. If not, then<BR>generate the reference plot from scratch."
                        '  JOptionPane.showMessageDialog(this.frame, message, "Missing coordinates", 1);
                        usePDB = refPDB
                    End If
                    Dim allPDBFile As String = tmpDirNameField & "/ligall.pdb"
                    allResid = True
                    lFileOK = writeTmpPDB(usePDB, allPDBFile, prog, allResid)
                End If
                If Not lFileOK Then
                    Return
                End If
                If program = DIMPLOT Then
                    If Not pdb.getDomainDefinition(0).Equals("") AndAlso Not pdb.getDomainDefinition(1).Equals("") Then
                        Dim domFile As String = tmpDirNameField & "/" & domFileName
                        lFileOK = writeDomFile(pdb, domFile)
                        haveDomains = True
                    End If
                End If
                Console.WriteLine("Calling HBADD ...")
                progress = RUNNING_HBADD

                Console.WriteLine("Running HBADD")
                ok = callHBADD(tmpPDBFile)

                If ok Then
                    progress = RUNNING_HBPLUS1

                    ok = callHBPLUS(tmpPDBFile, True)

                End If
                If ok Then
                    progress = RUNNING_HBPLUS2

                    ok = callHBPLUS(tmpPDBFile, False)

                End If
                If ok AndAlso program = DIMPLOT Then
                    progress = RUNNING_DIMER

                    ok = callDimer(pdb, tmpPDBFile, haveDomains)

                    tmpPDBFile = tmpDirNameField & "/dimplot.pdb"
                    progress = RUNNING_DIMHTML

                    ok = callDimhtml()

                End If
                If fitting Then
                    Dim drwName As String = tmpDirNameField & "/ensemble.drw"
                    ensemble.saveCoords()
                    Try
                        Call New WriteDrwFile(ensemble, True).write(drwName)
                        progress = RUNNING_LIGFIT

                        ok = callLIGFIT(pdb, haveDomains)
                        ranLigfit = True

                    Catch __unusedIOException1__ As IOException
                        Console.WriteLine("*** ERROR. I/O error writing: " & drwName)
                        ok = False
                    End Try
                End If
                If ok Then
                    progress = RUNNING_LIGPLOT

                    ok = callLIGPLOT(pdb, tmpPDBFile, program, ranLigfit)

                End If
                If ok Then
                    fileName = tmpDirNameField & "/" & "ligplot.drw"
                    Dim pdbCode = pdb.PDBCode
                    If ReferenceEquals(pdbCode, Nothing) Then
                        pdbCode = "none"
                    End If
                    Dim originalPDB = pdb
                    ok = readFile(fileName, pdbCode, 1, readFrom, ranLigfit)
                    If Not ok Then
                        '  JOptionPane.showMessageDialog(this.frame, this.errorMessage, "LIGPLOT failed", 0);
                    End If

                    If ok Then
                        pdb.OriginalPDB = originalPDB
                    End If
                    If ok AndAlso ranLigfit Then
                        ok = getShifts(ensemble)
                        fileName = tmpDirNameField & "/" & "ligplus.lgf"
                        Dim fileOk = True
                        Dim lgfFile As ReadLgfFile = Nothing
                        Try
                            lgfFile = New ReadLgfFile(ensemble, pdb, fileName)
                            fileOk = True
                        Catch __unusedFileNotFoundException1__ As FileNotFoundException
                            fileOk = False
                        Catch __unusedIOException2__ As IOException
                            fileOk = False
                        End Try
                        If lgfFile IsNot Nothing Then
                            fitType = lgfFile.FitType
                        End If
                        If pdb IsNot Nothing Then
                            getAnchors(pdb)
                        End If
                    End If
                End If
                If Not ok AndAlso fitting Then
                    ensemble.restoreCoords()
                End If
                If ok Then
                    ensemble.finalProcessing()
                    plotArea.newImage(ensemble)

                    If fitType = 0 Then
                        ' JOptionPane.showMessageDialog(this.frame, message, "Unable to fit", 1);
                        Dim message = "Unable to fit the plot to previous. Current plot just overlaid, with no residue equivalences identified."
                    ElseIf fitType = 2 AndAlso Not fitLigands Then
                        Dim message = "Unable to fit on binding site residues. So current plot fitted by superposing the ligands."
                        '  JOptionPane.showMessageDialog(this.frame, message, "Plot fitted on ligands", 1);
                    End If
                Else

                End If
            End SyncLock
        End Sub

        Public Overridable Sub controlRasMol()
            SyncLock Me
                Dim ok = True
                Dim fileName = "rasmol.script"
                Dim scriptFile As String = tmpDirNameField & "/" & fileName
                Dim fileOK = writeRasMolScript(ensemble, scriptFile)
                If Not fileOK Then
                    Return
                End If
                Dim rasmolExe = ligplus.Params.getGlobalProperty(5, 0)
                Dim command = New String(2) {}
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = rasmolExe
                command(1) = "-script"
                command(2) = scriptFile
                'frame.disableViewerButtons()
                ok = runExecutable(command, NO_MONITOR)
                ' frame.enableViewerButtons()
            End SyncLock
        End Sub

        Public Overridable Sub controlPyMOL()
            SyncLock Me
                Dim ok = True
                Dim fileName = "pymol.pml"
                Dim scriptFile As String = tmpDirNameField & "/" & fileName
                fileName = "pymol.pdb"
                Dim coordsFile As String = tmpDirNameField & "/" & fileName
                Dim fileOK = writePyMOLScript(ensemble, scriptFile, coordsFile)
                If Not fileOK Then
                    Return
                End If
                Dim pyMOLExe = ligplus.Params.getGlobalProperty(9, 0)
                Dim command = New String(2) {}
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = pyMOLExe
                command(1) = scriptFile
                ' frame.disableViewerButtons()
                ok = runExecutable(command, NO_MONITOR)
                ' frame.enableViewerButtons()
            End SyncLock
        End Sub

        Friend Overridable Function callDimer(pdb As PDBEntry, tmpPDBFile As String, haveDomains As Boolean) As Boolean
            SyncLock Me
                Dim ok = True
                Dim exeFile As String = ligplus.Params.exePath & "/dimer"
                Dim file = exeFile
                Dim dimerExe As String = file
                Dim command = New String(4) {}
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = dimerExe
                command(1) = tmpPDBFile
                If haveDomains Then
                    command(2) = "-d"
                    command(3) = "1"
                    command(4) = "2"
                Else
                    Dim molecule1 = pdb.getSelectedMolecule(0)
                    Dim molecule2 = pdb.getSelectedMolecule(1)
                    command(2) = "" & molecule1.Chain.ToString()
                    command(3) = "" & molecule2.Chain.ToString()
                    command(4) = ""
                End If
                ok = runExecutable(command, NO_MONITOR)
                Return ok
            End SyncLock
        End Function

        Friend Overridable Function callHBADD(tmpPDBFile As String) As Boolean
            SyncLock Me
                Dim ok = True
                Dim exeFile As String = ligplus.Params.exePath & "/hbadd"
                Dim file = exeFile
                Dim hbaddExe As String = file
                Dim hetDic = ligplus.Params.getGlobalProperty(1, 0)
                file = hetDic
                Dim hetDicName As String = file
                Dim command = New String(4) {}
                command(0) = hbaddExe
                command(1) = tmpPDBFile
                command(2) = hetDicName
                command(3) = "-wkdir"
                command(4) = tmpDirNameField & "/"
                Console.WriteLine("Het Group Dictionary: " & hetDicName)
                Console.WriteLine("Temporary PDB file: " & tmpPDBFile)
                ok = runExecutable(command, NO_MONITOR)
                Return ok
            End SyncLock
        End Function

        Friend Overridable Function callHBPLUS(tmpPDBFile As String, nonBond As Boolean) As Boolean
            SyncLock Me
                Dim haveRC = False
                Dim ok = True
                Console.WriteLine("In callHBPLUS with tmpPDBFile = " & tmpPDBFile)
                Dim exeFile As String = ligplus.Params.exePath & "/hbplus"
                Dim file = exeFile
                Dim hbplusExe As String = file
                Dim hbplusRC As String = tmpDirNameField & "/hbplus.rc"
                Dim rcFile = hbplusRC
                If rcFile.FileExists() Then
                    haveRC = True
                End If
                Dim command = New String(11) {}
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = hbplusExe
                command(1) = "-L"
                If haveRC Then
                    command(2) = "-f"
                    command(3) = hbplusRC
                End If
                If nonBond Then
                    Dim hParam = getHBPLUSParam("HBPLUS_NB_HPARAM")
                    Dim dParam = getHBPLUSParam("HBPLUS_NB_DPARAM")
                    command(4) = "-h"
                    command(5) = hParam
                    command(6) = "-d"
                    command(7) = dParam
                    command(8) = "-N"
                    command(9) = tmpPDBFile
                Else
                    Dim hParam = getHBPLUSParam("HBPLUS_HPARAM")
                    Dim dParam = getHBPLUSParam("HBPLUS_DPARAM")
                    command(4) = "-h"
                    command(5) = hParam
                    command(6) = "-d"
                    command(7) = dParam
                    command(8) = tmpPDBFile
                    command(9) = ""
                End If
                command(10) = "-wkdir"
                command(11) = tmpDirNameField & "/"
                Console.WriteLine("Command = " & command.ToString())
                ok = runExecutable(command, NO_MONITOR)
                Return ok
            End SyncLock
        End Function

        Friend Overridable Function callLIGFIT(pdb As PDBEntry, haveDomains As Boolean) As Boolean
            SyncLock Me
                Dim ok = True
                Dim resName = New String(1) {}
                Dim exeFile As String = ligplus.Params.exePath & "/ligfit"
                Dim file1 As String = exeFile
                Dim ligfitExe As String = file1
                Dim exeDir = ligplus.Params.exePath
                Dim file2 = exeDir
                Dim ligplotExeDir As String = file2
                Dim prmFile As String = ligplus.Params.paramPath & "/ligplot.prm"
                Dim file3 = prmFile
                Dim ligplotPrm As String = file3
                Dim rewritePrmFile = rewriteParameterFile(prmFile)
                If Not ReferenceEquals(rewritePrmFile, Nothing) Then
                    ligplotPrm = rewritePrmFile
                End If
                Dim command = New String(18) {}
                Dim i As Integer
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = ligfitExe
                command(1) = tmpDirNameField
                i = 2
                If program = LIGPLOT Then
                    Dim chain = pdb.FirstLigandResidue.Chain
                    If chain = " "c Then
                        resName(0) = pdb.FirstLigandResidue.ResName
                        resName(1) = pdb.LastLigandResidue.ResName
                        For k = 0 To 1
                            resName(k) = """-n" & resName(k) & """"
                        Next
                    Else
                        resName(1) = ""
                        resName(0) = ""
                    End If
                    command(2) = resName(0)
                    command(3) = pdb.FirstLigandResidue.ResNum
                    command(4) = resName(1)
                    command(5) = pdb.LastLigandResidue.ResNum
                    command(6) = "" & pdb.FirstLigandResidue.Chain.ToString()
                    i = 7
                Else
                    command(2) = "-dimplot"
                    If haveDomains Then
                        command(3) = "d1"
                        command(4) = "d2"
                    Else
                        command(3) = "" & pdb.getSelectedChain(0).ToString()
                        command(4) = "" & pdb.getSelectedChain(1).ToString()
                    End If
                    i = 5
                    If antibody Then
                        command(5) = "-antibody"
                        i = 6
                    ElseIf orderFirstSeq Then
                        command(5) = "-order"
                        i = 6
                    End If
                End If
                If plotMetal Then
                    command(i) = "-m"
                    i += 1
                End If
                If includeWaters Then
                    command(i) = "-w"
                    i += 1
                End If
                If fitLigands Then
                    command(i) = "-fl"
                    i += 1
                End If
                command(i) = "-exe"
                i += 1
                command(i) = ligplotExeDir
                i += 1
                command(i) = "-prm"
                i += 1
                command(i) = ligplotPrm
                i += 1
                If Not ReferenceEquals(coraFileName, Nothing) AndAlso Not fitLigands Then
                    command(i) = "-aln"
                    i += 1
                    command(i) = coraFileName
                    i += 1
                End If
                Console.Write("LIGFIT Command: ", New Object(-1) {})
                For j = 0 To command.Length - 1
                    If Not command(j).Equals("") Then
                        Console.Write(" {0}", New Object() {command(j)})
                    End If
                Next
                Console.Write(vbLf, New Object(-1) {})
                ok = runExecutable(command, NO_MONITOR)
                Return ok
            End SyncLock
        End Function

        Friend Overridable Function callLIGPLOT(pdb As PDBEntry, tmpPDBFile As String, program As Integer, ranLigFit As Boolean) As Boolean
            SyncLock Me
                Dim ok = True
                Dim resName = New String(1) {}
                Dim nLoops = LIGPLOT_LOOPS
                If program = DIMPLOT AndAlso ranLigFit Then
                    nLoops = DIMPLOT_LOOPS
                End If
                Console.WriteLine("+++ In callLIGPLOT +++")
                Dim exeFile As String = ligplus.Params.exePath & "/ligplot"
                Dim file = exeFile
                Dim ligplotExe As String = file
                Dim ligplotPrm As String = ligplus.Params.paramPath & "/ligplot.prm"
                If program = DIMPLOT Then
                    ligplotPrm = ligplus.Params.paramPath & "/dimplot.prm"
                End If
                file = ligplotPrm
                Dim ligplotPrmName As String = file
                Dim rewritePrmFile = rewriteParameterFile(ligplotPrm)
                If Not ReferenceEquals(rewritePrmFile, Nothing) Then
                    ligplotPrmName = rewritePrmFile
                End If
                Dim command = New String(19) {}
                Dim i As Integer
                For i = 0 To command.Length - 1
                    command(i) = ""
                Next
                command(0) = ligplotExe
                command(1) = tmpPDBFile
                If program = LIGPLOT Then
                    Dim chain = pdb.FirstLigandResidue.Chain
                    If chain = " "c Then
                        resName(0) = pdb.FirstLigandResidue.ResName
                        resName(1) = pdb.LastLigandResidue.ResName
                        For k = 0 To 1
                            resName(k) = """-n" & resName(k) & """"
                        Next
                    Else
                        resName(1) = ""
                        resName(0) = ""
                    End If
                    command(2) = resName(0)
                    command(3) = pdb.FirstLigandResidue.ResNum
                    command(4) = resName(1)
                    command(5) = pdb.LastLigandResidue.ResNum
                    command(6) = "" & pdb.FirstLigandResidue.Chain.ToString()
                End If
                If program = DIMPLOT Then
                    If ranLigFit Then
                        command(2) = "-norot"
                        command(3) = "-dimfit"
                        If ranLigFit Then
                            command(4) = "-ligfit"
                        End If
                    ElseIf dimplotOrientation = PORTRAIT Then
                        command(2) = "-norot"
                    End If
                End If
                command(7) = "-wkdir"
                command(8) = tmpDirNameField & "/"
                command(9) = "-prm"
                command(10) = ligplotPrmName
                command(11) = "-ctype"
                command(12) = params("CONTACT_TYPE")
                i = 13
                If plotMetal Then
                    command(i) = "-m"
                    i += 1
                End If
                If filterWaters Then
                    command(i) = "-wcut"
                    i += 1
                ElseIf includeWaters Then
                    command(i) = "-wat"
                    i += 1
                End If
                If program = LIGPLOT OrElse program = DIMPLOT Then
                    Dim trueFalseString As String = params("NO_ABORT")
                    If trueFalseString Is Nothing Then
                        trueFalseString = "FALSE"
                    End If
                    If trueFalseString.ToUpper().Equals("TRUE") Then
                        command(i) = "-no_abort"
                        i += 1
                    End If
                End If
                Console.WriteLine("LIGPLOT COMMAND: ")
                For j = 0 To command.Length - 1
                    If Not command(j).Equals("") Then
                        Console.Write(" {0}", New Object() {command(j)})
                    End If
                Next
                Console.Write(vbLf, New Object(-1) {})
                ok = runExecutable(command, nLoops)
                Return ok
            End SyncLock
        End Function

        Friend Overridable Function getShifts(ensemble As Ensemble) As Boolean
            Dim ok = True
            Dim shiftAtomCoords = New Single(1) {}
            Dim shift = New Single(1) {}
            Dim lastResidue As Residue = Nothing
            For iPDB = 0 To ensemble.PDBEntryList.Count - 1
                Dim fileOk = True
                Dim pdb As PDBEntry = ensemble.PDBEntryList(iPDB)
                Dim tmpPDBFile As String = tmpDirNameField & "/shifts" + pdb.PDBId.ToString() & ".pdb"
                Dim readFile As ReadPDBFile = Nothing
                Try
                    readFile = New ReadPDBFile(tmpPDBFile)
                    fileOk = True
                Catch __unusedFileNotFoundException1__ As FileNotFoundException
                    fileOk = False
                Catch __unusedIOException2__ As IOException
                    fileOk = False
                End Try
                If fileOk AndAlso readFile IsNot Nothing Then
                    Dim shiftsPDB = readFile.PDBEntry
                    Dim atomList As List(Of Atom) = shiftsPDB.AtomList
                    For i = 0 To atomList.Count - 1
                        Dim shiftAtom = atomList(i)
                        Dim atomName = shiftAtom.AtomName
                        Dim shiftResidue = shiftAtom.Residue
                        shiftAtomCoords(0) = shiftAtom.getCoord(0)
                        shiftAtomCoords(1) = shiftAtom.getCoord(1)
                        Dim atom = pdb.findAtom(atomName, shiftResidue.ResName, shiftResidue.ResNum, shiftResidue.Chain)
                        If atom IsNot Nothing Then
                            Dim residue = atom.Residue
                            If residue IsNot lastResidue Then
                                For j = 0 To 1
                                    Dim coord = atom.getCoord(j)
                                    shift(j) = shiftAtomCoords(j) - coord
                                Next
                                Dim textItem1 = residue.ResidueLabel
                                If textItem1 IsNot Nothing Then
                                    Dim coords = textItem1.Coords
                                    Dim newCoords = New Single(1) {}
                                    For k = 0 To 1
                                        newCoords(k) = coords(k) + shift(k)
                                    Next
                                    textItem1.Coords = newCoords
                                End If
                                residue.setMoved()
                                lastResidue = residue
                            End If
                            For iCoord = 0 To 1
                                Dim coord = atom.getCoord(iCoord)
                                coord += shift(iCoord)
                                atom.setCoord(iCoord, coord)
                            Next
                            Dim textItem = atom.AtomLabel
                            If textItem IsNot Nothing Then
                                Dim coords = textItem.Coords
                                Dim newCoords = New Single(1) {}
                                For j = 0 To 1
                                    newCoords(j) = coords(j) + shift(j)
                                Next
                                textItem.Coords = newCoords
                            End If
                        End If
                    Next
                End If
            Next
            Return ok
        End Function

        Friend Overridable Function runExecutable(command As String(), nloops As Integer) As Boolean
            Return True
        End Function

        Friend Overridable Sub getAnchors(pdb As PDBEntry)
            Dim fileOk = True
            Dim nOff = 0
            Dim minOff = DEFAULT_MINOFF
            Dim nAnchors = 0
            Dim atomNames = ""
            Dim params = pdb.Params
            Dim numberString = params("MIN_LIGATOMS_OFF")
            If Not ReferenceEquals(numberString, Nothing) Then
                minOff = Integer.Parse(numberString)
            End If
            Dim ligandRcmFile As String = tmpDirNameField & "/ligand.rcm"
            Dim readFile As ReadPDBFile = Nothing
            Try
                readFile = New ReadPDBFile(ligandRcmFile)
                fileOk = True
            Catch __unusedFileNotFoundException1__ As FileNotFoundException
                fileOk = False
            Catch __unusedIOException2__ As IOException
                fileOk = False
            End Try
            If fileOk AndAlso readFile IsNot Nothing Then
                Dim anchorsPDB = readFile.PDBEntry
                Dim atomList As List(Of Atom) = anchorsPDB.AtomList
                nAnchors = atomList.Count
                For i = 0 To nAnchors - 1
                    Dim anchorAtom = atomList(i)
                    Dim atomName = anchorAtom.AtomName
                    Dim anchorResidue = anchorAtom.Residue
                    Dim x1 = anchorAtom.getCoord(0)
                    Dim y1 = anchorAtom.getCoord(1)
                    Dim atom = pdb.findAtom(atomName, anchorResidue.ResName, anchorResidue.ResNum, anchorResidue.Chain)
                    If atom IsNot Nothing Then
                        Dim x2 = atom.getCoord(0)
                        Dim y2 = atom.getCoord(1)
                        Dim rmsd = Math.Sqrt((x1 - x2) * (x1 - x2) + (y1 - y2) * (y1 - y2))
                        If rmsd > 2.0R Then
                            Console.WriteLine("ANCHOR: " & atom.AtomName & "  Anchor " & x1.ToString() & ", " & y1.ToString() & "  In plot: " & x2.ToString() & ", " & y2.ToString())
                            If nOff > 0 Then
                                atomNames = atomNames & ","
                            End If
                            Dim aName As String = atom.AtomName.Replace(" ", "")
                            atomNames = atomNames & aName
                            nOff += 1
                        End If
                    End If
                Next
                If nOff > minOff AndAlso nOff > nAnchors / 2 Then
                    'JOptionPane.showMessageDialog(this.frame, message, "Ligand fit failed", 1);
                    Dim message = "<html>LIGPLOT failed to fit most of the ligand atoms to the<BR>first plot's ligand, due to the constraints of flattening.<P>&nbsp;<p>Manual fitting may be required, or you can try<BR>running the plots in reverse order, or ignore."
                ElseIf nOff > minOff Then
                    Dim someOne = "some"
                    Dim nAtomsString As String = nOff.ToString() & " atoms"
                    Dim wereWas = "were: "
                    If nOff = 1 Then
                        someOne = "one"
                        nAtomsString = "atom"
                        wereWas = "was: "
                    End If
                    Dim message = "<html>LIGPLOT failed to fit " & someOne & " of the ligand's atoms to the<BR>first plot's ligand, due to the constraints of flattening.<P>&nbsp;<P>The " & nAtomsString & " that failed to map successfully " & wereWas & atomNames & "<P>&nbsp;<p>Manual fitting may be required, or you can try<BR>running the plots in reverse order, or ignore."
                    '  JOptionPane.showMessageDialog(this.frame, message, "Part of ligand failed to fit", 1);
                End If
            End If
        End Sub

        Friend Overridable Function getHBPLUSParam(paramName As String) As String
            Dim paramValue = "2.70"
            If paramName.Equals("HBPLUS_DPARAM") Then
                paramValue = "3.35"
            ElseIf paramName.Equals("HBPLUS_NB_HPARAM") Then
                paramValue = "2.90"
            ElseIf paramName.Equals("HBPLUS_NB_DPARAM") Then
                paramValue = "3.90"
            End If
            Dim numberString = params(paramName)
            If Not numberString Is Nothing Then
                Dim value = -1.0F
                Try
                    value = Single.Parse(numberString)
                    paramValue = numberString
                Catch __unusedFormatException1__ As FormatException
                    value = -1.0F
                End Try
            End If
            Return paramValue
        End Function

        Friend Overridable Function callDimhtml() As Boolean
            Dim ok = True
            Dim exeFile As String = ligplus.Params.exePath & "/dimhtml"
            Dim file = exeFile
            Dim dimhtmlExe As String = file
            Dim command = New String(19) {}
            For i = 0 To command.Length - 1
                command(i) = ""
            Next
            command(0) = dimhtmlExe
            command(1) = "none"
            command(2) = "-dimp"
            command(3) = "-dir"
            command(4) = tmpDirNameField
            command(5) = "-flip"
            command(6) = "-ctype"
            command(7) = params("CONTACT_TYPE")
            Dim ipos = 8
            If ensemble.OrientationOption = PORTRAIT Then
                command(ipos) = "-portrait"
                ipos += 1
            End If
            Dim hPhobicRadius = 0.0F
            If Not Equals(params("IFACE_HPHOBIC1_RADIUS"), Nothing) Then
                Dim numberString = params("IFACE_HPHOBIC1_RADIUS")
                hPhobicRadius = Single.Parse(numberString)
            End If
            If Not Equals(params("IFACE_HPHOBIC2_RADIUS"), Nothing) Then
                Dim numberString = params("IFACE_HPHOBIC2_RADIUS")
                Dim hPhobicRadius2 = Single.Parse(numberString)
                If hPhobicRadius2 > hPhobicRadius Then
                    hPhobicRadius = hPhobicRadius2
                End If
            End If
            If hPhobicRadius > 0.0F Then
                command(ipos) = "-hs"
                ipos += 1
                command(ipos) = "" & hPhobicRadius.ToString()
                ipos += 1
            End If
            Console.WriteLine(" orderFirstSeq = " & orderFirstSeq.ToString())
            Console.WriteLine(" ipos = " & ipos.ToString())
            If orderFirstSeq Then
                command(ipos) = "-order"
                ipos += 1
            End If
            If includeWaters Then
                command(ipos) = "-waters"
                ipos += 1
            End If
            Console.WriteLine("Running DIMHTML: " & command.ToString())
            ok = runExecutable(command, DIMHTML_LOOPS)
            Return ok
        End Function

        Friend Overridable Function rewriteParameterFile(prmFile As String) As String
            Dim newFile As String = tmpDirNameField & "/ligplot.prm"
            Dim fileTmp = newFile
            Dim ligplotPrm As String = fileTmp
            Try
                Dim rewritePrm As RewritePrm = New RewritePrm(ensemble, prmFile, newFile, program, params)
            Catch __unusedFileNotFoundException1__ As FileNotFoundException
                Console.WriteLine("*** ERROR. File not found: " & prmFile)
                errorMessage = "File not found: " & prmFile
                ligplotPrm = Nothing
            Catch __unusedIOException2__ As IOException
                Console.WriteLine("*** ERROR. I/O error reading: " & prmFile)
                errorMessage = "I/O error reading: " & prmFile
                ligplotPrm = Nothing
            End Try
            Return ligplotPrm
        End Function






        Private Function writeDomFile(pdb As PDBEntry, domFile As String) As Boolean
            Dim ok = True
            Dim out As PrintStream = Nothing
            Try
                out = New PrintStream(domFile)
                For i = 0 To 1
                    Dim domainDefinition = pdb.getDomainDefinition(i)
                    out.format("Domain %d: %s" & vbLf, New Object() {Convert.ToInt32(i + 1), domainDefinition})
                Next
            Catch __unusedIOException1__ As IOException
                Console.WriteLine("*** ERROR. I/O error writing: " & domFile)
                ok = False
            Finally
                If out IsNot Nothing Then
                    out.Close()
                End If
            End Try
            Return ok
        End Function

        Private Function writeRasMolScript(ensemble As Ensemble, scriptFile As String) As Boolean
            Dim ok = True
            Try
                Call New WriteRasMolScript(ensemble).write(scriptFile)
            Catch __unusedIOException1__ As IOException
                Console.WriteLine("*** ERROR. I/O error writing: " & scriptFile)
                ok = False
            End Try
            Return ok
        End Function

        Private Function writePyMOLScript(ensemble As Ensemble, scriptFile As String, coordsFile As String) As Boolean
            Dim ok = True
            Try
                Call New WritePyMOLScript(ensemble, coordsFile).write(scriptFile)
            Catch __unusedIOException1__ As IOException
                Console.WriteLine("*** ERROR. I/O error writing: " & scriptFile)
                ok = False
            End Try
            Return ok
        End Function

        Private Function writeTmpPDB(pdb As PDBEntry, tmpPDBFile As String, prog As Integer, allResid As Boolean) As Boolean
            Dim ok = True
            Try
                Dim conectOptionString = params("CONECT_OPTION")
                Dim conectOption = 1
                Try
                    conectOption = Integer.Parse(conectOptionString)
                Catch __unusedFormatException1__ As FormatException
                    conectOption = 1
                End Try
                Call New WritePDBFile(frame, pdb, prog, allResid, conectOption).write(tmpPDBFile)
                Dim nAtoms = WritePDBFile.Atoms
                If nAtoms = 0 Then
                    'JOptionPane.showMessageDialog(this.frame, this.errorMessage, "No atoms written out. No plot to be produced", 0);
                    ok = False
                End If
                Console.WriteLine("Number of atoms written out: " & nAtoms.ToString())
            Catch __unusedIOException1__ As IOException
                Console.WriteLine("*** ERROR. I/O error writing: " & tmpPDBFile)
                ok = False
            End Try
            Return ok
        End Function


        Public Overridable Sub deleteTmpDir()
            If Not ReferenceEquals(tmpDirNameField, Nothing) Then
                tmpDirNameField.DeleteFile(strictFile:=False)
            End If
            tmpDirNameField = Nothing
        End Sub

        Public Overridable ReadOnly Property TmpDirName As String
            Get
                Return tmpDirNameField
            End Get
        End Property

        Public Overridable Function readFile(fileName As String, name As String, fileType As Integer, readFrom As Integer, ranLigfit As Boolean) As Boolean
            SyncLock Me
                Dim gzip = False
                Dim ok = False
                ok = True
                Try
                    If fileType = 1 Then
                        Dim pdbCode = stripExtension(name)
                        Console.WriteLine("Reading .drw file: " & fileName)
                        Console.WriteLine("Calling ReadDrwFile with antibody = " & antibody.ToString())
                        Dim read As ReadDrwFile = New ReadDrwFile(ensemble, fileName, pdbCode, antibody, antibodyNumberingScheme, fullHeavyChain, fullLightChain, fullAntigenChain, dimplotOrientation, ranLigfit, frame, plotArea)
                        pdb = read.LatestPDB
                        If pdb Is Nothing OrElse pdb.Natoms = 0 Then
                            Console.WriteLine("*** ERROR. LIGPLOT failed - No atom coords found")
                            Dim reasonForError = read.ErrorMessage
                            If ReferenceEquals(reasonForError, Nothing) Then
                                reasonForError = "possibly this case is too difficult to plot"
                            End If
                            errorMessage = "<html>LIGPLOT failed to produce a plot<P>" & reasonForError
                            ok = False
                        End If
                    ElseIf fileType <> 0 Then
                        Console.WriteLine("*** ERROR. Unidentified file type:" & fileName)
                        errorMessage = "Unidentified file type"
                        ok = False
                    End If
                Catch __unusedFileNotFoundException1__ As FileNotFoundException
                    Console.WriteLine("*** ERROR. File not found: " & fileName)
                    errorMessage = "File not found: " & fileName
                    ok = False
                Catch __unusedIOException2__ As IOException
                    Console.WriteLine("*** ERROR. I/O error reading: " & fileName)
                    errorMessage = "I/O error reading: " & fileName
                    ok = False
                End Try
                Return ok
            End SyncLock
        End Function







        Public Shared Function stripExtension(name As String) As String
            Dim length = name.Length
            Dim firstDot = -1
            Dim i = 0

            While i < length AndAlso firstDot = -1
                If name(i) = "."c Then
                    firstDot = i
                End If

                i += 1
            End While
            If firstDot > -1 Then
                name = name.Substring(0, firstDot)
            End If
            Return name
        End Function

        Private Sub createTmpDir(frame As LigPlusFrame, prefix As String)

        End Sub
    End Class

End Namespace
