namespace Pandacap.FurAffinity

open Pandacap.FurAffinity.Models
open Pandacap.FurAffinity.Interfaces

type internal FurAffinityClientFactory(handlerProvider: FurAffinityHttpHandlerProvider) =
    interface IFurAffinityClientFactory with
        member _.CreateClient(credentials) =
            new FurAffinityClient(
                handlerProvider.GetOrCreateHandler(),
                WWW,
                credentials)

        member _.CreateClient(credentials, domain) =
            new FurAffinityClient(
                handlerProvider.GetOrCreateHandler(),
                domain,
                credentials)
