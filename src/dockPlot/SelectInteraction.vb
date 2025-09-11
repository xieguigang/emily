Imports Microsoft.VisualBasic.Text.Parser
Imports System
Imports System.Collections.Generic

Namespace ligplus

    Public Class SelectInteraction
        Public Shared CANCEL As Integer = 0

        Public Shared RUN_LIGPLOT As Integer = 1

        Public Shared RUN_DIMPLOT As Integer = 2

        Public Shared RUN_ANTIBODY As Integer = 3

        Public Shared MAX_LIGAND_RESIDUES As Integer = 20

        Public Shared BOTH_FLAGS As Integer = 0

        Public Shared PLOT_FLAG As Integer = 1

        Public Shared WANTED_FLAG As Integer = 2

        Public Const INTERACT_DIST As Single = 10.0F

        Private errorField As Boolean = False

        Private includeWaters As Boolean = False

        Private dimplotIncludeWaters As Boolean = False

        Private filterWaters As Boolean = False

        Private orderFirstSeq As Boolean = False

        Private fitLigands As Boolean = False

        Private flaggedMinCoord As Single()

        Private flaggedMaxCoord As Single()

        Private nFlagged As Integer = 0

        Private nLigands As Integer = 0

        Private optionField As Integer = CANCEL

        Private antibodyNumberingSchemeField As Integer

        Private orientation As Integer

        Private firstFullResNum As Integer

        Private lastFullResNum As Integer

        Private heavyChainField As Char = "H"c

        Private lightChainField As Char = "L"c

        Private antigenChainField As Char = "A"c

        Private selectedMoleculeField As Molecule = Nothing

        Private flaggedResidueList As List(Of Object)

        Private pdb As PDBEntry = Nothing

        Private domainDefinition As String() = New String(1) {}

        Private rName As String

        Private rNum As String

        Private firstResNum As String = ""

        Private lastResNum As String = ""

        Private fullHeavyChainField As String = "H"

        Private fullLightChainField As String = "L"

        Private fullAntigenChainField As String = "A"
        Private antigenChainTextField As String
        Private PDBTitleLabel As String
        Private PDBCodeLabel As String
        Private heavyChainTextField As String
        Private lightChainTextField As String

        Public Sub New(pdb As PDBEntry)
            Me.pdb = pdb

        End Sub



        Private Sub addToFlaggedList(residue As Residue)
            flaggedResidueList.Add(residue)
            For i = 0 To 2
                If nFlagged = 0 Then
                    flaggedMinCoord(i) = residue.getCoordsMin(i)
                    flaggedMaxCoord(i) = residue.getCoordsMax(i)
                Else
                    If residue.getCoordsMax(i) > flaggedMaxCoord(i) Then
                        flaggedMaxCoord(i) = residue.getCoordsMax(i)
                    End If
                    If residue.getCoordsMin(i) < flaggedMinCoord(i) Then
                        flaggedMinCoord(i) = residue.getCoordsMin(i)
                    End If
                End If
            Next
            nFlagged += 1
        End Sub

        Public Overridable Function checkInRange(residue As Residue, margin As Single) As Boolean
            Dim inRange = True
            Dim i = 0

            While i < 3 AndAlso inRange = True
                If residue.getCoordsMin(i) - flaggedMaxCoord(i) > margin Then
                    inRange = False
                End If
                If flaggedMinCoord(i) - residue.getCoordsMax(i) > margin Then
                    inRange = False
                End If

                i += 1
            End While
            Return inRange
        End Function

        Private Function extractResNum(token As String) As Boolean
            Dim isResName = False
            Dim isResNum = False
            Dim insertionCode = " "c
            Dim first = token(0)
            Dim last = token(token.Length - 1)
            If token.Length > 2 Then
                If first = last AndAlso (first = """"c OrElse first = "'"c) Then
                    token = token.Substring(0, token.Length - 1)
                    token = token.Substring(1)
                    first = token(0)
                    last = token(token.Length - 1)
                End If
            End If
            If token.Length = 1 AndAlso (first = """"c OrElse first = "'"c) Then
                Return False
            End If
            If token.Length > 1 AndAlso (last = """"c OrElse last = "'"c) Then
                token = token.Substring(0, token.Length - 1)
                If token.Length = 1 Then
                    token = "  " & token
                ElseIf token.Length = 2 Then
                    token = " " & token
                End If
                token = "-n" & token
            End If
            If token.Length > 2 AndAlso token.Substring(0, 2).Equals("-n") Then
                isResName = True
                token = token.Substring(2)
            Else
                isResNum = True
                Dim numeric = True
                If token.Length = 1 AndAlso (token(0) < "0"c OrElse token(0) > "9"c) AndAlso token(0) <> "-"c Then
                    numeric = False
                Else
                    For i = 0 To token.Length - 1 - 1
                        If (token(i) < "0"c OrElse token(i) > "9"c) AndAlso token(i) <> "-"c Then
                            numeric = False
                        End If
                    Next
                End If
                If Not numeric Then
                    isResNum = False
                    isResName = True
                ElseIf token.Length > 1 Then
                    Dim ch = token(token.Length - 1)
                    If ch < "0"c OrElse ch > "9"c Then
                        insertionCode = ch
                        token = token.Substring(0, token.Length - 1)
                    End If
                End If
            End If
            If isResName Then
                If token.Length > 3 Then
                    isResName = False
                ElseIf token.Length = 2 Then
                    token = token & " "
                ElseIf token.Length = 1 Then
                    token = token & "  "
                End If
                If isResName Then
                    rName = token.ToUpper()
                End If
            End If
            If isResNum Then
                If token.Length > 4 Then
                    isResNum = False
                Else
                    If token.Length = 3 Then
                        token = " " & token
                    ElseIf token.Length = 2 Then
                        token = "  " & token
                    ElseIf token.Length = 1 Then
                        token = "   " & token
                    End If
                    token = token & insertionCode.ToString()
                    rNum = token.ToUpper()
                End If
            End If
            Return isResNum
        End Function

        Public Overridable ReadOnly Property SelectedMolecule As Molecule
            Get
                Return selectedMoleculeField
            End Get
        End Property

        Private Sub flagInteractingResidues()
            Dim residueList As List(Of Residue) = pdb.ResidueList
            For i = 0 To residueList.Count - 1
                Dim residue = residueList(i)
                If Not residue.Wanted Then
                    Dim inRange = checkInRange(residue, 10.0F)
                    If inRange Then
                        inRange = False
                        Dim j = 0

                        While j < flaggedResidueList.Count AndAlso Not inRange
                            Dim flaggedResidue As Residue = flaggedResidueList(j)
                            inRange = pdb.checkInRange(residue, flaggedResidue, 10.0F)
                            If inRange Then
                                flagResidue(residue, False)
                            End If

                            j += 1
                        End While
                    End If
                End If
            Next
        End Sub

        Private Function flagLigandResidues(residueRange As String, resName As String(), resNum As String(), chain As Char) As Boolean
            Dim done = False
            Dim foundEnd = False
            Dim inRange = False
            Dim match = False
            Dim ok = True
            Dim nResid = 0
            Dim fullChain As String = chain.ToString() & ""
            Dim residueList As List(Of Residue) = pdb.ResidueList
            If residueList.Count = 0 Then
                Return False
            End If
            Dim fullInsCode = " "
            Dim resNo = resNum(0)
            Dim len = resNum(0).Length
            Dim ch = resNum(0)(len - 1)
            If ch < "0"c OrElse ch > "9"c Then
                fullInsCode = ch.ToString() & ""
                resNo = resNum(0).Substring(0, len - 1)
            End If
            Dim fullResNum As Integer = Integer.Parse(resNo.Trim())
            Dim i = 0

            While i < residueList.Count AndAlso Not done
                Dim residue = residueList(i)
                match = False
                If fullChain.Equals(residue.FullChain) Then
                    If Not inRange Then
                        If fullResNum = residue.FullResNum AndAlso fullInsCode.Equals(residue.FullInsCode) Then
                            If Not ReferenceEquals(resName(0), Nothing) AndAlso resName(0).Length > 0 Then
                                If resName(0).Equals(residue.FullResName) Then
                                    match = True
                                End If
                            Else
                                match = True
                            End If
                        End If
                        If match Then
                            inRange = True
                        End If
                    End If
                    If inRange Then
                        match = False
                        Dim residueNumber As Integer = Integer.Parse(resNum(1).Trim())
                        If residueNumber = residue.FullResNum Then
                            If Not ReferenceEquals(resName(1), Nothing) AndAlso resName(1).Length > 0 Then
                                If resName(1).Equals(residue.FullResName) Then
                                    match = True
                                End If
                            Else
                                match = True
                            End If
                        End If
                        If match Then
                            done = True
                            foundEnd = True
                        End If
                    End If
                Else
                    inRange = False
                End If
                If inRange Then
                    flagResidue(residue, True)
                    If nResid = 0 Then
                        pdb.FirstLigandResidue = residue
                    End If
                    pdb.LastLigandResidue = residue
                    nResid += 1
                End If

                i += 1
            End While
            If nResid = 0 Then
                '  JOptionPane.showMessageDialog(this, "<html>Ligand residues not found: " + residueRange, "No ligand", 0);
                ok = False
            ElseIf Not foundEnd Then
                '  JOptionPane.showMessageDialog(this, "<html>End ligand residue not found: " + residueRange, "Ligand end not found", 0);
                ok = False
            ElseIf nResid > MAX_LIGAND_RESIDUES Then
                Dim options = New Object() {"Continue", "Cancel"}
                '  int n = JOptionPane.showOptionDialog(this, "<html>Too many residues in ligand: " + nResid + ". Maximum allowed is " + MAX_LIGAND_RESIDUES, "Too many ligand residues", 0, 3, null, options, options[1]);
                ' if (n != 0)
                '  {
                ok = False
                ' }
            End If
            Return ok
        End Function

        Private Sub flagAllWaters()
            Dim nResid = 0
            Dim residueList As List(Of Residue) = pdb.ResidueList
            For i = 0 To residueList.Count - 1
                Dim residue = residueList(i)
                If residue.FullResName.Equals("HOH") Then
                    flagResidue(residue, True)
                End If
            Next
        End Sub

        Private Sub flagMoleculeResidues(molecule As Molecule, domain As Integer)
            Dim residueList As List(Of Residue) = molecule.ResidueList
            For i = 0 To residueList.Count - 1
                Dim residue = residueList(i)
                flagResidue(residue, True)
                If domain = -1 Then
                    If i = 0 Then
                        pdb.FirstLigandResidue = residue
                    End If
                    pdb.LastLigandResidue = residue
                Else
                    residue.Domain = domain + 1
                End If
            Next
        End Sub

        Private Function flagPPRangeResidues(domainDefinition As String, fullResNum As Integer(), fullChain As String, wholeChain As Boolean, domain As Integer) As Boolean
            Dim done = False
            Dim foundEnd = False
            Dim inRange = False
            Dim ok = True
            Dim nDuplicates = 0
            Dim nResid = 0
            firstResNum = ""
            lastResNum = ""
            Dim residueList As List(Of Residue) = pdb.ResidueList
            Dim i = 0

            While i < residueList.Count AndAlso Not done
                Dim residue = residueList(i)
                If fullChain.Equals(residue.FullChain) Then
                    If wholeChain Then
                        inRange = True
                        foundEnd = True
                        Dim moleculeType = residue.Molecule.MoleculeType
                        If moleculeType <> 1 AndAlso moleculeType <> 3 AndAlso moleculeType <> 4 Then
                            inRange = False
                        End If
                    End If
                    If Not inRange Then
                        If fullResNum(0) = residue.FullResNum Then
                            inRange = True
                        End If
                    End If
                    If inRange Then
                        If fullResNum(1) = residue.FullResNum Then
                            done = True
                            foundEnd = True
                        End If
                    End If
                Else
                    inRange = False
                End If
                If inRange Then
                    If residue.Wanted AndAlso residue.ResidueType <> 6 Then
                        nDuplicates += 1
                    Else
                        flagResidue(residue, True)
                        residue.Domain = domain + 1
                        If firstResNum.Equals("") Then
                            firstResNum = residue.ResNum
                            firstFullResNum = residue.FullResNum
                        End If
                        lastResNum = residue.ResNum
                        lastFullResNum = residue.FullResNum
                        nResid += 1
                    End If
                End If

                i += 1
            End While
            Console.WriteLine("Number of residues: " & nResid.ToString())
            If nResid = 0 Then
                '	JOptionPane.showMessageDialog(this, "<html>No residues found for chain [" + fullChain + "]", "Missing chain", 0);
                If wholeChain Then
                Else
                    '	JOptionPane.showMessageDialog(this, "<html>No residues found in residue range: " + fullResNum[0] + " - " + fullResNum[1] + " chain " + fullChain, "No residues", 0);
                End If
                ok = False
            ElseIf Not foundEnd Then
                '  JOptionPane.showMessageDialog(this, "<html>End residue not found: " + fullResNum[1] + " chain " + fullChain, "End residue not found", 0);
                ok = False
            ElseIf nDuplicates > 0 Then
                '  JOptionPane.showMessageDialog(this, "<html>Residue ranges overlap: " + domainDefinition, "Duplicate residues", 0);
                ok = False
            End If
            Return ok
        End Function

        Private Sub flagResidue(residue As Residue, addToList As Boolean)
            residue.Wanted = True
            residue.Molecule.Wanted = True
            If addToList Then
                addToFlaggedList(residue)
            End If
            Dim attachedResidue = residue.Attachment
            If attachedResidue IsNot Nothing Then
                attachedResidue.Wanted = True
                If addToList Then
                    addToFlaggedList(attachedResidue)
                End If
            End If
        End Sub

        Private Function formatResNum(token As String) As String
            Dim insertionCode = " "c
            Dim resNum = ""
            If token.Length > 1 Then
                Dim ch = token(token.Length - 1)
                If ch < "0"c OrElse ch > "9"c Then
                    insertionCode = ch
                    token = token.Substring(0, token.Length - 1)
                End If
            End If
            If token.Length = 3 Then
                token = " " & token
            ElseIf token.Length = 2 Then
                token = "  " & token
            ElseIf token.Length = 1 Then
                token = "   " & token
            End If
            resNum = token & insertionCode.ToString()
            Return resNum
        End Function

        Public Overridable ReadOnly Property [Error] As Boolean
            Get
                Return errorField
            End Get
        End Property

        Public Overridable ReadOnly Property LigandList As String()
            Get
                Dim nItems = 0
                nLigands = pdb.getMoleculeTypeCount(4) + pdb.getMoleculeTypeCount(5)
                If nLigands = 0 Then
                    Dim arrayOfString = New String(0) {}
                    arrayOfString(0) = "No ligands or metals found"
                    Return arrayOfString
                End If
                Dim ligands = New String(nLigands - 1) {}
                Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
                Dim i = 0

                While i < moleculeList.Count AndAlso nItems < nLigands
                    Dim molecule = moleculeList(i)
                    If molecule.MoleculeType = 4 OrElse molecule.MoleculeType = 5 Then
                        Dim fontColour As String
                        If molecule.MoleculeType = 4 Then
                            fontColour = "<font color=blue>"
                        Else
                            fontColour = "<font color=green>"
                        End If
                        Dim ligandDescription = molecule.getLigandDescription(True)
                        If ReferenceEquals(ligandDescription, Nothing) OrElse ligandDescription.Equals("") Then
                            ligandDescription = "Unknown"
                        End If
                        Dim [string] = "<html>" & fontColour & "<B>" & molecule.LigandSequence & "</B>: " & molecule.ResidueRange & " - " & ligandDescription & "</font>"
                        ligands(nItems) = [string]
                        nItems += 1
                    End If

                    i += 1
                End While
                Return ligands
            End Get
        End Property

        Public Overridable Sub getAntibodyChains()
            Dim haveHeavy = False
            Dim haveLight = False
            Dim possibleAntigen = ""
            Dim nProtProt = pdb.NProtProt
            If nProtProt > 2 Then
                Dim protPairsList1 As List(Of Molecule) = pdb.getProtPairsList(0)
                Dim protPairsList2 As List(Of Molecule) = pdb.getProtPairsList(1)
                For i = 0 To nProtProt - 1
                    Dim molecule1 = protPairsList1(i)
                    Dim molecule2 = protPairsList2(i)
                    Dim chain1 = molecule1.Chain
                    Dim chain2 = molecule2.Chain
                    Dim fullChain1 = molecule1.FullChain
                    Dim len = fullChain1.Length
                    If len = 0 OrElse fullChain1(0) = " "c Then
                        fullChain1 = chain1.ToString() & ""
                    End If
                    Dim fullChain2 = molecule2.FullChain
                    len = fullChain2.Length
                    If len = 0 OrElse fullChain2(0) = " "c Then
                        fullChain2 = chain2.ToString() & ""
                    End If
                    If fullChain1.Equals("H") OrElse fullChain2.Equals("H") Then
                        haveHeavy = True
                        If fullChain1.Equals("H") AndAlso fullChain2.Equals("L") Then
                            possibleAntigen = fullChain2
                        ElseIf fullChain2.Equals("H") AndAlso fullChain1.Equals("L") Then
                            possibleAntigen = fullChain1
                        End If
                    End If
                    If fullChain1.Equals("L") OrElse fullChain2.Equals("L") Then
                        haveLight = True
                    End If
                Next
                If haveHeavy AndAlso haveLight Then
                    Me.heavyChainTextField = "H"
                    Me.lightChainTextField = "L"
                End If
                Dim done = False
                Dim j = 0

                While j < nProtProt AndAlso Not done
                    Dim molecule1 = protPairsList1(j)
                    Dim molecule2 = protPairsList2(j)
                    Dim chain1 = molecule1.Chain
                    Dim chain2 = molecule2.Chain
                    Dim fullChain1 = molecule1.FullChain
                    Dim len = fullChain1.Length
                    If len = 0 OrElse fullChain1(0) = " "c Then
                        fullChain1 = chain1.ToString() & ""
                    End If
                    Dim fullChain2 = molecule2.FullChain
                    len = fullChain2.Length
                    If len = 0 OrElse fullChain2(0) = " "c Then
                        fullChain2 = chain2.ToString() & ""
                    End If
                    Dim intChain = ""
                    Dim otherChain = ""
                    Dim skip = False
                    If fullChain1.Equals("H") AndAlso Not fullChain2.Equals("L") Then
                        intChain = fullChain2
                        otherChain = "L"
                    ElseIf fullChain1.Equals("L") AndAlso Not fullChain2.Equals("H") Then
                        intChain = fullChain2
                        otherChain = "H"
                    ElseIf fullChain2.Equals("H") AndAlso Not fullChain1.Equals("L") Then
                        intChain = fullChain1
                        otherChain = "L"
                    ElseIf fullChain2.Equals("L") AndAlso Not fullChain1.Equals("H") Then
                        intChain = fullChain1
                        otherChain = "H"
                    Else
                        skip = True
                    End If
                    If Not skip Then
                        Dim k = j + 1

                        While k < nProtProt AndAlso Not done
                            molecule1 = protPairsList1(k)
                            molecule2 = protPairsList2(k)
                            chain1 = molecule1.Chain
                            chain2 = molecule2.Chain
                            fullChain1 = molecule1.FullChain
                            len = fullChain1.Length
                            If len = 0 OrElse fullChain1(0) = " "c Then
                                fullChain1 = chain1.ToString() & ""
                            End If
                            fullChain2 = molecule2.FullChain
                            len = fullChain2.Length
                            If len = 0 OrElse fullChain2(0) = " "c Then
                                fullChain2 = chain2.ToString() & ""
                            End If
                            If fullChain1.Equals(intChain) AndAlso fullChain2.Equals(otherChain) OrElse fullChain2.Equals(intChain) AndAlso fullChain1.Equals(otherChain) Then
                                antigenChainTextField = intChain
                                done = True
                            End If

                            k += 1
                        End While
                    End If

                    j += 1
                End While
                If Not done AndAlso Not possibleAntigen.Equals("") Then
                    antigenChainTextField = possibleAntigen
                End If
            End If
        End Sub

        Public Overridable ReadOnly Property ProtProtList As String()
            Get
                Dim nItems = 0
                Dim nProtProt = pdb.NProtProt
                If nProtProt = 0 Then
                    Dim arrayOfString = New String(0) {}
                    arrayOfString(0) = "No protein-protein interactions found"
                    Return arrayOfString
                End If
                Dim lProtProtList = New String(nProtProt - 1) {}
                Dim protPairsList1 As List(Of Molecule) = pdb.getProtPairsList(0)
                Dim protPairsList2 As List(Of Molecule) = pdb.getProtPairsList(1)
                For i = 0 To nProtProt - 1
                    Dim molecule1 = protPairsList1(i)
                    Dim molecule2 = protPairsList2(i)
                    Dim [string] = "<html><font color=blue><b>Chain " & molecule1.FullChain & " : Chain " & molecule2.FullChain & "</b></font>"
                    lProtProtList(nItems) = [string]
                    nItems += 1
                Next
                Return lProtProtList
            End Get
        End Property

        Friend Overridable ReadOnly Property HeavyChain As Char
            Get
                Return heavyChainField
            End Get
        End Property

        Friend Overridable ReadOnly Property LightChain As Char
            Get
                Return lightChainField
            End Get
        End Property

        Friend Overridable ReadOnly Property AntigenChain As Char
            Get
                Return antigenChainField
            End Get
        End Property

        Friend Overridable ReadOnly Property FullHeavyChain As String
            Get
                Return fullHeavyChainField
            End Get
        End Property

        Friend Overridable ReadOnly Property FullLightChain As String
            Get
                Return fullLightChainField
            End Get
        End Property

        Friend Overridable ReadOnly Property FullAntigenChain As String
            Get
                Return fullAntigenChainField
            End Get
        End Property




        Public Overridable ReadOnly Property FitLigandsOption As Boolean
            Get
                Return fitLigands
            End Get
        End Property


        Public Overridable ReadOnly Property AntibodyNumberingScheme As Integer
            Get
                Return antibodyNumberingSchemeField
            End Get
        End Property


        Public Overridable ReadOnly Property [Option] As Integer
            Get
                Return optionField
            End Get
        End Property

        Private Function getSelectedChain(listItem As Integer, i As Integer) As Molecule
            Dim molecule As Molecule = Nothing
            Dim protPairsList As List(Of Molecule) = pdb.getProtPairsList(i)
            molecule = protPairsList(listItem)
            Return molecule
        End Function

        Public Overridable Function getSelectedLigand(item As Integer) As Molecule
            Dim nItems = 0
            Dim selectedMolecule As Molecule = Nothing
            Dim moleculeList As List(Of Molecule) = pdb.MoleculeList
            Dim i = 0
            While i < moleculeList.Count AndAlso selectedMolecule Is Nothing
                Dim molecule = moleculeList(i)
                If molecule.MoleculeType = 4 OrElse molecule.MoleculeType = 5 Then
                    If nItems = item Then
                        selectedMolecule = molecule
                    End If
                    nItems += 1
                End If

                i += 1
            End While
            Return selectedMolecule
        End Function

        Private Sub initFlaggedList()
            flaggedResidueList = New List(Of Object)()
            nFlagged = 0
            For i = 0 To 2
                flaggedMinCoord(i) = 0.0F
                flaggedMaxCoord(i) = 0.0F
            Next
        End Sub

        Private Function interpretDomainDefinition(domainDefinition As String, domain As Integer) As String
            Dim ok = True
            Dim resRangeNext = True
            Dim chainNext = False
            Dim wholeChain = False
            Dim nRange = 0
            Dim iRes = 0
            Dim formattedDefinition = ""
            Dim fullResNum = New Integer(1) {}
            Dim tokens As Scanner = New Scanner(domainDefinition)
            While tokens.HasNext() AndAlso ok
                Dim token As String = tokens.Next()
                If token.Equals("&") Then
                    formattedDefinition = formattedDefinition & "& "
                    chainNext = False
                    resRangeNext = True
                    wholeChain = False
                    iRes = 0
                    Continue While
                End If
                If chainNext Then
                    Dim fullChain = token
                    firstResNum = ""
                    lastResNum = ""
                    ok = flagPPRangeResidues(domainDefinition, fullResNum, fullChain, wholeChain, domain)
                    If wholeChain Then
                        formattedDefinition = formattedDefinition & firstFullResNum.ToString() & " - " & lastFullResNum.ToString() & " "
                    End If
                    formattedDefinition = formattedDefinition & fullChain & " "
                    nRange += 1
                    chainNext = False
                    resRangeNext = True
                    Continue While
                End If
                If resRangeNext AndAlso token.Equals("*") Then
                    wholeChain = True
                    chainNext = True
                    resRangeNext = False
                    Continue While
                End If
                If resRangeNext AndAlso iRes < 2 Then
                    Dim minus = False
                    Dim ch = token(0)
                    If ch = "-"c Then
                        minus = True
                    End If
                    Dim bothMinus = False
                    Dim nMinus = 0
                    Dim len = token.Length
                    For i = 0 To len - 1
                        ch = token(i)
                        If ch = "-"c Then
                            nMinus += 1
                        End If
                    Next
                    If nMinus = 3 Then
                        bothMinus = True
                    End If
                    Dim resNums As Scanner = New Scanner(token)
                    resNums.UseDelimiter("-")
                    While resNums.HasNext()
                        Dim resNum As String = resNums.Next()
                        Try
                            fullResNum(iRes) = Integer.Parse(resNum)
                        Catch __unusedFormatException1__ As FormatException
                            fullResNum(iRes) = 0
                            ok = False
                        End Try
                        If bothMinus OrElse iRes = 0 AndAlso minus Then
                            fullResNum(iRes) = -1 * fullResNum(iRes)
                        End If
                        formattedDefinition = formattedDefinition & fullResNum(iRes).ToString() & " "
                        If iRes = 0 Then
                            formattedDefinition = formattedDefinition & "- "
                        End If
                        iRes += 1
                    End While
                    chainNext = True
                    resRangeNext = False
                End If
            End While
            If Not ok Then
                '  JOptionPane.showMessageDialog(this, "<html>Invalid residue range: " + domainDefinition, "Residue range error", 0);
                formattedDefinition = ""
            End If
            Return formattedDefinition
        End Function

        Public Overridable Function interpretResidueRange(residueRange As String) As Boolean
            Dim ok = True
            Dim chain = " "c
            Dim range = 0
            Dim resName = New String(1) {}
            Dim resNum = New String(1) {}
            For i = 0 To 1
                resNum(i) = Nothing
                resName(i) = Nothing
            Next
            rNum = Nothing
            rName = Nothing
            Dim tokens As Scanner = New Scanner(residueRange)
            While tokens.HasNext()
                Dim token As String = tokens.Next()
                If range < 2 Then
                    Dim haveResNum = extractResNum(token)
                    If haveResNum Then
                        resName(range) = rName
                        resNum(range) = rNum
                        rNum = Nothing
                        rName = Nothing
                        range += 1
                    End If
                    Continue While
                End If
                If token.Length = 1 Then
                    chain = token(0)
                End If
            End While
            If ReferenceEquals(resNum(0), Nothing) OrElse ReferenceEquals(resNum(1), Nothing) OrElse resNum(0).Length = 0 OrElse resNum(1).Length = 0 Then
                '  JOptionPane.showMessageDialog(this, "<html>Invalid ligand range: " + residueRange, "Ligand range error", 0);
                ok = False
            Else
                ok = flagLigandResidues(residueRange, resName, resNum, chain)
            End If
            Return ok
        End Function

        Public Overridable Sub showPDBstats(pdb As PDBEntry)
            Dim nItems = 0
            If Not ReferenceEquals(pdb.PDBCode, Nothing) Then
                PDBCodeLabel = "<html><B>PDB code: <font color=#8B0000>" & pdb.PDBCode & "</font></B>"
            End If
            If Not ReferenceEquals(pdb.FullTitle, Nothing) Then
                Dim title = pdb.FullTitle
                If title.Length > 0 Then
                    title = TextItem.typesetText(title)
                End If
                PDBTitleLabel = "<html><B>Title:</B> <font color=#8B0000>" & title & "</font>"
            End If
            Dim message = ""
            For type = 1 To 7
                Dim count = pdb.getMoleculeTypeCount(type)
                If count > 0 Then
                    If nItems > 0 Then
                        message = message & "; "
                    End If
                    message = message & "<B>" & count.ToString() & "</B> " & Molecule.MOLECULE_TYPE(type)
                    If count > 1 Then
                        message = message & "s"
                    End If
                    If type = 1 OrElse type = 3 OrElse type = 2 Then
                        message = message & " (<B>" & pdb.getMoleculeChains(type) & "</B>)"
                    End If
                    nItems += 1
                End If
            Next

        End Sub

    End Class

End Namespace
