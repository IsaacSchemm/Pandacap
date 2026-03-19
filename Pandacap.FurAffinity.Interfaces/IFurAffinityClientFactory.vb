Imports Pandacap.FurAffinity.Models

Public Interface IFurAffinityClientFactory
    Function CreateClient(credentials As IFurAffinityCredentials) As IFurAffinityClient

    Function CreateClient(credentials As IFurAffinityCredentials,
                          domain As Domain) As IFurAffinityClient
End Interface
