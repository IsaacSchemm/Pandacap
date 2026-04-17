Imports System.Net.Http

Public Interface IWeasylHttpHandlerProvider
    Function GetOrCreateHandler() As HttpMessageHandler
End Interface
