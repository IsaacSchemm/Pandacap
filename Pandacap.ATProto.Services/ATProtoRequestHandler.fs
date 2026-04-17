namespace Pandacap.ATProto.Services

open System.Net.Http
open Pandacap.ATProto.Services.Interfaces

type ATProtoRequestHandler(
    httpClientFactory: IHttpClientFactory
) =
    interface IATProtoRequestHandler with
        member _.GetJsonAsync(uri, cancellationToken) = task {
            use client = httpClientFactory.CreateClient()
            use req = new HttpRequestMessage(HttpMethod.Get, uri)
            req.Headers.Accept.ParseAdd("application/json")
            return! client.SendAsync(req, cancellationToken)
        }
