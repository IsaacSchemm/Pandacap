namespace Pandacap.ActivityPub.Inbound

open System
open System.Net.Http
open System.Net.Http.Json
open Pandacap.ActivityPub.RemoteObjects.Interfaces

module internal WebFingerService =
    let readAsAsync<'T> cancellationToken (_: 'T) (resp: HttpResponseMessage) =
        resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<'T>(cancellationToken)

type WebFingerService(httpClientFactory: IHttpClientFactory) =
    interface IWebFingerService with
        member _.ResolveIdForHandleAsync(handle, cancellationToken) = task {
            let host = handle.Split('@') |> Array.last
            let resource = $"acct:{handle.TrimStart('@')}"

            let! info = task {
                use client = httpClientFactory.CreateClient()

                use! resp = client.GetAsync($"https://{host}/.well-known/webfinger?resource={Uri.EscapeDataString(resource)}")
                return! resp |> WebFingerService.readAsAsync cancellationToken {|
                    subject = ""
                    aliases = [""]
                    links = [{|
                        rel = ""
                        ``type`` = ""
                        href = ""
                    |}]
                |}
            }

            return info.links
                |> Seq.where (fun l -> l.``type`` = "application/activity+json")
                |> Seq.map (fun l -> l.href)
                |> Seq.head
        }
