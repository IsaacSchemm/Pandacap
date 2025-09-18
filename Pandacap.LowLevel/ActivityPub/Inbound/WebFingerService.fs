namespace Pandacap.ActivityPub.Inbound

open System
open System.Net.Http
open System.Net.Http.Json
open Pandacap.ConfigurationObjects

module internal WebFingerService =
    let readAsAsync<'T> (_: 'T) (resp: HttpResponseMessage) =
        resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<'T>()

type WebFingerService(httpClientFactory: IHttpClientFactory) =
    member _.FetchAsync(host: string, resource: string) = task {
        use client = httpClientFactory.CreateClient()

        use! resp = client.GetAsync($"https://{host}/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}")
        return! resp |> WebFingerService.readAsAsync {|
            subject = ""
            aliases = [""]
            links = [{|
                rel = ""
                ``type`` = ""
                href = ""
            |}]
        |}
    }

    member this.ResolveIdForHandleAsync(handle: string) = task {
        let host = handle.Split('@') |> Array.last
        let resource = $"acct:{handle.TrimStart('@')}"
        let! info = this.FetchAsync(host, resource)
        return info.links
            |> Seq.where (fun l -> l.``type`` = "application/activity+json")
            |> Seq.map (fun l -> l.href)
            |> Seq.head
    }
