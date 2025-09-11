Imports System.IO
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Text.Parser

Namespace ligplus

    Public Class ReadPDBFile
        Private Const MAX_FIELDS As Integer = 1000

        Public Enum LineType
            ATOM_RECORD
            CONECT_RECORD
            ENDMDL_RECORD
            HEADER_RECORD
            HETATM_RECORD
            HETNAM_RECORD
            HHBNNB_RECORD
            MODEL_RECORD
            TER_RECORD
            TITLE_RECORD
            LOOP_RECORD
            NOT_WANTED
        End Enum

        Public Enum LoopType
            LOOP_NOT_WANTED
            LOOP_AUTHOR_NAMES
            LOOP_CITATION_NAMES
            LOOP_ENTITY_DEF
            LOOP_POLYMER_TYPES
            LOOP_NONPOLYMER_TYPES
            LOOP_CHEM_COMP
            LOOP_ATOM_RECORDS
            LOOP_LIGAND_DEFS
            LOOP_DB_REFS
            LOOP_DB_ALIGN
            LOOP_SOURCE
            LOOP_CONECT
        End Enum

        Public Shared LOOP_TYPE_NAME As String() = New String() {"_audit_author.", "_citation_author.", "_entity.", "_entity_poly.", "_pdbx_entity_nonpoly.", "_chem_comp.", "_atom_site.", "_pdbx_nonpoly_scheme.", "_struct_ref.", "_struct_ref_seq.", "_entity_src_nat.", "_struct_conn."}

        Private Const NLOOP_TYPES As Integer = 12

        Private Const MAX_ENTITIES As Integer = 1000

        Private Const NENTITY_FIELDS As Integer = 10

        Public Shared LOOP_ENTITY_FIELD As String() = New String() {"_entity.id", "_entity.type", "_entity.src_method", "_entity.pdbx_description", "_entity.formula_weight", "_entity.pdbx_number_of_molecules", "_entity.pdbx_ec", "_entity.pdbx_mutation", "_entity.pdbx_fragment", "_entity.details"}

        Private Const ENTITY_ID As Integer = 0

        Private Const ENTITY_TYPE As Integer = 1

        Private Const ENTITY_SRC_METHOD As Integer = 2

        Private Const ENTITY_PDBX_DESCRIPTION As Integer = 3

        Private Const ENTITY_FORMULA_WEIGHT As Integer = 4

        Private Const ENTITY_PDBX_NUMBER_OF_MOLECULES As Integer = 5

        Private Const ENTITY_PDBX_EC As Integer = 6

        Private Const ENTITY_PDBX_MUTATION As Integer = 7

        Private Const ENTITY_PDBX_FRAGMENT As Integer = 8

        Private Const ENTITY_DETAILS As Integer = 9

        Public Shared LOOP_ATOM_FIELD As String() = New String() {"_atom_site.group_PDB", "_atom_site.id", "_atom_site.type_symbol", "_atom_site.label_atom_id", "_atom_site.label_alt_id", "_atom_site.label_comp_id", "_atom_site.label_asym_id", "_atom_site.label_entity_id", "_atom_site.label_seq_id", "_atom_site.pdbx_PDB_ins_code", "_atom_site.Cartn_x", "_atom_site.Cartn_y", "_atom_site.Cartn_z", "_atom_site.occupancy", "_atom_site.B_iso_or_equiv", "_atom_site.pdbx_formal_charge", "_atom_site.auth_seq_id", "_atom_site.auth_comp_id", "_atom_site.auth_asym_id", "_atom_site.auth_atom_id", "_atom_site.pdbx_PDB_model_num", "_atom_site.calc_flag"}

        Private Const NATOM_FIELDS As Integer = 22

        Private Const ATOM_HETATM As Integer = 0

        Private Const ATOM_NO As Integer = 1

        Private Const ATOM_ELEMENT As Integer = 2

        Private Const ATOM_NAME As Integer = 3

        Private Const ATOM_ALT_ID As Integer = 4

        Private Const ATOM_RES_NAME As Integer = 5

        Private Const ATOM_CHAIN As Integer = 6

        Private Const ATOM_ENTITY_ID As Integer = 7

        Private Const ATOM_RES_NUM As Integer = 8

        Private Const ATOM_INS_CODE As Integer = 9

        Private Const ATOM_X_COORD As Integer = 10

        Private Const ATOM_Y_COORD As Integer = 11

        Private Const ATOM_Z_COORD As Integer = 12

        Private Const ATOM_OCCUPANCY As Integer = 13

        Private Const ATOM_B_FACTOR As Integer = 14

        Private Const ATOM_CHARGE As Integer = 15

        Private Const ATOM_AUTH_RES_NUM As Integer = 16

        Private Const ATOM_AUTH_RES_NAME As Integer = 17

        Private Const ATOM_AUTH_CHAIN As Integer = 18

        Private Const ATOM_AUTH_ATOM_NAME As Integer = 19

        Private Const ATOM_MODEL_NO As Integer = 20

        Private Const ATOM_CALC_FLAG As Integer = 21

        Private Const NTEXT_FIELDS As Integer = 5

        Public Shared LOOP_TEXT_FIELD As String() = New String() {"_struct.title", "_struct_keywords.pdbx_keywords", "_pdbx_database_status.recvd_initial_deposition_date", "_struct.pdbx_descriptor", "_entity_src_gen.pdbx_gene_src_scientific_name"}

        Private Const TEXT_TITLE As Integer = 0

        Private Const TEXT_HEADER As Integer = 1

        Private Const TEXT_DATE As Integer = 2

        Private Const TEXT_STRUCTURE As Integer = 3

        Private Const TEXT_SOURCE As Integer = 4

        Private Const NHETNAM_FIELDS As Integer = 3

        Public Shared LOOP_HETNAM_FIELD As String() = New String() {"_pdbx_entity_nonpoly.entity_id", "_pdbx_entity_nonpoly.name", "_pdbx_entity_nonpoly.comp_id"}

        Private Const HETNAME_ENTITY_ID As Integer = 0

        Private Const HETNAME_NAME As Integer = 1

        Private Const HETNAME_COMP_ID As Integer = 2

        Private Const NCHEMCOMP_FIELDS As Integer = 7

        Public Shared LOOP_CHEMCOMP_FIELD As String() = New String() {"_chem_comp.id", "_chem_comp.type", "_chem_comp.mon_nstd_flag", "_chem_comp.name", "_chem_comp.pdbx_synonyms", "_chem_comp.formula", "_chem_comp.formula_weight"}

        Private Const CHEM_COMP_ID As Integer = 0

        Private Const CHEM_COMP_TYPE As Integer = 1

        Private Const CHEM_COMP_NSTD_FLAG As Integer = 2

        Private Const CHEM_COMP_NAME As Integer = 3

        Private Const CHEM_COMP_SYNONYMS As Integer = 4

        Private Const CHEM_COMP_FORMULA As Integer = 5

        Private Const CHEM_COMP_WEIGHT As Integer = 6

        Private Const NCONECT_FIELDS As Integer = 14

        Public Shared LOOP_CONECT_FIELD As String() = New String() {"_struct_conn.ptnr1_label_asym_id", "_struct_conn.ptnr1_label_comp_id", "_struct_conn.ptnr1_label_seq_id", "_struct_conn.ptnr1_label_atom_id", "_struct_conn.ptnr2_label_asym_id", "_struct_conn.ptnr2_label_comp_id", "_struct_conn.ptnr2_label_seq_id", "_struct_conn.ptnr2_label_atom_id", "_struct_conn.ptnr1_auth_asym_id", "_struct_conn.ptnr1_auth_comp_id", "_struct_conn.ptnr1_auth_seq_id", "_struct_conn.ptnr2_auth_asym_id", "_struct_conn.ptnr2_auth_comp_id", "_struct_conn.ptnr2_auth_seq_id"}

        Private Const CONN_CHAIN1 As Integer = 0

        Private Const CONN_RES_NAME1 As Integer = 1

        Private Const CONN_RES_NUM1 As Integer = 2

        Private Const CONN_ATOM_NAME1 As Integer = 3

        Private Const CONN_CHAIN2 As Integer = 4

        Private Const CONN_RES_NAME2 As Integer = 5

        Private Const CONN_RES_NUM2 As Integer = 6

        Private Const CONN_ATOM_NAME2 As Integer = 7

        Private Const CONN_AUTH_CHAIN1 As Integer = 8

        Private Const CONN_AUTH_RES_NAME1 As Integer = 9

        Private Const CONN_AUTH_RES_NUM1 As Integer = 10

        Private Const CONN_AUTH_CHAIN2 As Integer = 11

        Private Const CONN_AUTH_RES_NAME2 As Integer = 12

        Private Const CONN_AUTH_RES_NUM2 As Integer = 13

        Private Const PDB_LOOP As Integer = 0

        Private Const CIF_LOOP As Integer = 1

        Private Const NCONECT_PER_LINE As Integer = 5

        Private fromMmcif As Boolean = False

        Private alternate As Char

        Private atHet As Char

        Private chain As Char = " "c

        Private lastChain As Char = Char.MinValue

        Private fullAtomName As String

        Private fullChain As String = " "

        Private lastFullChain As String = vbNullChar

        Private entityDesc As String() = New String(999) {}

        Private textFieldString As String() = New String(4) {}

        Private atomNumber As Integer

        Private lineTypeField As Integer = -1

        Private nElements As Integer = 0

        Private elementCount As Integer() = New Integer(Atom.METAL_NAME.Length + Atom.ELEMENTField.Length - 1) {}

        Private entityID As Integer = -1

        Private fieldNo As Integer = 0

        Private fullResNum As Integer = 0

        Private lastFullResNum As Integer = -99

        Private textField As Integer = -1

        Private bValue As Single

        Private coords As Single()

        Private occupancy As Single

        Public pdb As PDBEntry

        Private lastResidue As Residue = Nothing

        Private residue As Residue = Nothing

        Private atomName As String

        Private element As String

        Private lastResName As String = Nothing

        Private lastResNum As String = Nothing

        Private resName As String

        Private resNum As String

        Private insCode As String = " "

        Private lastInsCode As String = " "

        Private fullResName As String = Nothing

        Private lastFullResName As String = "!"

        Private elementType As String() = New String(Atom.METAL_NAME.Length + Atom.ELEMENTField.Length - 1) {}

        Private conectRecordsList As List(Of Object) = Nothing

        Public Sub New(fileName As String)
            Dim gzip = False
            Dim haveCIF = True
            init()
            conectRecordsList = New List(Of Object)()
            Dim ipos = fileName.LastIndexOf("."c)
            If ipos > 0 AndAlso ipos < fileName.Length - 1 Then
                Dim fileExtension As String = fileName.Substring(ipos + 1).ToLower()
                If fileExtension.CompareTo("gz") = 0 Then
                    gzip = True
                    Console.WriteLine("Have gzipped file ...")
                End If
            End If
            Dim [loop] = 0

            While [loop] < 2 AndAlso haveCIF
                Dim inputStream As StreamReader
                'If fileName.Substring(0, 7).Equals("http://") OrElse fileName.Substring(0, 8).Equals("https://") OrElse fileName.Substring(0, 6).Equals("ftp://") Then
                '    Dim fileStream As StreamReader
                '    Console.WriteLine("HAVE URL: " & fileName)
                '    Dim url As URL = New URL(fileName)
                '    If gzip Then
                '        Dim iStream As Stream = url.openStream()
                '        Dim gzis As GZIPInputStream = New GZIPInputStream(iStream)
                '        fileStream = New StreamReader(gzis)
                '    Else
                '        fileStream = New StreamReader(url.openStream())
                '    End If
                '    inputStream = New StreamReader(fileStream)
                'Else
                '    Dim fileStream As StreamReader
                '    If gzip Then
                '        Dim fileIn As FileStream = New FileStream(fileName, FileMode.Open, FileAccess.Read)
                '        Dim gzis As GZIPInputStream = New GZIPInputStream(fileIn)
                '        fileStream = New StreamReader(gzis)
                '    Else
                '        fileStream = New StreamReader(fileName)
                '    End If
                '    inputStream = New StreamReader(fileStream)
                'End If
                If [loop] = 0 Then
                    haveCIF = getPDB(inputStream)
                Else
                    getCIF(inputStream)
                    fromMmcif = True
                    pdb.setFromMmcif()
                End If

                [loop] += 1
            End While
            pdb.fixDuplicates()
            Console.WriteLine("PDB code: " & pdb.PDBCode)
            Console.WriteLine("No. of residues read in: " & pdb.Nresidues.ToString())
            Console.WriteLine("No. of atoms read in:    " & pdb.Natoms.ToString())
        End Sub

        Private Sub init()
            coords = New Single(2) {}
            pdb = New PDBEntry()
            initElements()
        End Sub

        Private Sub initElements()
            nElements = 0
            For i = 0 To elementCount.Length - 1
                elementCount(i) = 0
                elementType(i) = "  "
            Next
        End Sub

        Private Function checkDuplicate() As Boolean
            Dim isDuplicate = False
            If residue.FullResName.Equals("UNK") Then
                Return isDuplicate
            End If
            Dim atomList As List(Of Atom) = residue.AtomList
            For i = 0 To atomList.Count - 1
                Dim atom = atomList(i)
                Dim otherAtomName = atom.AtomName
                If atomName.Equals(otherAtomName) Then
                    If alternate <> " "c OrElse occupancy > 0.0R AndAlso occupancy < 1.0R Then
                        isDuplicate = True
                        Dim occupancy2 = atom.Occupancy
                        If occupancy > occupancy2 Then
                            atom.AtomNumber = atomNumber
                            For icoord = 0 To 2
                                atom.setCoord(icoord, coords(icoord))
                            Next
                            atom.Occupancy = occupancy
                        End If
                    End If
                End If
            Next
            Return isDuplicate
        End Function

        Private Sub checkResidue()
            Dim moleculeType = 0
            If fullResNum <> lastFullResNum OrElse Not insCode.Equals(lastInsCode) OrElse Not fullResName.Equals(lastFullResName) OrElse Not fullChain.Equals(lastFullChain) Then
                If lastResidue IsNot Nothing Then
                    lastResidue.determineResidueType()
                End If
                residue = pdb.addResidue(resName, resNum, chain, fullResName, fullResNum, insCode, fullChain, moleculeType)
                If entityID > 0 AndAlso entityID < 1000 Then
                    Dim desc = entityDesc(entityID)
                    residue.EntityDesc = desc
                    If Not ReferenceEquals(desc, Nothing) AndAlso (desc.Contains("DNA") OrElse desc.Contains("RNA")) Then
                        residue.ResidueType = 3
                    End If
                End If
                lastResidue = residue
                lastResName = resName
                lastResNum = resNum
                lastInsCode = insCode
                lastChain = chain
                lastFullResName = fullResName
                lastFullResNum = fullResNum
                lastFullChain = fullChain
                initElements()
            End If
        End Sub

        Private Function fixQuoteFields(inputLine As String) As String
            Dim inQuote = False
            Dim lastSpace = True
            Dim startingQuote = ""
            Dim outputLine = ""
            Dim len = inputLine.Length
            For i = 0 To len - 1
                Dim letter As String = inputLine(i).ToString() & ""
                Dim replace = letter
                If inQuote Then
                    If letter.Equals(startingQuote) Then
                        inQuote = False
                        replace = " "
                        startingQuote = ""
                    ElseIf letter.Equals(" ") Then
                        replace = "_"
                    End If
                ElseIf lastSpace AndAlso (letter.Equals("""") OrElse letter.Equals("'")) Then
                    inQuote = True
                    startingQuote = letter
                    replace = " "
                End If
                outputLine = outputLine & replace
                If letter.Equals(" ") Then
                    lastSpace = True
                Else
                    lastSpace = False
                End If
            Next
            Return outputLine
        End Function

        Private Function getLineType(inputLine As String) As LineType
            Dim lType = LineType.NOT_WANTED
            If inputLine.Length < 6 Then
                inputLine = inputLine & "      "
            End If
            If inputLine.Substring(0, 6).Equals("HEADER") Then
                lType = LineType.HEADER_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("TITLE ") Then
                lType = LineType.TITLE_RECORD
            ElseIf inputLine.Substring(0, 4).Equals("HHB ") OrElse inputLine.Substring(0, 4).Equals("NNB ") OrElse inputLine.Substring(0, 4).Equals("-HB ") OrElse inputLine.Substring(0, 4).Equals("-NB ") Then
                lType = LineType.HHBNNB_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("ATOM  ") Then
                lType = LineType.ATOM_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("HETNAM") Then
                lType = LineType.HETNAM_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("HETATM") Then
                lType = LineType.HETATM_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("MODEL ") Then
                lType = LineType.MODEL_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("TER   ") Then
                lType = LineType.TER_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("ENDMDL") Then
                lType = LineType.ENDMDL_RECORD
            ElseIf inputLine.Substring(0, 6).Equals("CONECT") Then
                lType = LineType.CONECT_RECORD
            ElseIf inputLine.Substring(0, 5).Equals("loop_") Then
                lType = LineType.LOOP_RECORD
            End If
            Return lType
        End Function

        Private Function getAtomDetails(inputLine As String) As Boolean
            Dim ok = True
            If inputLine.Length < 54 Then
                Return False
            End If
            Dim i = 0

            While i < 3
                coords(i) = 0.0F
                i += 1
            End While
            bValue = 0.0F
            occupancy = 0.0F
            atomNumber = Integer.Parse(inputLine.Substring(6, 5).Trim())
            atomName = inputLine.Substring(12, 4)
            resName = inputLine.Substring(17, 3)
            resNum = inputLine.Substring(22, 5)
            chain = inputLine(21)
            Dim resNo = inputLine.Substring(22, 4)
            Dim insCode = inputLine(26)
            fullResName = resName.Trim()
            fullResNum = Integer.Parse(resNo.Trim())
            fullChain = chain.ToString() & ""
            alternate = inputLine(16)
            If (chain < "0"c OrElse chain > "9"c) AndAlso (chain < "A"c OrElse chain > "Z"c) AndAlso (chain < "a"c OrElse chain > "z"c) AndAlso chain <> " "c Then
                chain = "X"c
            End If
            coords(0) = Single.Parse(inputLine.Substring(30, 8).Trim())
            coords(1) = Single.Parse(inputLine.Substring(38, 8).Trim())
            coords(2) = Single.Parse(inputLine.Substring(46, 8).Trim())
            If inputLine.Length > 65 Then
                occupancy = Single.Parse(inputLine.Substring(54, 6).Trim())
                bValue = Single.Parse(inputLine.Substring(60, 6).Trim())
            End If
            If inputLine.Length > 77 Then
                If inputLine(76) = " "c Then
                    element = inputLine.Substring(77, 1)
                Else
                    element = inputLine.Substring(76, 2)
                End If
            Else
                element = "  "
            End If
            If element.Equals("  ") Then
                element = Atom.identifyElement(atomName)
            End If
            Return ok
        End Function

        Public Overridable ReadOnly Property PDBEntry As PDBEntry
            Get
                Return pdb
            End Get
        End Property

        Private Function getCIF(inputStream As StreamReader) As Integer
            Dim fIndex = New Integer(999) {}
            Dim title As String = Nothing
            Dim fieldName = New String(999) {}
            Dim token As Scanner = Nothing
            Dim brokenLine = False
            Dim inLoop = False
            Dim isUniprot = False
            Dim lastLine = ""
            Dim hetDescription As String = Nothing, hetName = hetDescription
            Dim nAtoms = 0
            Dim nConect = 0
            entityID = -1
            Dim nLoops = 0, nEntry = nLoops, nFields = nEntry
            Dim loopType As LoopType = LoopType.LOOP_NOT_WANTED
            Try
                Dim inputLine As Value(Of String) = ""
                While Not ((inputLine = inputStream.ReadLine()) Is Nothing)
                    Dim line_str As String = inputLine
                    Dim lineLen = line_str.Length
                    If lineLen > 0 AndAlso inputLine(0) = "#"c Then
                        inLoop = False
                        isUniprot = False
                        nEntry = 0
                        nFields = 0
                        loopType = loopType.LOOP_NOT_WANTED
                        Continue While
                    End If
                    If lineLen > 10 AndAlso line_str.Substring(0, 10).Equals("_entry.id ") Then
                        Dim pdbCode = line_str.Substring(10)
                        pdbCode = pdbCode.ToLower().Trim()
                        pdb.PDBCode = pdbCode
                        Continue While
                    End If
                    If lineLen > 4 AndAlso line_str.Substring(0, 5).Equals("loop_") Then
                        nLoops += 1
                        For iField = 0 To 999
                            fieldName(iField) = Nothing
                        Next
                        fieldNo = 0
                        nEntry = 0
                        nFields = 0
                        inLoop = True
                        loopType = LoopType.LOOP_NOT_WANTED
                        Continue While
                    End If
                    If inLoop = True AndAlso inputLine(0) = "_"c Then
                        If nFields < 1000 Then
                            fieldName(nFields) = line_str.Substring(0, lineLen)
                            If nFields = 0 Then
                                Dim iMatch = -1
                                For iType = 0 To 11
                                    Dim fLen = LOOP_TYPE_NAME(iType).Length
                                    If lineLen >= fLen AndAlso line_str.Substring(0, fLen).Equals(LOOP_TYPE_NAME(iType)) Then
                                        iMatch = iType
                                    End If
                                Next
                                If iMatch = 0 Then
                                    loopType = loopType.LOOP_AUTHOR_NAMES
                                ElseIf iMatch = 1 Then
                                    loopType = loopType.LOOP_CITATION_NAMES
                                ElseIf iMatch = 2 Then
                                    loopType = loopType.LOOP_ENTITY_DEF
                                ElseIf iMatch = 3 Then
                                    loopType = loopType.LOOP_POLYMER_TYPES
                                ElseIf iMatch = 4 Then
                                    loopType = loopType.LOOP_NONPOLYMER_TYPES
                                ElseIf iMatch = 5 Then
                                    loopType = loopType.LOOP_CHEM_COMP
                                ElseIf iMatch = 6 Then
                                    loopType = loopType.LOOP_ATOM_RECORDS
                                ElseIf iMatch = 7 Then
                                    loopType = loopType.LOOP_LIGAND_DEFS
                                ElseIf iMatch = 8 Then
                                    loopType = loopType.LOOP_DB_REFS
                                ElseIf iMatch = 9 Then
                                    loopType = loopType.LOOP_DB_ALIGN
                                ElseIf iMatch = 10 Then
                                    loopType = loopType.LOOP_SOURCE
                                ElseIf iMatch = 11 Then
                                    loopType = loopType.LOOP_CONECT
                                End If
                            End If
                            nFields += 1
                        Else
                            Console.WriteLine("*** Warning. CIF file contains too many fields")
                        End If
                        brokenLine = False
                        Continue While
                    End If
                    If inLoop = True AndAlso inputLine(0) <> "_"c AndAlso loopType <> loopType.LOOP_NOT_WANTED Then
                        Dim nTokens As Integer
                        Dim isDuplicate As Boolean
                        Select Case loopType
                            Case ReadPDBFile.LineType.HETNAM_RECORD
                                If nEntry = 0 Then
                                    checkFields(fieldName, nFields, fIndex, LOOP_ENTITY_FIELD, 10)
                                End If
                                inputLine = fixQuoteFields(inputLine)
                                If inputLine(0) = ";"c Then
                                    Dim len = CStr(inputLine).Length
                                    If len > 2 Then
                                        inputLine = lastLine & " " & CStr(inputLine).Substring(1, len - 1 - 1)
                                    End If
                                End If
                                lastLine = inputLine
                                token = New Scanner(inputLine)
                                nTokens = 0
                                While token.HasNext()
                                    Dim value As String = token.Next()
                                    Dim iField = -1
                                    Dim jField = 0
                                    While jField < nFields AndAlso jField < 10 AndAlso iField = -1
                                        If fIndex(jField) = nTokens Then
                                            iField = nTokens
                                        End If

                                        jField += 1
                                    End While
                                    Select Case iField
                                        Case 0
                                            entityID = tryParse(value.Trim()).Value
                                        Case 3
                                            If entityID > 0 AndAlso entityID < 1000 Then
                                                entityDesc(entityID) = value
                                            End If
                                    End Select
                                    nTokens += 1
                                End While
                            Case ReadPDBFile.LineType.CONECT_RECORD
                                If nEntry = 0 Then
                                    checkFields(fieldName, nFields, fIndex, LOOP_CHEMCOMP_FIELD, 7)
                                    hetDescription = Nothing
                                    hetName = Nothing
                                End If
                                inputLine = fixQuoteFields(inputLine)
                                token = New Scanner(inputLine)
                                nTokens = 0
                                While token.HasNext()
                                    Dim value As String = token.Next()
                                    Dim iField = -1
                                    Dim jField = 0
                                    While jField < nFields AndAlso jField < 7 AndAlso iField = -1
                                        If fIndex(jField) = nTokens Then
                                            iField = nTokens
                                            Select Case iField
                                                Case 0
                                                    hetName = value
                                                Case 3
                                                    hetDescription = value
                                                    If Not ReferenceEquals(hetName, Nothing) AndAlso Not ReferenceEquals(hetDescription, Nothing) Then
                                                        pdb.addHetGroup(hetName, hetDescription)
                                                    End If
                                            End Select
                                        End If

                                        jField += 1
                                    End While
                                    nTokens += 1
                                End While
                            Case ReadPDBFile.LineType.MODEL_RECORD
                                If nEntry = 0 Then
                                    checkFields(fieldName, nFields, fIndex, LOOP_ATOM_FIELD, 22)
                                End If
                                atHet = "A"c
                                nAtoms = getAtomDetails(inputLine, nFields, fIndex, nAtoms)
                                checkResidue()
                                isDuplicate = checkDuplicate()
                                If Not isDuplicate Then
                                    pdb.addAtom(atHet, atomNumber, atomName, coords, bValue, occupancy, residue, element, coords)
                                End If
                            Case Else
                                If nEntry = 0 Then
                                    checkFields(fieldName, nFields, fIndex, LOOP_CONECT_FIELD, 14)
                                End If
                                nConect = getConectDetails(inputLine, nFields, fIndex, nConect)
                        End Select
                        nEntry += 1
                        Continue While
                    End If
                    If lineLen > 0 AndAlso (CStr(inputLine)(0) = "_"c OrElse textField > -1) Then
                        textField = processTextField(inputLine, textField)
                        Continue While
                    End If

                    line_str = inputLine

                    If lineLen > 4 AndAlso (line_str.Substring(0, 4).Equals("HHB ") OrElse line_str.Substring(0, 4).Equals("NNB ") OrElse line_str.Substring(0, 4).Equals("-HB ") OrElse line_str.Substring(0, 4).Equals("-NB ")) Then
                        pdb.addHHBNNB(inputLine)
                    End If
                End While
                If conectRecordsList.Count > 0 Then
                    processConectRecords()
                End If

            Finally
                If lastResidue IsNot Nothing Then
                    lastResidue.determineResidueType()
                End If
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
            If Not ReferenceEquals(textFieldString(0), Nothing) Then
                title = textFieldString(0)
                pdb.FullTitle = title
            End If
            Return nAtoms
        End Function

        Private Sub checkFields(fieldName As String(), nFields As Integer, fIndex As Integer(), wantedField As String(), nWantedFields As Integer)
            For iField = 0 To nWantedFields - 1
                fIndex(iField) = -1
            Next
            For jField = 0 To nFields - 1
                Dim found = False
                Dim fLen = fieldName(jField).Length
                Dim i = 0

                While i < nWantedFields AndAlso Not found
                    Dim nLen = wantedField(i).Length
                    If nLen <= fLen AndAlso fieldName(jField).Substring(0, nLen).Equals(wantedField(i)) Then
                        fIndex(i) = jField
                        found = True
                    End If

                    i += 1
                End While
            Next
        End Sub

        Private Function getAtomDetails(inputLine As String, nFields As Integer, fIndex As Integer(), nAtoms As Integer) As Integer
            element = "  "
            entityID = -1
            atomName = "  "
            resName = "  "
            resNum = "  "
            fullResName = "  "
            fullResNum = 0
            fullChain = "  "
            Dim isMetal = False
            Dim token As Scanner = New Scanner(inputLine)
            Dim nTokens = 0
            While token.HasNext()
                Dim value As String = token.Next()
                Dim iField = -1
                Dim jField = 0

                While jField < nFields AndAlso jField < 22 AndAlso iField = -1
                    If fIndex(jField) = nTokens Then
                        Dim eLen As Integer
                        Dim Elem As String
                        iField = nTokens
                        Select Case jField
                            Case 0
                                If value.Equals("HETATM", StringComparison.OrdinalIgnoreCase) Then
                                    atHet = "H"c
                                    Exit Select
                                End If
                                atHet = "A"c
                            Case 1
                                atomNumber = tryParse(value.Trim()).Value
                            Case 2
                                eLen = value.Length
                                If eLen = 1 Then
                                    element = " " & value & "  "
                                Else
                                    element = value & "  "
                                End If
                                Elem = Atom.isMetal(element)
                                If Elem.Equals("  ") Then
                                    isMetal = False
                                    Exit Select
                                End If
                                isMetal = True
                            Case 3, 19
                                fullAtomName = value
                                atomName = formatAtomName(fullAtomName, isMetal)
                            Case 5, 17
                                fullResName = value
                            Case 7
                                entityID = tryParse(value.Trim()).Value
                            Case 8, 16
                                If Not value.Equals(".", StringComparison.OrdinalIgnoreCase) Then
                                    fullResNum = Integer.Parse(value)
                                End If
                            Case 9
                                insCode = value
                                If insCode(0) = "?"c Then
                                    insCode = " "
                                End If
                            Case 6, 18
                                fullChain = value
                            Case 10
                                coords(0) = Single.Parse(value)
                            Case 11
                                coords(1) = Single.Parse(value)
                            Case 12
                                coords(2) = Single.Parse(value)
                            Case 13
                                occupancy = Single.Parse(value)
                            Case 14
                                bValue = Single.Parse(value)
                        End Select
                    End If

                    jField += 1
                End While
                nTokens += 1
            End While
            nAtoms += 1
            Return nAtoms
        End Function

        Private Function formatAtomName(value As String, isMetal As Boolean) As String
            Dim name = value
            Dim tmpName = name
            name = removeQuotes(tmpName)
            If isMetal Then
                If element(0) = " "c Then
                    tmpName = " " & name
                Else
                    tmpName = name
                End If
            Else
                tmpName = " " & name
            End If
            Dim namLen = tmpName.Length
            If namLen = 1 Then
                name = tmpName & "   "
            ElseIf namLen = 2 Then
                name = tmpName & "  "
            ElseIf namLen = 3 Then
                name = tmpName & " "
            Else
                name = tmpName
            End If
            Return name
        End Function

        Private Function removeQuotes(value As String) As String
            Dim ch = Char.MinValue
            Dim name = value
            Dim tmpName = name
            Dim iPos = 0
            If tmpName(0) = """"c OrElse tmpName(0) = "'"c Then
                ch = tmpName(0)
            End If
            Dim len = tmpName.Length
            If tmpName(len - 1) = ch Then
                iPos = 1
                len -= 1
            End If
            If iPos > 0 Then
                name = tmpName.Substring(iPos, len - iPos)
            End If
            Return name
        End Function

        Private Function getConectDetails(inputLine As String, nFields As Integer, fIndex As Integer(), nConect As Integer) As Integer
            Dim fullChain2 = "", fullChain1 = fullChain2
            Dim fullResName2 = "", fullResName1 = fullResName2
            Dim fullAtomName2 = "", fullAtomName1 = fullAtomName2
            Dim fullResNum2 = -99, fullResNum1 = fullResNum2
            Dim token As Scanner = New Scanner(inputLine)
            Dim nTokens = 0
            While token.HasNext()
                Dim value As String = token.Next()
                Dim iField = -1
                Dim jField = 0

                While jField < nFields AndAlso jField < 14 AndAlso iField = -1
                    If fIndex(jField) = nTokens Then
                        Select Case jField
                            Case 0, 8
                                If Not value.Equals(".") Then
                                    fullChain1 = value
                                End If
                            Case 3
                                fullAtomName1 = removeAtomQuotes(value)
                            Case 1, 9
                                fullResName1 = value
                            Case 2, 10
                                fullResNum1 = tryParse(value.Trim()).Value
                            Case 4, 11
                                If Not value.Equals(".") Then
                                    fullChain2 = value
                                End If
                            Case 7
                                fullAtomName2 = removeAtomQuotes(value)
                            Case 5, 12
                                fullResName2 = value
                            Case 6, 13
                                fullResNum2 = tryParse(value.Trim()).Value
                        End Select
                    End If

                    jField += 1
                End While
                nTokens += 1
            End While
            Dim conectRecord As String = fullAtomName1 & vbTab & fullResName1 & vbTab & fullResNum1.ToString() & vbTab & fullChain1 & vbTab & fullAtomName2 & vbTab & fullResName2 & vbTab & fullResNum2.ToString() & vbTab & fullChain2
            conectRecordsList.Add(conectRecord)
            nConect += 1
            Return nConect
        End Function

        Public Shared Function tryParse(text As String) As Integer?
            Try
                Return Convert.ToInt32(Integer.Parse(text))
            Catch __unusedFormatException1__ As FormatException
                Return Convert.ToInt32(-1)
            End Try
        End Function

        Private Function getPDB(inputStream As StreamReader) As Boolean
            Dim haveCIF = False
            Dim readAtoms = True
            Dim title As String = Nothing
            Try
                Dim inputLine As Value(Of String) = Nothing
                While Not haveCIF AndAlso Not ((inputLine = inputStream.ReadLine) Is Nothing)
                    Dim line_str As String = inputLine
                    Dim lType = getLineType(inputLine)
                    atHet = "A"c
                    Select Case lType
                        Case LineType.HEADER_RECORD
                            If line_str.Length > 65 AndAlso ReferenceEquals(pdb.PDBCode, Nothing) Then
                                Dim pdbCode = line_str.Substring(62, 4)
                                pdbCode = pdbCode.ToLower()
                                pdb.PDBCode = pdbCode
                            End If
                        Case LineType.TITLE_RECORD
                            If ReferenceEquals(title, Nothing) Then
                                title = line_str.Substring(10)
                            Else
                                title = title & line_str.Substring(10)
                            End If
                            pdb.FullTitle = title
                        Case LineType.HETNAM_RECORD
                            If line_str.Length > 13 Then
                                Dim hetName = line_str.Substring(11, 3)
                                Dim hetDescription = line_str.Substring(15)
                                If inputLine(15) = " "c Then
                                    hetDescription = line_str.Substring(16)
                                End If
                                hetDescription = TextItem.stringTruncate(hetDescription)
                                If inputLine(9) = " "c Then
                                    pdb.addHetGroup(hetName, hetDescription)
                                    Continue While
                                End If
                                pdb.appendHetDescription(hetName, hetDescription)
                            End If
                        Case LineType.HETATM_RECORD
                            atHet = "H"c
                        Case LineType.ATOM_RECORD
                            If readAtoms Then
                                Dim ok = getAtomDetails(inputLine)
                                If ok Then
                                    checkResidue()
                                    Dim isDuplicate As Boolean = checkDuplicate()
                                    If Not isDuplicate Then
                                        pdb.addAtom(atHet, atomNumber, atomName, coords, bValue, occupancy, residue, element, coords)
                                    End If
                                End If
                            End If
                        Case LineType.CONECT_RECORD
                            processConect(inputLine)
                        Case LineType.MODEL_RECORD, LineType.ENDMDL_RECORD
                            If pdb.Natoms > 0 Then
                                readAtoms = False
                            End If
                        Case LineType.HHBNNB_RECORD
                            pdb.addHHBNNB(inputLine)
                        Case LineType.LOOP_RECORD
                            haveCIF = True
                            Console.WriteLine("mmCIF file detected: " & CStr(inputLine))
                        Case LineType.TER_RECORD
                            If residue IsNot Nothing Then
                                residue.setPostTER()
                            End If
                    End Select
                End While

            Finally
                If lastResidue IsNot Nothing Then
                    lastResidue.determineResidueType()
                End If
                If inputStream IsNot Nothing Then
                    inputStream.Close()
                End If
            End Try
            Return haveCIF
        End Function

        Private Sub processConect(inputLine As String)
            Dim atom1 As Atom = Nothing
            Dim atom2 As Atom = Nothing
            Dim done = False
            Dim iatom = 0
            Dim ipos = 6
            Dim jpos = ipos + 5
            Dim nAts = 0
            Dim length = inputLine.Length
            Dim atomNo = New Integer(4) {}
            While Not done
                If length >= jpos Then
                    Dim numberString = inputLine.Substring(ipos, jpos - ipos)
                    If iatom < 5 Then
                        Try
                            atomNo(iatom) = Integer.Parse(numberString.Trim())
                            iatom += 1
                        Catch __unusedFormatException1__ As FormatException
                            done = True
                        End Try
                    Else
                        done = True
                    End If
                    ipos += 5
                    jpos = ipos + 5
                    Continue While
                End If
                done = True
            End While
            nAts = iatom
            For iatom = 0 To nAts - 1
                Dim atom = pdb.getAtom(atomNo(iatom))
                If atom IsNot Nothing Then
                    If iatom = 0 Then
                        atom1 = atom
                    Else
                        atom2 = atom
                        pdb.addConect(atom1, atom2)
                    End If
                End If
            Next
        End Sub

        Private Function processTextField(inputLine As String, textField As Integer) As Integer
            Dim token As Scanner = New Scanner(inputLine)
            Dim nTokens = 0
            Dim token1 = ""
            Dim token2 = ""
            While token.HasNext()
                Dim value As String = token.Next()
                If nTokens = 0 AndAlso textField = -1 Then
                    token1 = value
                ElseIf token2.Equals("") Then
                    token2 = value
                Else
                    token2 = token2 & " " & value
                End If
                nTokens += 1
            End While
            If nTokens = 0 Then
                Return -1
            End If
            If textField > -1 Then
                token2 = removeQuotes(token2)
                textFieldString(textField) = token2
                Return -1
            End If
            textField = -1
            Dim tLen = token1.Length
            Dim iField = 0

            While iField < 5 AndAlso textField = -1
                Dim fLen = LOOP_TEXT_FIELD(iField).Length
                If tLen = fLen AndAlso token1.Equals(LOOP_TEXT_FIELD(iField)) Then
                    textField = iField
                End If

                iField += 1
            End While
            If textField > -1 AndAlso nTokens > 1 Then
                token2 = removeQuotes(token2)
                textFieldString(textField) = token2
                textField = -1
            End If
            Return textField
        End Function

        Private Function removeAtomQuotes(text As String) As String
            Dim len = text.Length
            If len > 2 Then
                If text(0) = "'"c AndAlso text(len - 1) = "'"c OrElse text(0) = """"c AndAlso text(len - 1) = """"c Then
                    Dim tmp = text.Substring(1, len - 1 - 1)
                    text = tmp
                End If
            End If
            Return text
        End Function

        Private Sub processConectRecords()
            Dim token As Scanner = Nothing
            Dim fullChain2 = "", fullChain1 = fullChain2
            Dim fullResName2 = "", fullResName1 = fullResName2
            Dim fullAtomName2 = "", fullAtomName1 = fullAtomName2
            Dim fullResNum2 = -99, fullResNum1 = fullResNum2
            Dim atomList = pdb.AtomList
            For i = 0 To conectRecordsList.Count - 1
                Dim conectRecord As String = conectRecordsList(i)
                token = New Scanner(conectRecord)
                token.UseDelimiter(vbTab)
                Dim nTokens = 0
                While token.HasNext()
                    Dim value As String = token.Next()
                    Select Case nTokens
                        Case 0
                            fullAtomName1 = value
                        Case 1
                            fullResName1 = value
                        Case 2
                            fullResNum1 = tryParse(value.Trim()).Value
                        Case 3
                            fullChain1 = value
                        Case 4
                            fullAtomName2 = value
                        Case 5
                            fullResName2 = value
                        Case 6
                            fullResNum2 = tryParse(value.Trim()).Value
                        Case 7
                            fullChain2 = value
                    End Select
                    nTokens += 1
                End While
                Dim atom1 = identifyAtom(atomList, fullAtomName1, fullResName1, fullResNum1, fullChain1)
                Dim atom2 = identifyAtom(atomList, fullAtomName2, fullResName2, fullResNum2, fullChain2)
                If atom1 IsNot Nothing AndAlso atom2 IsNot Nothing Then
                    pdb.addConect(atom1, atom2)
                End If
            Next
        End Sub

        Private Function identifyAtom(atomList As List(Of Atom), fullAtomName As String, fullResName As String, fullResNum As Integer, fullChain As String) As Atom
            Dim foundAtom As Atom = Nothing
            Dim i = 0

            While i < atomList.Count AndAlso foundAtom Is Nothing
                Dim atom = atomList(i)
                Dim residue = atom.Residue
                If fullChain.Equals(residue.FullChain) AndAlso fullResName.Equals(residue.FullResName) AndAlso fullResNum = residue.FullResNum AndAlso fullAtomName.Equals(atom.AtomName.Trim()) Then
                    foundAtom = atom
                End If

                i += 1
            End While
            Return foundAtom
        End Function
    End Class

End Namespace
