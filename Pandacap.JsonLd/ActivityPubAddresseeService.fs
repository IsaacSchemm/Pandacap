namespace Pandacap.JsonLd

open System.Threading

type ActivityPubAddresseeService(remoteActorService: ActivityPubRemoteActorService) =
    member _.HydrateAsync(id: string, cancellationToken: CancellationToken) = task {
        if id = "https://www.w3.org/ns/activitystreams#Public" then
            return Public
        else
            try
                let! actor = remoteActorService.FetchActorAsync(id, cancellationToken)

                match actor.Type with
                | "https://www.w3.org/ns/activitystreams#Person" ->
                    return Person actor
                | "https://www.w3.org/ns/activitystreams#Group" ->
                    return Group actor
                | _ ->
                    return Other id
            with _ ->
                return Other id
    }
