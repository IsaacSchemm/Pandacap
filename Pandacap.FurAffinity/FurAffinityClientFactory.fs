namespace Pandacap.FurAffinity

open Pandacap.FurAffinity.Models
open Pandacap.FurAffinity.Interfaces

type FurAffinityClientFactory() =
    interface IFurAffinityClientFactory with
        member _.CreateClient(credentials) =
            new FurAffinityClient(Domain.WWW, credentials)

        member _.CreateClient(credentials, domain) =
            new FurAffinityClient(domain, credentials)
