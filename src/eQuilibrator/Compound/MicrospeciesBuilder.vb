' ============================================================================
' MicrospeciesBuilder.vb - 由 pKa 预测构造微物种列表
' ============================================================================
' 功能：
'   将 PkaPredictor 输出的可解离位点（仅 IsSignificant）转换为一组微物种
'   (CompoundMicrospecies)，供 StandardFormationEnergyTransformed 的 Legendre
'   分区函数使用。
'
' 约定（与 Alberty / eQuilibrator 一致）：
'   以 SMILES 绘制的（中性）结构为参考态，其 ddg_over_rt = 0。
'     - 酸性位点去质子：电荷 -1、质子数 -1，本征能量 +RT·ln10·pKa
'     - 碱性位点加质子：电荷 +1、质子数 +1，本征能量 -RT·ln10·pKa
'   逐级质子化采用“宏观 pKa 阶梯”：pKa 升序排列，第 j 步质子化（k=j-1→k=j）
'   使用第 (n-j+1) 大的 pKa，从而保证从全去质子态逐级加质子时，先加的是
'   最强碱/最弱酸位点（与真实宏观解离顺序一致）。
' ============================================================================

Imports System.Collections.Generic
Imports System.Linq
Imports eQuilibrator.EquilibratorApi.Core.Models
Imports eQuilibrator.SmilesChem

Namespace EquilibratorThermodynamics

    ''' <summary>
    ''' 由 pKa 预测结果构造微物种列表（组件贡献法 / Legendre 分区函数用）。
    ''' </summary>
    Public Class MicrospeciesBuilder

        ''' <summary>
        ''' 构造微物种列表。
        ''' </summary>
        ''' <param name="predictions">PkaPredictor 的预测结果（仅 IsSignificant 的会被采用）</param>
        ''' <param name="frameworkCharge">SMILES 显式形式电荷之和</param>
        ''' <param name="totalProtonCount">分子总质子数（含隐式氢）</param>
        ''' <param name="compoundId">回填到微物种的化合物 ID（可选，仅用于关联）</param>
        ''' <returns>微物种列表；若无显著解离位点则返回空列表（调用方应回退组贡献法）</returns>
        Public Shared Function Build(predictions As List(Of PkaPrediction),
                                     frameworkCharge As Integer,
                                     totalProtonCount As Integer,
                                     Optional compoundId As Integer = -1) As List(Of CompoundMicrospecies)

            ' 仅采用水相中真正解离的位点
            Dim sites = predictions.Where(Function(p) p.IsSignificant).ToList()
            Dim acidicCount = sites.FindAll(Function(p) p.IsAcidic).Count
            Dim n = sites.Count

            Dim microspecies As New List(Of CompoundMicrospecies)()
            If n = 0 Then Return microspecies

            ' pKa 升序排列，构造“逐级质子化”阶梯：stepPka(j) 为第 j 步（k=j-1→k=j）的 pKa
            Dim pkaAsc = sites.Select(Function(p) p.Pka).OrderBy(Function(v) v).ToList()
            Dim stepPka(n) As Double          ' 1-indexed
            For j As Integer = 1 To n
                stepPka(j) = pkaAsc(n - j)    ' 反向取用，使宏观阶梯顺序正确
            Next

            ' 参考态 = 绘制结构：所有酸性位质子化、所有碱性位未质子化 → k* = acidicCount
            Dim kStar = acidicCount
            Dim LOG10 = Math.Log(10.0)

            For k As Integer = 0 To n
                ' ddg(k) = LOG10 · ( Σ_{j=k+1}^{kStar} stepPka(j)  −  Σ_{j=kStar+1}^{k} stepPka(j) )，单位 RT
                Dim ddg As Double = 0.0
                If k < kStar Then
                    For j As Integer = k + 1 To kStar
                        ddg += stepPka(j)
                    Next
                ElseIf k > kStar Then
                    For j As Integer = kStar + 1 To k
                        ddg -= stepPka(j)
                    Next
                End If
                ddg *= LOG10

                Dim ms As New CompoundMicrospecies() With {
                    .Id = k + 1,
                    .CompoundId = compoundId,
                    .DdgOverRt = ddg,
                    .Charge = (frameworkCharge - acidicCount) + k,
                    .NumberProtons = (totalProtonCount - acidicCount) + k,
                    .NumberMagnesiums = 0,
                    .IsMajor = False
                }
                microspecies.Add(ms)
            Next

            Return microspecies
        End Function

    End Class

End Namespace
