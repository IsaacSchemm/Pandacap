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

    type User = {
        name: string
        profile: string
        avatar: string
        full_name: string
    }

    let GetUserAsync factory credentials (name: string) cancellationToken = task {
        use client = getClient factory credentials
        use! resp = client.GetAsync($"/user/{Uri.EscapeDataString(name)}.json", cancellationToken = cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<User>(cancellationToken)
    }

    type Submission = {
        id: int
        title: string
        thumbnail: string
        link: string
        name: string
        profile: string
        profile_name: string
    }

    [<RequireQualifiedAccess>]
    type FavoritesPage =
    | First
    | After of int
    | Before of int

    let GetFavoritesAsync factory credentials (name: string) sfw pagination cancellationToken = task {
        let qs = String.concat "&" [
            "full=1"

            match pagination with
            | FavoritesPage.First -> ()
            | FavoritesPage.After x -> $"next={x}"
            | FavoritesPage.Before x -> $"next={x}"

            if sfw then "sfw=1"
        ]

        use client = getClient factory credentials
        use! resp = client.GetAsync($"/user/{Uri.EscapeDataString(name)}/favorites.json?{qs}", cancellationToken = cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Submission list>(cancellationToken)
    }

    module Notifications =
        type Submissions = {
            new_submissions: Submission list
        }

        let GetSubmissionsAsync factory credentials (from: int) (sfw: bool) cancellationToken = task {
            let qs = String.concat "&" [
                $"from={from}"
                if sfw then "sfw=1"
            ]

            use client = getClient factory credentials
            use! resp = client.GetAsync($"/notifications/submissions.json?{qs}", cancellationToken = cancellationToken)
            return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Submissions>()
        }

        type Watch = {
            watch_id: int
            name: string
            profile: string
            profile_name: string
            avatar: string
            posted_at: DateTime
            deleted: bool
        }

        type SubmissionComment = {
            comment_id: int
            name: string
            profile: string
            profile_name: string
            is_reply: bool
            your_submission: bool
            their_submission: bool
            submission_id: int
            title: string
            posted_at: DateTime
            deleted: bool
        }

        type JournalComment = {
            comment_id: int
            name: string
            profile: string
            profile_name: string
            is_reply: bool
            your_journal: bool
            their_journal: bool
            journal_id: int
            title: string
            posted_at: DateTime
            deleted: bool
        }

        type Shout = {
            shout_id: int
            name: string
            profile: string
            profile_name: string
            posted_at: DateTime
            deleted: bool
        }

        type Favorite = {
            favorite_notification_id: int
            name: string
            profile: string
            profile_name: string
            submission_id: int
            submission_name: string
            posted_at: DateTime
            deleted: bool
        }

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
            new_watches: Watch list
            new_submission_comments: SubmissionComment list
            new_journal_comments: Journal list
            new_shouts: Shout list
            new_favorites: Favorite list
            new_journals: Journal list
        }

        let GetOthersAsync factory credentials cancellationToken = task {
            use client = getClient factory credentials
            use! resp = client.GetAsync($"/notifications/others.json", cancellationToken = cancellationToken)
            return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Others>()
        }

    type Note = {
        note_id: int
        subject: string
        is_inbound: bool
        is_read: bool
        name: string
        profile: string
        profile_name: string
        user_deleted: bool
        posted_at: DateTime
    }

    let GetNotesAsync factory credentials (folder: string) cancellationToken = task {
        use client = getClient factory credentials
        use! resp = client.GetAsync($"/notes/{Uri.EscapeDataString(folder)}.json", cancellationToken = cancellationToken)
        return! resp.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<Note list>()
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