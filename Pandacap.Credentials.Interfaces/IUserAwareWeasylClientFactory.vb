Imports System.Threading
Imports Pandacap.Weasyl.Interfaces

Public Interface IUserAwareWeasylClientFactory
    Function CreateWeasylClientAsync(cancellationToken As CancellationToken) As Task(Of IWeasylClient)
End Interface
