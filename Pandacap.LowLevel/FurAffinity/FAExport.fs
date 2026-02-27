namespace Pandacap.FurAffinity

open System
open System.Net.Http
open System.Net.Http.Json

module FAExport =
    let private getClient (factory: IHttpClientFactory) (credentials: IFurAffinityCredentials) =
        let client = factory.CreateClient()
        client.BaseAddress <- new Uri("https://faexport.spangle.org.uk")
        client.DefaultRequestHeaders.Add("FA_COOKIE",  $"b={credentials.B}; a={credentials.A}")
        client.DefaultRequestHeaders.UserAgent.ParseAdd(credentials.UserAgent)
        client

    module Notifications =
        type Journal = {
            journal_id: int
            title: string
            name: string
            profile: string
            profile_name: string
            posted_at: DateTime
            deleted: bool
        }

        type Others = {
            new_journals: Journal list
        }

        let GetOthersAsync factory credentials cancellationToken = task {
            use client = getClient factory credentials
            use! resp = client.GetAsync($"/notifications/others.json", cancellationToken = cancellationToken)
            return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Others>()
        }

    type PostedJournal = {
        url: string
    }

    let PostJournalAsync factory credentials title description cancellationToken = task {
        use client = getClient factory credentials
        use req = new HttpRequestMessage(HttpMethod.Post, "/journal.json")
        req.Content <- new FormUrlEncodedContent(dict [
            "title", title
            "description", description
        ])
        use! resp = client.SendAsync(req, cancellationToken = cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<PostedJournal>()
    }