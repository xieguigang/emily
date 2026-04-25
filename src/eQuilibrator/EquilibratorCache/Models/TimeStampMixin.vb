Namespace Cache

    ''' <summary>
    ''' 时间戳混入模块。
    ''' 等价于 Python equilibrator_cache/models/mixins.py。
    ''' 定义了创建时间和更新时间列，可混入到其他表模型中。
    ''' </summary>
    Public Class TimeStampMixin

        ''' <summary>
        ''' 创建时间。默认值为当前 UTC 时间。
        ''' 等价于 Python created_on = Column(DateTime(timezone=True), nullable=False, default=timezone_aware_now)。
        ''' </summary>
        Public Property CreatedOn As DateTime = TimeStampMixin.TimezoneAwareNow()

        ''' <summary>
        ''' 更新时间。每次更新时自动填充。
        ''' 等价于 Python updated_on = Column(DateTime(timezone=True), nullable=True, onupdate=timezone_aware_now)。
        ''' </summary>
        Public Property UpdatedOn As DateTime?

        ''' <summary>
        ''' 返回当前 UTC 时间。
        ''' 等价于 Python timezone_aware_now()。
        ''' </summary>
        Public Shared Function TimezoneAwareNow() As DateTime
            Return DateTime.UtcNow
        End Function

    End Class
End Namespace