Imports System.Threading

Public Interface IFurAffinityOnlineStatsProvider
    Function IsBotUsageOkAsync(cancellationToken As CancellationToken) As Task(Of Boolean)
End Interface
