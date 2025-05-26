namespace Pandacap.Clients

open System
open System.Net.Http
open System.Text.Json
open System.Threading
open Pandacap.Data
open Pandacap.Html
open Pandacap.PlatformBadges

module Mastodon =
    let GetLocalTimelineAsync(client: HttpClient, host: string, max_id: string, cancellationToken: CancellationToken) = task {
        let qs = String.concat "&" [
            "local=true"
            if not (isNull max_id) then
                $"max_id={Uri.EscapeDataString(max_id)}"
        ]

        use req = new HttpRequestMessage(HttpMethod.Get, $"https://{host}/api/v1/timelines/public?{qs}")
        req.Headers.Accept.ParseAdd("application/json")

        use! resp = client.SendAsync(req, cancellationToken)
        let! json = resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken)

        let deserializeAs (_: 'T) (json: string) = JsonSerializer.Deserialize<'T>(json)

        let list = json |> deserializeAs [{|
            id = ""
            created_at = DateTimeOffset.MinValue
            sensitive = false
            spoiler_text = ""
            uri = ""
            url = ""
            content = ""
            account = {|
                username = ""
                display_name = ""
                uri = ""
                url = ""
                avatar = ""
            |}
            media_attachments = [{|
                ``type`` = ""
                url = ""
                preview_url = ""
                description = ""
            |}]
            tags = [{|
                name = ""
            |}]
        |}]

        return [
            for post in list do {
                new IPost with
                    member _.Badges = [PostPlatform.GetBadge ActivityPub]
                    member _.DisplayTitle =
                        if post.sensitive then
                            $"[{post.spoiler_text}]"
                        else
                            post.content
                            |> TextConverter.FromHtml
                            |> TitleGenerator.FromBody
                            |> ExcerptGenerator.FromText 60
                    member _.Id = post.id
                    member _.LinkUrl = $"/RemotePosts?id={Uri.EscapeDataString(post.uri)}"
                    member _.PostedAt = post.created_at
                    member _.ProfileUrl = post.account.url
                    member _.Thumbnails = [
                        if not post.sensitive then
                            for attachment in post.media_attachments do
                                if attachment.``type`` = "image" then {
                                    new IPostThumbnail with
                                        member _.Url = attachment.preview_url
                                        member _.AltText = attachment.description
                                }
                    ]
                    member _.Username = post.account.username
                    member _.Usericon = post.account.avatar
            }
        ]
    }
