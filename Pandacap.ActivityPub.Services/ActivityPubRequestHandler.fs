namespace Pandacap.ActivityPub.Services

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text
open System.Threading
open FSharp.Data
open Pandacap.ActivityPub.Static
open Pandacap.ActivityPub.Models
open Pandacap.ActivityPub.Services.Interfaces

type internal ActivityPubRequestHandler(
    prerequisites: IActivityPubCommunicationPrerequisites,
    httpClientFactory: IHttpClientFactory
) =
    let activityMediaType = "application/activity+json"

    let addSignatureAsync (req: HttpRequestMessage) (cancellationToken: CancellationToken) = task {
        let headers = [
            "(request-target)", $"{req.Method.Method.ToLowerInvariant()} {req.RequestUri.AbsolutePath}"
            "host", req.Headers.Host
            "date", (req.Headers.Date.Value.ToString("r"))

            match req.Headers.TryGetValues("Digest") with
            | true, values -> "digest", Seq.exactlyOne values
            | _ -> ()
        ]

        let signatureInput =
            headers
            |> Seq.map (fun (k, v) -> $"{k}: {v}")
            |> String.concat "\n"
            |> Encoding.UTF8.GetBytes

        let! signature =
            prerequisites.SignRsaSha256Async(signatureInput, cancellationToken)

        let headerNames =
            headers
            |> Seq.map fst
            |> String.concat " "

        req.Headers.Add("Signature", $"keyId=\"{ActivityPubHostInformation.ActorId}#main-key\",algorithm=\"rsa-sha256\",headers=\"{headerNames}\",signature=\"{Convert.ToBase64String(signature)}\"")
    }

    let findAlternateLinks (html: string) = seq {
        let document = HtmlDocument.Parse html
        let links = document.CssSelect("link")
        for link in links do
            let attr x =
                link.TryGetAttribute(x)
                |> Option.map (fun a -> a.Value())
            if attr "rel" = Some "alternate" then
                match attr "type", attr "href" with
                | Some t, Some href ->
                    yield {|
                        Type = t
                        Href = href
                    |}
                | _ -> ()
    }

    let rec getAsync (url: Uri) (includeSignature: bool) (cancellationToken: CancellationToken) = task {
        try
            use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, url)
            req.Headers.Host <- url.Host
            req.Headers.Date <- DateTime.UtcNow
            req.Headers.UserAgent.ParseAdd(prerequisites.UserAgent)

            if includeSignature then
                do! addSignatureAsync req cancellationToken

            req.Headers.Accept.ParseAdd(activityMediaType)

            use httpClient = httpClientFactory.CreateClient()

            use! res = httpClient.SendAsync(req, cancellationToken)
            res.EnsureSuccessStatusCode() |> ignore

            let! body = res.Content.ReadAsStringAsync(cancellationToken)

            let mediaType =
                res.Content.Headers.ContentType
                |> Option.ofObj
                |> Option.map (fun c -> c.MediaType)

            match mediaType with
            | Some "text/html" ->
                let href =
                    body
                    |> findAlternateLinks
                    |> Seq.where (fun attr -> attr.Type = activityMediaType)
                    |> Seq.map (fun attr -> new Uri(url, attr.Href))
                    |> Seq.except [url]
                    |> Seq.tryHead
                    |> Option.defaultWith (fun () -> raise ActivityJsonNotFoundException)

                return! getAsync href true cancellationToken

            | Some "application/ld+json"
            | Some "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
            | Some "application/activity+json" ->
                return body

            | _ ->
                return raise ActivityJsonNotFoundException

        with _ when includeSignature ->
            return! getAsync url false cancellationToken
    }

    interface IActivityPubRequestHandler with
        member _.GetJsonAsync(url: Uri, cancellationToken: CancellationToken) = task {
            return! getAsync url true cancellationToken
        }

        member _.PostAsync(url: Uri, json: string, cancellationToken) = task {
            let body = Encoding.UTF8.GetBytes(json)
            let digest =
                body
                |> SHA256.HashData
                |> Convert.ToBase64String

            use req = new HttpRequestMessage(System.Net.Http.HttpMethod.Post, url)
            req.Headers.Host <- url.Host
            req.Headers.Date <- DateTime.UtcNow
            req.Headers.UserAgent.ParseAdd(prerequisites.UserAgent)

            req.Headers.Add("Digest", $"SHA-256={digest}")

            do! addSignatureAsync req cancellationToken

            req.Content <- new ByteArrayContent(body)
            req.Content.Headers.ContentType <- MediaTypeHeaderValue.Parse(activityMediaType)

            use httpClient = httpClientFactory.CreateClient()

            use! res = httpClient.SendAsync(req)
            res.EnsureSuccessStatusCode() |> ignore
        }
