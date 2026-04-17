Imports System.Threading
Imports Microsoft.FSharp.Collections

Public Interface IDeliveryInboxCollector
    Function GetDeliveryInboxesAsync(Optional isCreate As Boolean = False,
                                     Optional cancellationToken As CancellationToken = Nothing) As Task(Of FSharpList(Of String))
End Interface
