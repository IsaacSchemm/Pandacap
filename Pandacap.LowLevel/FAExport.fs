namespace Pandacap.LowLevel

open System
open System.Net.Http
open System.Net.Http.Json
open Pandacap.Types

module FAExport =
    let private getClient (factory: IHttpClientFactory) (credentials: IFurAffinityCredentials) =
        let client = factory.CreateClient()
        client.BaseAddress <- new Uri("https://faexport.spangle.org.uk")
        client.DefaultRequestHeaders.Add("FA_COOKIE",  $"b={credentials.B}; a={credentials.A}")
        client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentInformation.UserAgent)
        client

    type NotificationsSubmission = {
        id: int
        title: string
        thumbnail: string
        link: string
        name: string
        profile: string
        profile_name: string
    }

    type NotificationsSubmissions = {
        new_submissions: NotificationsSubmission list
    }

    let GetNotificationsSubmissionsAsync factory credentials (from: int) (sfw: bool) cancellationToken = task {
        let qs = String.concat "&" [
            $"from={from}"
            if sfw then "sfw=1"
        ]

        use client = getClient factory credentials
        use! resp = client.GetAsync($"/notifications/submissions.json?{qs}", cancellationToken = cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<NotificationsSubmissions>()
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