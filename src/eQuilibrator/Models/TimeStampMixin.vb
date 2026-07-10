Namespace EquilibratorApi.Core.Models

    ''' <summary>
    ''' 时间戳混入模块。
    ''' 等价于 Python equilibrator_cache/models/mixins.py，定义创建/更新时间列。
    ''' </summary>
    Public Class TimeStampMixin

        ''' <summary>创建时间，默认当前 UTC 时间。</summary>
        Public Property CreatedOn As DateTime = TimeStampMixin.TimezoneAwareNow()

        ''' <summary>更新时间。</summary>
        Public Property UpdatedOn As DateTime?

        ''' <summary>返回当前 UTC 时间。</summary>
        Public Shared Function TimezoneAwareNow() As DateTime
            Return DateTime.UtcNow
        End Function
    End Class
End Namespace
