Imports System.Threading

Public Interface IInboxPopulator
    Function PopulateInboxAsync(cancellationToken As CancellationToken) As Task
End Interface
