Imports Microsoft.VisualBasic
Imports Microsoft.VisualBasic.Text
Imports Microsoft.VisualBasic.Text.Parser
Imports PrintStream = System.IO.StreamWriter

Namespace ligplus

    Public Class WritePDBFile
        Public Shared STANDARD As Integer = 0

        Public Shared LIGPLOT As Integer = 1

        Public Shared DIMPLOT As Integer = 2

        Private Shared nAtoms As Integer

        Public Shared letters As String = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

        Private frame As LigPlusFrame

        Private chain As Char

        Private lastChain As Char = " "c

        Private atomNumber As Integer

        Private dummyResNum As Integer = 0

        Private nextChainNo As Integer = 0

        Private lastFullChain As String = "--"

        Private lastFullResNum As Integer = -999

        Private atomName As String

        Private fullResName As String

        Private lastFullResName As String = " "

        Private resName As String

        Private resNum As String

        Private hashNotes As String

        Private lastResidue As Residue = Nothing

        Dim pdb As PDBEntry
        Dim allResid As Boolean
        Dim conectOption As Integer
        Dim mode As Integer

        Friend Sub New(frame As LigPlusFrame, pdb As PDBEntry, mode As Integer, allResid As Boolean, conectOption As Integer)
            nAtoms = 0
            Me.frame = frame
            Me.pdb = pdb
            Me.allResid = allResid
            Me.conectOption = conectOption
            Me.mode = mode
        End Sub

        Public Sub write(fileName As String)
            Dim OK = True
            Dim lastAtHet = Char.MinValue
            Dim lastChain = Char.MinValue
            Dim lastDomain = 0
            Dim line = 0
            Dim hhbnnbErrors = ""
            Dim sequence = ""
            Dim open = False
            Dim prevChain = Char.MinValue
            Dim residueList As List(Of Residue) = pdb.ResidueList

            Using out = New PrintStream(fileName)
                out.Write("REMARK PDB CODE: " & pdb.PDBCode & vbLf)
                For i = 0 To residueList.Count - 1
                    Dim residue = residueList(i)
                    Dim aaCode = residue.AaCode
                    Dim isSeq = True
                    If residue.Molecule.MoleculeType <> 1 Then
                        isSeq = False
                    End If
                    If residue.Chain <> prevChain Then
                        sequence = sequence & "*" & residue.Chain.ToString() & "*"
                    End If
                    prevChain = residue.Chain
                    If residue.Wanted OrElse allResid AndAlso Not residue.FullResName.Equals("HOH") AndAlso residue.Molecule.Wanted Then
                        If isSeq Then
                            If Not open Then
                                sequence = sequence & "["
                                open = True
                            End If
                            sequence = sequence & aaCode
                        End If
                        Dim domain = residue.Domain
                        Dim atomList As List(Of Atom) = residue.AtomList
                        For k = 0 To atomList.Count - 1
                            hashNotes = ""
                            Dim atom = atomList(k)
                            If atom.Wanted Then
                                If line > 0 AndAlso (atom.AtHet <> lastAtHet OrElse residue.Chain <> lastChain) Then
                                    out.Write("TER" & vbLf)
                                End If
                                If domain <> lastDomain Then
                                    out.format("REMARK DOMAIN %d" & vbLf, New Object() {Convert.ToInt32(domain)})
                                    lastDomain = domain
                                End If
                                Dim element = atom.Element
                                If element.Length = 1 Then
                                    element = " " & element(0).ToString()
                                End If
                                If Not pdb.FromMmcif Then
                                    atomNumber = atom.AtomNumber
                                    atomName = atom.AtomName
                                    resName = residue.ResName
                                    chain = residue.Chain
                                    resNum = residue.ResNum
                                Else
                                    atomNumber = nAtoms + 1
                                    If atomNumber > 99999 Then
                                        atomNumber = 99999
                                    End If
                                    atom.AtomNumber = atomNumber
                                    atomName = formatAtomName(atom.AtomName)
                                    chain = formatChain(residue)
                                    residue.Chain = chain
                                    resName = formatResName(residue)
                                    residue.ResName = resName
                                    resNum = formatResNum(residue)
                                    residue.ResNum = resNum
                                End If
                                out.format("%s%5d %s %s %c%s   %8.3f%8.3f%8.3f%6.2f%6.2f          %s  %s" & vbLf, New Object() {
                                           atom.KeyWord, Convert.ToInt32(atomNumber), atomName, resName, Convert.ToChar(chain), resNum, Convert.ToSingle(atom.getCoord(0)), Convert.ToSingle(atom.getCoord(1)), Convert.ToSingle(atom.getCoord(2)),
                                           Convert.ToSingle(atom.Occupancy),
                                           Convert.ToSingle(atom.BValue), element, hashNotes})
                                atom.AtomNumber = atomNumber
                                atom.AtomName = atomName
                                residue.ResNum = resNum
                                residue.ResName = resName
                                Dim molecule = residue.Molecule
                                molecule.Chain = chain
                                line += 1
                                nAtoms += 1
                                lastAtHet = atom.AtHet
                                lastChain = residue.Chain
                            End If
                        Next
                    ElseIf isSeq Then
                        If open Then
                            sequence = sequence & "]"
                            open = False
                        End If
                        sequence = sequence & aaCode
                    End If
                Next
                Dim hhbnnbList As List(Of String) = pdb.HHBNNBList
                If hhbnnbList IsNot Nothing Then
                    For k = 0 To hhbnnbList.Count - 1
                        Dim hhbnnbLine = hhbnnbList(k)
                        Dim outLine = validateHHBNNB(pdb, hhbnnbLine)
                        If Not outLine.Equals("") Then
                            out.Write(outLine & vbLf)
                        Else
                            hhbnnbErrors = hhbnnbErrors & hhbnnbLine & ChrW(10).ToString()
                        End If
                    Next
                End If
                Dim conectList As List(Of Conect) = pdb.ConectList
                Dim wanted = True
                If conectOption = 2 Then
                    wanted = False
                End If
                Dim j = 0

                While j < conectList.Count AndAlso wanted
                    Dim conect = conectList(j)
                    Dim atom1 = conect.Atom1
                    Dim atom2 = conect.Atom2
                    Dim conectOK = True
                    If conectOption = 1 AndAlso atom1 IsNot Nothing AndAlso atom2 IsNot Nothing Then
                        conectOK = checkConect(atom1, atom2)
                    End If
                    If atom1 IsNot Nothing AndAlso atom2 IsNot Nothing AndAlso atom1.Residue.Wanted AndAlso atom2.Residue.Wanted AndAlso conectOK Then
                        out.format("CONECT%5d%5d" & vbLf, New Object() {Convert.ToInt32(atom1.AtomNumber), Convert.ToInt32(atom2.AtomNumber)})
                    End If

                    j += 1
                End While
                out.Write("END" & vbLf)
                If open Then
                    sequence = sequence & "]"
                    open = False
                End If
                out.format("REMARK SEQUENCE %s" & vbLf, New Object() {sequence})
                If Not hhbnnbErrors.Equals("") Then
                    Dim message = "<html>Unable to identify atoms listed in PDB file's HHB or NNB records:<P>" & vbLf & hhbnnbErrors & vbLf & "Records ignored"
                    message.debug("Warning. Format error(s) in HHB/NNB records")
                End If

            End Using
        End Sub

        Private Function formatAtomName(atomName As String) As String
            Dim len = atomName.Length
            Dim tmp = atomName
            If len = 3 Then
                tmp = " " & atomName
            ElseIf len = 1 OrElse len = 2 Then
                If Not Atom.isMetal(atomName).Equals("  ") Then
                    tmp = atomName
                Else
                    tmp = " " & atomName
                End If
            ElseIf len > 4 Then
                hashNotes += "#a" & atomName
            End If
            atomName = tmp & "    "
            tmp = atomName.Substring(0, 4)
            atomName = tmp
            Return atomName
        End Function

        Private Function formatResName(residue As Residue) As String
            fullResName = residue.FullResName
            Dim len = fullResName.Length
            Select Case len
                Case 1
                    resName = "  " & fullResName
                    Return resName
                Case 2
                    resName = " " & fullResName
                    Return resName
                Case 3
                    resName = fullResName
                    Return resName
            End Select
            If residue Is lastResidue Then
                resName = residue.ResName
            Else
                Dim i As Integer = dummyResNum / 676
                resName = letters(i).ToString() & ""
                Dim newNum = dummyResNum - i * 26 * 26
                i = newNum / 26
                resName += letters(i)
                i = newNum - i * 26
                resName += letters(i)
                dummyResNum += 1
                residue.ResName = resName
                lastResidue = residue
            End If
            hashNotes += "#r" & fullResName
            Return resName
        End Function

        Private Function formatResNum(residue As Residue) As String
            Dim resNum As String
            Dim fullResNum = residue.FullResNum
            If fullResNum < 10000 Then
                resNum = String.Format("{0,4:D} ", New Object() {Convert.ToInt32(fullResNum)})
            Else
                Dim i As Integer = fullResNum / 10000 - 1
                Dim newNum = fullResNum - 10000 * i
                Dim insCode = letters(i)
                resNum = String.Format("{0,4:D}{1}", New Object() {Convert.ToInt32(newNum), Convert.ToChar(insCode)})
                hashNotes += "#n" & fullResNum.ToString()
            End If
            Return resNum
        End Function

        Private Function formatChain(residue As Residue) As Char
            Dim chain As Char
            Dim fullChain = residue.FullChain
            Dim len = fullChain.Length
            Select Case len
                Case 1
                    chain = fullChain(0)
                    Return chain
                Case 0
                    chain = "A"c
                    Return chain
            End Select
            If fullChain.Equals(lastFullChain, StringComparison.OrdinalIgnoreCase) Then
                chain = lastChain
            ElseIf nextChainNo < 26 Then
                chain = letters(nextChainNo)
                nextChainNo += 1
                lastChain = chain
            Else
                chain = "?"c
                lastChain = "?"c
            End If
            hashNotes += "#c" & fullChain
            lastFullChain = fullChain
            Return chain
        End Function

        Private Function validateHHBNNB(pdb As PDBEntry, hhbnnbLine As String) As String
            Dim ok = True
            Dim nTokens = 0
            Dim outLine = ""
            Dim fullAtomName = ""
            Dim fullResName = ""
            Dim fullChain = ""
            Dim fullInsCode = " "
            Dim fullResNum = 0
            Dim token As Scanner = New Scanner(hhbnnbLine)
            While token.HasNext()
                Dim len As Integer
                Dim ch As Char
                Dim atom As Atom
                Dim value As String = token.Next()
                Select Case nTokens
                    Case 0
                        outLine = value & "  "
                    Case 1, 5
                        fullResName = value
                    Case 2, 6
                        fullChain = value
                        If fullChain(0) = "_"c Then
                            fullChain = " "
                        End If
                    Case 3, 7
                        len = value.Length
                        ch = value(len - 1)
                        If ch < "0"c OrElse ch > "9"c Then
                            fullInsCode = ch.ToString() & ""
                            Dim tmp = value.Substring(0, len - 1)
                            value = tmp
                        End If
                        fullResNum = tryParse(value).Value
                    Case 4, 8
                        fullAtomName = value
                        atom = pdb.findFullAtom(fullAtomName, fullResName, fullResNum, fullInsCode, fullChain)
                        If atom IsNot Nothing Then
                            Dim residue = atom.Residue
                            outLine = outLine & " " & residue.ResName & " " & residue.Chain.ToString() & " " & residue.ResNum & " " & atom.AtomName & "    "
                        Else
                            outLine = ""
                            Return outLine
                        End If
                        fullAtomName = ""
                        fullResName = ""
                        fullChain = ""
                        fullInsCode = " "
                        fullResNum = 0
                End Select
                nTokens += 1
            End While
            outLine = outLine & "0.00"
            Return outLine
        End Function

        Public Shared Function tryParse(text As String) As Integer?
            Try
                Return Convert.ToInt32(Integer.Parse(text))
            Catch __unusedFormatException1__ As FormatException
                Return Convert.ToInt32(-1)
            End Try
        End Function

        Private Function checkConect(atom1 As Atom, atom2 As Atom) As Boolean
            Dim conectOK = True
            Dim distance = 0.0F
            For i = 0 To 2
                Dim coord1 = atom1.getOriginalCoord(i)
                Dim coord2 = atom2.getOriginalCoord(i)
                distance += (coord1 - coord2) * (coord1 - coord2)
            Next
            distance = CSng(System.Math.Sqrt(distance))
            If distance > 3.0R Then
                Console.WriteLine("*** CONECT record discarded: " & atom1.ToString() & " - " & atom2.ToString() & "  Dist = " & distance.ToString())
                conectOK = False
            ElseIf distance > 2.5R Then
                Console.WriteLine("*** Warning: long CONECT record: " & atom1.ToString() & " - " & atom2.ToString() & "  Dist = " & distance.ToString())
            End If
            Return conectOK
        End Function

        Public Shared ReadOnly Property Atoms As Integer
            Get
                Return nAtoms
            End Get
        End Property
    End Class

End Namespace
