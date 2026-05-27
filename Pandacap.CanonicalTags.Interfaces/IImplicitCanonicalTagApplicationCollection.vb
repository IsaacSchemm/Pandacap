Imports Microsoft.FSharp.Collections

Public Interface IImplicitCanonicalTagApplicationCollection
    ReadOnly Property Characters As FSharpSet(Of Guid)
    ReadOnly Property Species As FSharpSet(Of Guid)
    ReadOnly Property Settings As FSharpSet(Of Guid)
    ReadOnly Property Mediums As FSharpSet(Of Guid)
End Interface
