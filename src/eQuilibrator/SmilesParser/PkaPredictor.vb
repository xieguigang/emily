' ============================================================================
' PkaPredictor.vb - 基于官能团的 pKa 预测器
' ============================================================================
' 功能：
'   将 FunctionalGroupDetector 识别出的可解离基团映射为经验 pKa，并标注酸/碱性。
'   对酚羟基、杂环氮等做环上下文修正（酚 vs 醇、吡啶型 N vs 吡咯型 N-H）。
'
' 说明：
'   * 经验 pKa 为占位/近似常数（无水相取代基效应修正），仅用于演示算法流程
'     与 pH 依赖形状，不能对标 equilibrator.org 定量值。
'   * 脂肪醇 (pKa~16) 与吡咯型 N-H (pKa~17) 在水相中基本不离解，标记为
'     IsSignificant=False，供展示但不参与微物种构造。
' ============================================================================

Namespace SmilesChem

    ''' <summary>
    ''' 单条 pKa 预测结果。
    ''' </summary>
    Public Class PkaPrediction

        ''' <summary>预测的 pKa 值</summary>
        Public Property Pka As Double

        ''' <summary>
        ''' 是否为酸性解离位点（HA ⇌ A⁻ + H⁺，去质子使电荷 -1、质子数 -1）；
        ''' False 表示碱性位点（B + H⁺ ⇌ BH⁺，加质子使电荷 +1、质子数 +1）。
        ''' </summary>
        Public Property IsAcidic As Boolean

        ''' <summary>来源基团符号（如 -COOH）</summary>
        Public Property GroupNotation As String

        ''' <summary>说明（用于调试/展示）</summary>
        Public Property Description As String

        ''' <summary>水相中是否真正发生解离（醇/吡咯 N-H 为 False）</summary>
        Public Property IsSignificant As Boolean = True
    End Class

    ''' <summary>
    ''' 基于官能团识别的 pKa 预测器。
    ''' 将已识别的功能团列表映射为一组经验 pKa；对环上下文（酚/醇、吡啶/吡咯）做修正。
    ''' </summary>
    Public Class PkaPredictor

        ''' <summary>
        ''' 对分子图 + 已识别基团列表预测可解离位点的 pKa。
        ''' 返回空列表表示该分子无可预测的解离位点（如烷烃、苯）。
        ''' </summary>
        ''' <param name="mol">已解析的分子图（用于环上下文判断）</param>
        ''' <param name="groups">FunctionalGroupDetector 识别出的基团/片段</param>
        Public Shared Function Predict(mol As SmilesMolecule, groups As List(Of FunctionalGroup)) As List(Of PkaPrediction)
            Dim result As New List(Of PkaPrediction)()
            If mol Is Nothing OrElse groups Is Nothing Then Return result

            For Each g In groups
                Select Case g.Notation
                    Case "-COOH"
                        result.Add(New PkaPrediction With {
                            .Pka = 4.76, .IsAcidic = True, .GroupNotation = g.Notation,
                            .Description = "羧基 (aliphatic carboxyl)"})

                    Case "-SO3H"
                        result.Add(New PkaPrediction With {
                            .Pka = -1.5, .IsAcidic = True, .GroupNotation = g.Notation,
                            .Description = "磺酸基 (sulfonic acid, 强酸)"})

                    Case "-SH"
                        result.Add(New PkaPrediction With {
                            .Pka = 10.3, .IsAcidic = True, .GroupNotation = g.Notation,
                            .Description = "巯基 (thiol)"})

                    Case "-OH"
                        ' 环上下文：连接碳为芳香碳 → 酚 (pKa~10)，否则脂肪醇 (pKa~16，水相可忽略)
                        Dim oAtom = g.AtomIndices(0)
                        Dim attachedCarbon = mol.GetNeighborsWhere(oAtom, Function(a) a.Symbol = "C")
                        Dim isPhenol = False
                        If attachedCarbon.Count > 0 AndAlso attachedCarbon(0) >= 0 AndAlso attachedCarbon(0) < mol.Atoms.Count Then
                            isPhenol = mol.Atoms(attachedCarbon(0)).IsAromatic
                        End If
                        If isPhenol Then
                            result.Add(New PkaPrediction With {
                                .Pka = 10.0, .IsAcidic = True, .GroupNotation = g.Notation,
                                .Description = "酚羟基 (phenol)"})
                        Else
                            ' 脂肪醇：解离极弱，水相中近似不离解，仅作展示
                            result.Add(New PkaPrediction With {
                                .Pka = 16.0, .IsAcidic = True, .GroupNotation = g.Notation,
                                .IsSignificant = False,
                                .Description = "醇羟基 (alcohol, ~non-dissociating in water)"})
                        End If

                    Case "-NH2"
                        result.Add(New PkaPrediction With {
                            .Pka = 9.2, .IsAcidic = False, .GroupNotation = g.Notation,
                            .Description = "伯胺 (primary amine)"})

                    Case "-NH-"
                        result.Add(New PkaPrediction With {
                            .Pka = 10.6, .IsAcidic = False, .GroupNotation = g.Notation,
                            .Description = "仲胺 (secondary amine)"})

                    Case ">N-"
                        result.Add(New PkaPrediction With {
                            .Pka = 9.8, .IsAcidic = False, .GroupNotation = g.Notation,
                            .Description = "叔胺 (tertiary amine)"})

                    Case Else
                        ' 含氮芳香杂环片段：逐原子判断每个 N 是吡啶型（碱性）还是吡咯型（弱酸性）
                        If IsNitrogenHeterocycle(g.Notation) Then
                            For Each idx In g.AtomIndices
                                If idx >= 0 AndAlso idx < mol.Atoms.Count AndAlso mol.Atoms(idx).Symbol = "N" Then
                                    Dim hOnN = mol.GetHCount(idx)
                                    If hOnN >= 1 Then
                                        result.Add(New PkaPrediction With {
                                            .Pka = 17.0, .IsAcidic = True, .GroupNotation = g.Notation,
                                            .IsSignificant = False,
                                            .Description = "吡咯型 N-H (weakly acidic, ignored)"})
                                    Else
                                        result.Add(New PkaPrediction With {
                                            .Pka = 5.2, .IsAcidic = False, .GroupNotation = g.Notation,
                                            .Description = "吡啶型氮 (pyridine N)"})
                                    End If
                                End If
                            Next
                        End If
                End Select
            Next

            Return result
        End Function

        ''' <summary>判断基团符号是否为含氮芳香杂环片段。</summary>
        Private Shared Function IsNitrogenHeterocycle(notation As String) As Boolean
            Return notation = "-C5H4N" OrElse notation.StartsWith("ArHet") OrElse notation.StartsWith("Het")
        End Function

    End Class

End Namespace
