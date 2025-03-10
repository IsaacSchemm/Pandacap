namespace Pandacap.Podcasts

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Threading.Tasks

type internal PodcastStream(
    httpClientFactory: IHttpClientFactory,
    uri: Uri,
    contentLength: int64
) =
    inherit Stream()

    let mutable pos = 0L

    override _.CanRead = true
    override _.CanSeek = true
    override _.CanWrite = false

    override _.Length = contentLength

    override _.Position with get () = pos and set v = pos <- v

    override _.Flush() = ()

    override _.Read(_, _, _) =  raise (NotImplementedException())

    override _.ReadAsync(buffer, cancellationToken) = ValueTask<int> (task {
        let posTo = min (pos + int64 buffer.Length) contentLength

        if posTo = pos then
            return 0
        else
            use client = httpClientFactory.CreateClient()

            use req = new HttpRequestMessage(HttpMethod.Get, uri)
            req.Headers.Range <- new RangeHeaderValue(pos, posTo)

            use! resp = client.SendAsync(req, cancellationToken)
            use! stream = resp.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken)

            let! read = stream.ReadAsync(buffer, cancellationToken)
            pos <- pos + int64 read

            return read
    })

    override _.Seek(offset, origin) =
        pos <-
            match origin with
            | SeekOrigin.Begin -> offset
            | SeekOrigin.Current -> pos + offset
            | SeekOrigin.End -> contentLength + offset
            | x -> raise (ArgumentException($"Unrecgonized origin {x}", nameof origin))
        pos

    override _.SetLength(_) = raise (NotSupportedException())

    override _.Write(_, _, _) = raise (NotSupportedException())
