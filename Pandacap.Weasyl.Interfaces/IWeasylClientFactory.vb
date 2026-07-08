Public Interface IWeasylClientFactory
    Function CreateWeasylClient(weasylCredentials As IWeasylCredentials) As IWeasylClient
End Interface
