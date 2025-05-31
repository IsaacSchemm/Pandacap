namespace Pandacap.Clients

open System
open System.Net.Http
open System.Threading
open Pandacap.ConfigurationObjects
open Pandacap.Data
open Pandacap.LowLevel.Twtxt
open Pandacap.LowLevel.MyLinks

type TwtxtClient(
    appInfo: ApplicationInformation,
    httpClientFactory: IHttpClientFactory
) =
    member _.ReadFeedAsync(uri: string, cancellationToken: CancellationToken) = task {
        let appName = UserAgentInformation.ApplicationName
        let ver = UserAgentInformation.VersionNumber

        use client = httpClientFactory.CreateClient()
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{appName}/{ver} (+https://{appInfo.ApplicationHostname}/twtxt.txt; @{appInfo.Username})")

        use! resp = client.GetAsync(uri, cancellationToken)

        let! data = resp.EnsureSuccessStatusCode().Content.ReadAsByteArrayAsync(cancellationToken)

        let feed =
            let f = FeedReader.ReadFeed(data)

            match f.metadata.url with
            | _::_ -> f
            | [] ->
                let m = { f.metadata with url = [uri] }
                { f with metadata = m }

        return feed
    }

    member _.BuildFeed(url: string, avatars: Avatar seq, links: MyLink seq, following: TwtxtFeed seq, posts: Post seq, next: Post seq) =
        let twt (post: Post) = {
            timestamp = post.PublishedTime
            text = String.concat "\u2028" [
                if isNull post.Title then
                    post.Body
                else
                    post.Title

                for i in post.Images do
                    $"![{i.AltText}](https://{appInfo.ApplicationHostname}/Blobs/UserPosts/{post.Id}/{i.Blob.Id})"

                String.concat " " [
                    for t in post.Tags do $"#{t}"
                ]
            ]
            replyContext = NoReplyContext
        }

        FeedBuilder.BuildFeed {
            metadata = {
                url = [url]
                nick = [appInfo.Username]
                avatar = [
                    for a in avatars do
                        $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{a.Id}"
                ]
                follow = [
                    for f in following do {
                        url = f.Url
                        text = f.Nick
                    }
                ]
                link = [
                    for link in links do {
                        url = link.url
                        text = link.platformName
                    }
                ]
                refresh = []
                prev = [
                    for post in next do {
                        hash = HashGenerator.getHash url (twt post)
                        url = $"{(new Uri(url)).GetLeftPart(UriPartial.Path)}?format=twtxt&next={post.Id}"
                    }
                ]
            }
            twts = [for post in posts do twt post]
        }
