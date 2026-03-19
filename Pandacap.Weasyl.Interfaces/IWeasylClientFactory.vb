Public Interface IWeasylClientFactory
    Function CreateWeasylClient(apiKey As String, phpProxyHost As String) As IWeasylClient
End Interface
