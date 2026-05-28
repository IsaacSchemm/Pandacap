Public Interface ICanonicalTagTreeDisplayNode
    ReadOnly Property Id As Guid?
    ReadOnly Property Name As String
    ReadOnly Property Type As CanonicalTagType
    ReadOnly Property Children As IEnumerable(Of ICanonicalTagTreeDisplayNode)
End Interface
