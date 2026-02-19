namespace Pandacap.ActivityPub.Communication

open System
open System.Net.Http
open System.Net.Http.Headers
open System.Security.Cryptography
open System.Text
open System.Threading
open Pandacap.ActivityPub
open Pandacap.Html

type ActivityPubRequestHandler(
    prerequisites: IActivityPubCommunicationPrerequisites,
    httpClientFactory: IHttpClientFactory,
    hostInformation: ActivityPubHostInformation
) =
    let mediaTypes = [
        "application/activity+json"
        "application/ld+json; profile=\"https://www.w3.org/ns/activitystreams\""
    ]

    let addSignatureAsync (req: HttpRequestMessage) = task {
        let headers = [
            "(request-target)", $"{req.Method.Method.ToLowerInvariant()} {req.RequestUri.AbsolutePath}"
            "host", req.Headers.Host
            "date", (req.Headers.Date.Value.ToString("r"))

            match req.Headers.TryGetValues("Digest") with
            | true, values -> "digest", Seq.exactlyOne values
            | _ -> ()
        ]

        let! signature =
            headers
            |> Seq.map (fun (k, v) -> $"{k}: {v}")
            |> String.concat "\n"
            |> Encoding.UTF8.GetBytes
            |> prerequisites.SignRsaSha256Async

        let headerNames =
            headers
            |> Seq.map fst
            |> String.concat " "

        req.Headers.Add("Signature", $"keyId=\"{hostInformation.ActorId}#main-key\",algorithm=\"rsa-sha256\",headers=\"{headerNames}\",signature=\"{Convert.ToBase64String(signature)}\"")
    }

    let rec getAsync (url: Uri) (includeSignature: bool) (cancellationToken: CancellationToken) = task {
        try
            use req = new HttpRequestMessage(HttpMethod.Get, url)
            req.Headers.Host <- url.Host
            req.Headers.Date <- DateTime.UtcNow
            req.Headers.UserAgent.ParseAdd(prerequisites.UserAgent)

            if includeSignature then
                do! addSignatureAsync req

            for mediaType in mediaTypes do
                req.Headers.Accept.ParseAdd(mediaType)

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
                    |> LinkRelAlternate.ParseFromHtml
                    |> Seq.where (fun attr -> mediaTypes |> Seq.contains attr.Type)
                    |> Seq.map (fun attr -> new Uri(url, attr.Href))
                    |> Seq.except [url]
                    |> Seq.tryHead
                    |> Option.defaultWith (fun () -> failwithf "Request returned an HTML response with no link rel=alternate for %A" mediaTypes)

                return! getAsync href true cancellationToken
            | _ ->
                return body
        with _ when includeSignature ->
            return! getAsync url false cancellationToken
    }

    member _.GetJsonAsync (url: Uri, cancellationToken: CancellationToken) = task {
        return! getAsync url true cancellationToken
    }

    member _.PostAsync (url: Uri, json: string) = task {
        let body = Encoding.UTF8.GetBytes(json)
        let digest =
            body
            |> SHA256.HashData
            |> Convert.ToBase64String

        use req = new HttpRequestMessage(HttpMethod.Post, url)
        req.Headers.Host <- url.Host
        req.Headers.Date <- DateTime.UtcNow
        req.Headers.UserAgent.ParseAdd(prerequisites.UserAgent)

        req.Headers.Add("Digest", $"SHA-256={digest}")

        do! addSignatureAsync req

        req.Content <- new ByteArrayContent(body)
        req.Content.Headers.ContentType <- new MediaTypeHeaderValue(Seq.head mediaTypes)

        use httpClient = httpClientFactory.CreateClient()

        use! res = httpClient.SendAsync(req)
        res.EnsureSuccessStatusCode() |> ignore
    }
