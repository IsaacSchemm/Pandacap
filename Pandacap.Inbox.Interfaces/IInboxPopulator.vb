Imports System.Threading

Public Interface IInboxPopulator
    Function PopulateInboxAsync(Optional cancellationToken As CancellationToken = Nothing) As Task
End Interface
