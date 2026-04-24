''' <summary>
''' 模型层初始化模块。
''' 等价于 Python equilibrator_cache/models/__init__.py。
''' 导出所有模型类和 SQLAlchemy Base。
''' </summary>

''' <summary>
''' 模型层的公共入口，提供所有模型类的访问。
''' 等价于 Python 中的 __all__ 导出。
''' </summary>
Public Module ModelsInit

    ''' <summary>
    ''' 获取所有模型类型的列表。
    ''' 用于反射和批量操作。
    ''' </summary>
    Public Function GetAllModelTypes() As Type()
        Return New Type() {
            GetType(Registry),
            GetType(Compound),
            GetType(CompoundIdentifier),
            GetType(CompoundMicrospecies),
            GetType(MagnesiumDissociationConstant)
        }
    End Function

End Module
