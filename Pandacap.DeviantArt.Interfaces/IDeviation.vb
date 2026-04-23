Public Interface IDeviation
    ReadOnly Property DeviationId As Guid
    ReadOnly Property Title As String
    ReadOnly Property Url As String
    ReadOnly Property PublishedTime As DateTimeOffset?
    ReadOnly Property Thumbnails As IEnumerable(Of String)
End Interface
