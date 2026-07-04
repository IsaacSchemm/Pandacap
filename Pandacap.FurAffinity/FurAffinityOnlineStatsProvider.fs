namespace Pandacap.FurAffinity

open System
open System.Net.Http
open System.Threading
open FSharp.Data
open Pandacap.FurAffinity.Models
open Pandacap.FurAffinity.Interfaces

type internal FurAffinityOnlineStatsProvider(
    httpMessageHandler: HttpMessageHandler,
    timeProvider: TimeProvider
) =
    let flag = new SemaphoreSlim(1, 1)

    let mutable ok = false
    let mutable exp = DateTimeOffset.MinValue

    interface IFurAffinityOnlineStatsProvider with
        member _.IsBotUsageOkAsync(cancellationToken) = task {
            do! flag.WaitAsync(cancellationToken)

            try
                if exp < timeProvider.GetUtcNow() then
                    use client = new HttpClient(httpMessageHandler, disposeHandler = false)

                    use! resp = client.GetAsync("https://www.furaffinity.net/help/", cancellationToken = cancellationToken)
                    ignore (resp.EnsureSuccessStatusCode())

                    let! html = resp.Content.ReadAsStringAsync(cancellationToken)
                    let document = HtmlDocument.Parse html

                    let stats = Scraper.getOnlineStats document

                    ok <- stats.Registered < 15000
                    exp <-
                        if ok
                        then timeProvider.GetUtcNow().AddMinutes(5)
                        else timeProvider.GetUtcNow().AddMinutes(55)
            finally
                flag.Release() |> ignore

            return ok
        }
