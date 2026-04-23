Public Interface IMessage
    ReadOnly Property Type As String
    ReadOnly Property From As IAuthor
    ReadOnly Property Deviation As IDeviation
    ReadOnly Property Timestamp As DateTimeOffset?
End Interface
