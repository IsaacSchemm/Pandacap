Imports System.Threading

Public Interface IFurAffinityOnlineStatsProvider
    Function IsBotUsageOkAsync(Optional cancellationToken As CancellationToken = Nothing) As Task(Of Boolean)
End Interface
