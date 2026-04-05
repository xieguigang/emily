Namespace EquilibratorThermodynamics

    ' ========================================================================
    ' 基团贡献参数
    ' ========================================================================

    ''' <summary>
    ''' 基团贡献参数
    ''' 存储每个基团对标准生成能的贡献值
    ''' </summary>
    Public Class GroupContributionParameters

        ''' <summary>
        ''' 基团ID到标准生成能贡献的映射
        ''' 单位: kJ/mol
        ''' </summary>
        Public Property GroupEnergies As Dictionary(Of Integer, Double)

        ''' <summary>
        ''' 基团ID到基团名称的映射
        ''' </summary>
        Public Property GroupNames As Dictionary(Of Integer, String)

        ''' <summary>
        ''' 标准温度 (K)
        ''' </summary>
        Public Property StandardTemperature As Double = 298.15

        ''' <summary>
        ''' 气体常数 R (kJ/(mol·K))
        ''' </summary>
        Public ReadOnly Property GasConstant As Double = 0.008314

        ''' <summary>
        ''' RT 在标准温度下的值 (kJ/mol)
        ''' </summary>
        Public ReadOnly Property RT As Double
            Get
                Return GasConstant * StandardTemperature
            End Get
        End Property

        Public Sub New()
            GroupEnergies = New Dictionary(Of Integer, Double)()
            GroupNames = New Dictionary(Of Integer, String)()
            InitializeDefaultParameters()
        End Sub

        ''' <summary>
        ''' 初始化默认的基团贡献参数
        ''' 这些参数来自Alberty和eQuilibrator的研究
        ''' </summary>
        Private Sub InitializeDefaultParameters()
            ' 这里存储的是简化的示例参数
            ' 实际应用中需要从训练数据中获取完整的参数集

            ' 常见基团的贡献值 (kJ/mol)
            ' 这些值是基于Alberty的热力学数据

            ' 碳氢基团
            GroupEnergies(1) = -17.9   ' -CH3
            GroupEnergies(2) = 8.6     ' -CH2-
            GroupEnergies(3) = 26.0    ' >CH-
            GroupEnergies(4) = 40.0    ' >C<

            ' 含氧基团
            GroupEnergies(5) = -181.0  ' -OH (醇)
            GroupEnergies(6) = -361.0  ' -COOH (羧酸)
            GroupEnergies(7) = -276.0  ' -CHO (醛)
            GroupEnergies(8) = -289.0  ' >C=O (酮)
            GroupEnergies(9) = -235.0  ' -O- (醚)

            ' 含氮基团
            GroupEnergies(10) = 62.0   ' -NH2
            GroupEnergies(11) = 50.0   ' >NH
            GroupEnergies(12) = 27.0   ' >N-
            GroupEnergies(13) = 95.0   ' -CN

            ' 含硫基团
            GroupEnergies(14) = -42.0  ' -SH
            GroupEnergies(15) = -30.0  ' -S-

            ' 磷酸基团
            GroupEnergies(16) = -1025.0 ' -OPO3(2-)
            GroupEnergies(17) = -980.0  ' -OPO3H-
            GroupEnergies(18) = -935.0  ' -OPO3H2

            ' 环结构修正
            GroupEnergies(20) = 15.0   ' 五元环
            GroupEnergies(21) = 10.0   ' 六元环
            GroupEnergies(22) = 25.0   ' 芳香环

            ' 共轭系统修正
            GroupEnergies(30) = -15.0  ' 共轭双键

            ' 离子化修正
            GroupEnergies(40) = -20.0  ' 羧酸解离 (-COO-)
            GroupEnergies(41) = 30.0   ' 胺质子化 (-NH3+)

            ' 基团名称
            GroupNames(1) = "-CH3"
            GroupNames(2) = "-CH2-"
            GroupNames(3) = ">CH-"
            GroupNames(4) = ">C<"
            GroupNames(5) = "-OH"
            GroupNames(6) = "-COOH"
            GroupNames(7) = "-CHO"
            GroupNames(8) = ">C=O"
            GroupNames(9) = "-O-"
            GroupNames(10) = "-NH2"
            GroupNames(11) = ">NH"
            GroupNames(12) = ">N-"
            GroupNames(13) = "-CN"
            GroupNames(14) = "-SH"
            GroupNames(15) = "-S-"
            GroupNames(16) = "-OPO3(2-)"
            GroupNames(17) = "-OPO3H-"
            GroupNames(18) = "-OPO3H2"
            GroupNames(20) = "五元环"
            GroupNames(21) = "六元环"
            GroupNames(22) = "芳香环"
            GroupNames(30) = "共轭双键"
            GroupNames(40) = "-COO-"
            GroupNames(41) = "-NH3+"
        End Sub

        ''' <summary>
        ''' 获取基团的贡献值
        ''' </summary>
        Public Function GetGroupEnergy(groupId As Integer) As Double
            If GroupEnergies.ContainsKey(groupId) Then
                Return GroupEnergies(groupId)
            End If
            Return 0.0
        End Function

        ''' <summary>
        ''' 设置基团的贡献值
        ''' </summary>
        Public Sub SetGroupEnergy(groupId As Integer, energy As Double, Optional name As String = Nothing)
            GroupEnergies(groupId) = energy
            If name IsNot Nothing Then
                GroupNames(groupId) = name
            End If
        End Sub
    End Class

End Namespace