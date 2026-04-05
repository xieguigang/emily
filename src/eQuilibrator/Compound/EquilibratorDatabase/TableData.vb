Namespace EquilibratorThermodynamics
    ' ========================================================================
    ' 数据模型类 - 对应数据库表结构
    ' ========================================================================

    ''' <summary>
    ''' 化合物主表 - 对应 compounds 表
    ''' </summary>
    Public Class Compound
        Public Property CreatedOn As DateTime?
        Public Property UpdatedOn As DateTime?
        Public Property Id As Integer
        Public Property InChIKey As String
        Public Property InChI As String
        Public Property Smiles As String
        Public Property Mass As Double
        Public Property AtomBag As Dictionary(Of String, Integer)
        Public Property AtomBagRaw As Byte()
        Public Property DissociationConstants As List(Of Double)
        Public Property DissociationConstantsRaw As Byte()
        Public Property GroupVector As Double()
        Public Property GroupVectorRaw As Byte()

        ' 导航属性
        Public Property Microspecies As List(Of CompoundMicrospecies)
        Public Property Identifiers As List(Of CompoundIdentifier)
        Public Property MagnesiumDissociationConstants As List(Of MagnesiumDissociationConstant)
    End Class

    ''' <summary>
    ''' 化合物微物种表 - 对应 compound_microspecies 表
    ''' 表示化合物在不同质子化状态下的物种
    ''' </summary>
    Public Class CompoundMicrospecies
        Public Property CreatedOn As DateTime?
        Public Property UpdatedOn As DateTime?
        Public Property Id As Integer
        Public Property CompoundId As Integer
        Public Property Charge As Integer
        Public Property NumberProtons As Integer
        Public Property NumberMagnesiums As Integer
        Public Property IsMajor As Boolean
        Public Property DdGOverRt As Double?

        ' 导航属性
        Public Property Compound As Compound
    End Class

    ''' <summary>
    ''' 化合物标识符表 - 对应 compound_identifiers 表
    ''' </summary>
    Public Class CompoundIdentifier
        Public Property CreatedOn As DateTime?
        Public Property UpdatedOn As DateTime?
        Public Property Id As Integer
        Public Property CompoundId As Integer
        Public Property RegistryId As Integer
        Public Property Accession As String

        ' 导航属性
        Public Property Compound As Compound
        Public Property Registry As Registry
    End Class

    ''' <summary>
    ''' 镁解离常数表 - 对应 magnesium_dissociation_constant 表
    ''' </summary>
    Public Class MagnesiumDissociationConstant
        Public Property CreatedOn As DateTime?
        Public Property UpdatedOn As DateTime?
        Public Property Id As Integer
        Public Property CompoundId As Integer
        Public Property NumberProtons As Integer
        Public Property NumberMagnesiums As Integer
        Public Property DissociationConstant As Double

        ' 导航属性
        Public Property Compound As Compound
    End Class

    ''' <summary>
    ''' 注册表表 - 对应 registries 表
    ''' </summary>
    Public Class Registry
        Public Property CreatedOn As DateTime?
        Public Property UpdatedOn As DateTime?
        Public Property Id As Integer
        Public Property Name As String
        Public Property [Namespace] As String
        Public Property Pattern As String
        Public Property Identifier As String
        Public Property Url As String
        Public Property IsPrefixed As Boolean
        Public Property AccessUrl As String
    End Class

End Namespace