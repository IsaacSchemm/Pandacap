namespace Pandacap.Clients

open System
open System.Net.Http
open System.Threading
open Pandacap.ConfigurationObjects
open Pandacap.Data
open Pandacap.LowLevel.Txt
open Pandacap.LowLevel.MyLinks

type FeedType = Artwork | Text | All

type TwtxtClient(
    appInfo: ApplicationInformation,
    httpClientFactory: IHttpClientFactory
) =
    let myFeed = $"https://{appInfo.ApplicationHostname}/twtxt.txt"

    member _.MyFeed = myFeed

    member _.ReadFeedAsync(uri: Uri, cancellationToken: CancellationToken) = task {
        let appName = UserAgentInformation.ApplicationName
        let ver = UserAgentInformation.VersionNumber

        use client = httpClientFactory.CreateClient()
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{appName}/{ver} (+{myFeed}; @{appInfo.Username})")

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

    member _.BuildFeed(avatars: Avatar list, links: MyLink list, following: TwtxtFeed list, posts: Post list) =
        FeedBuilder.BuildFeed {
            metadata = {
                url = [new Uri(myFeed)]
                nick = [appInfo.Username]
                avatar = [
                    match avatars with
                    | [a] -> $"https://{appInfo.ApplicationHostname}/Blobs/Avatar/{a.Id}"
                    | _ -> ()
                ]
                follow = [
                    for f in following do {
                        url = new Uri(f.Url)
                        text = f.Nick
                    }
                ]
                link = [
                    for link in links do {
                        url = new Uri(link.url)
                        text = link.platformName
                    }
                ]
                refresh = []
            }
            twts = [
                for post in posts do {
                    timestamp = post.PublishedTime
                    text = String.concat "\u2028" [
                        if isNull post.Title then
                            post.Body
                        else
                            post.Title

                        for i in post.Images do
                            $"![{i.AltText}](https://{appInfo.ApplicationHostname}/Blobs/Thumbnails/{i.Blob.Id})"

                        if post.Images.Count > 0 then
                            $"https://{appInfo.ApplicationHostname}/UserPosts/{post.Id}"

                        String.concat " " [
                            for t in post.Tags do $"#{t}"
                        ]
                    ]
                    replyContext = NoReplyContext
                }
            ]
        }
