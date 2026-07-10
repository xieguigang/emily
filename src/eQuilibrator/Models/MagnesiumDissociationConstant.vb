Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' 镁离子解离常数模型。
    ''' 等价于 Python equilibrator_cache/models/magnesium_dissociation_constant.py。
    ''' 表示化合物特定伪异构体的 Mg2+ 解离常数。
    ''' </summary>
    Public Class MagnesiumDissociationConstant
        Inherits TimeStampMixin

        ''' <summary>主键 ID</summary>
        Public Property Id As Integer

        ''' <summary>所属化合物的 ID（外键）</summary>
        Public Property CompoundId As Integer

        ''' <summary>质子数。</summary>
        Public Property NumberProtons As Integer = 0

        ''' <summary>镁离子数。</summary>
        Public Property NumberMagnesiums As Integer = 0

        ''' <summary>解离常数。</summary>
        Public Property DissociationConstant As Double

        Public Overrides Function ToString() As String
            Return $"MagnesiumDissociationConstant(compound_id={CompoundId}, " &
                   $"number_protons={NumberProtons}, number_magnesiums={NumberMagnesiums})"
        End Function
    End Class
End Namespace
