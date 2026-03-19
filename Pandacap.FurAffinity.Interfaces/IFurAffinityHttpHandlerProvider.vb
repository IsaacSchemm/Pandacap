Imports System.Net.Http

Public Interface IFurAffinityHttpHandlerProvider
    Function GetOrCreateHandler() As HttpMessageHandler
End Interface
