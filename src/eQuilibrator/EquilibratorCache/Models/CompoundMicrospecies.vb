''' <summary>
''' 化合物微物种模型。
''' 等价于 Python equilibrator_cache/models/compound_microspecies.py。
''' 表示化合物在不同质子化和镁离子结合状态下的微物种，
''' 是热力学计算的核心数据模型。
''' </summary>
Public Class CompoundMicrospecies

    ' =========================================================================
    ' 数据库列映射
    ' =========================================================================

    ''' <summary>主键 ID</summary>
    Public Property Id As Integer

    ''' <summary>所属化合物的 ID（外键）</summary>
    Public Property CompoundId As Integer

    ''' <summary>
    ''' 微物种相对于最稳定物种的标准 Gibbs 自由能差（单位 RT）。
    ''' 等价于 Python ddg_over_rt = Column(Float)。
    ''' </summary>
    Public Property DdgOverRt As Double

    ''' <summary>
    ''' 微物种的电荷数。
    ''' </summary>
    Public Property Charge As Integer

    ''' <summary>
    ''' 质子数。
    ''' </summary>
    Public Property NumberProtons As Integer

    ''' <summary>
    ''' 镁离子数。
    ''' </summary>
    Public Property NumberMagnesiums As Integer

    ' =========================================================================
    ' 方法
    ' =========================================================================

    ''' <summary>
    ''' 计算此微物种在指定条件下的变换相对 Gibbs 自由能。
    ''' 等价于 Python transformed_ddg_over_rt(self, pH, pMg, ionic_strength_M, T_in_K)。
    ''' 
    ''' 该方法首先将 ddg_over_rt 从标准温度校正到目标温度，
    ''' 然后应用 Legendre 变换来考虑 pH、pMg 和离子强度的影响。
    ''' </summary>
    ''' <param name="pH">pH 值</param>
    ''' <param name="pMg">pMg 值</param>
    ''' <param name="ionic_strength_M">离子强度（摩尔/升）</param>
    ''' <param name="T_in_K">温度（开尔文）</param>
    ''' <returns>变换后的相对 Gibbs 自由能（单位 RT）</returns>
    Public Function TransformedDdgOverRt(pH As Double, pMg As Double,
                                          ionic_strength_M As Double,
                                          T_in_K As Double) As Double
        ' 当计算 ddg_over_rt 时，是在标准温度下进行的。
        ' 如果要在其他温度下计算变换，需要将常数从 RT 单位转换为 kJ/mol 单位，
        ' 然后除以新温度下的 RT。因此需要乘以温度差异补偿因子。
        Dim ddg_over_rt As Double = Me.DdgOverRt * ThermodynamicConstants.default_T_in_K / T_in_K

        Return ddg_over_rt + ThermodynamicConstants.LegendreTransform(
            pH:=pH,
            pMg:=pMg,
            ionic_strength_M:=ionic_strength_M,
            T_in_K:=T_in_K,
            charge:=Me.Charge,
            num_protons:=Me.NumberProtons,
            num_magnesiums:=Me.NumberMagnesiums)
    End Function

    ''' <summary>
    ''' 返回对象的字符串表示。
    ''' </summary>
    Public Overrides Function ToString() As String
        Return $"CompoundMicrospecies(id={Id}, charge={Charge}, " &
               $"protons={NumberProtons}, magnesiums={NumberMagnesiums})"
    End Function

End Class
