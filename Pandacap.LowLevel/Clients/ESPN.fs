namespace Pandacap.Clients

open System
open System.Net.Http
open System.Text.Json
open FSharp.Data

module ESPN =
    let contributorDataSample = {|
        page = {|
            content = {|
                contributor = Some {|
                    feed = [{|
                        headline = ""
                        id = ""
                        published = DateTimeOffset.MinValue
                        byline = ""
                        description = ""
                        hdr = Some {|
                            image = {|
                                url = ""
                            |}
                        |}
                        absoluteLink = ""
                    |}]
                |}
            |}
        |}
    |}

    let parseContributorData (str: string) = List.last [
        contributorDataSample
        JsonSerializer.Deserialize(str)
    ]

type ESPNClient(
    httpClientFactory: IHttpClientFactory
) =
    member _.GetContributorAsync(slug: string) = task {
        use client = httpClientFactory.CreateClient()
        use! resp = client.GetAsync($"https://www.espn.com/contributor/{Uri.EscapeDataString(slug)}")
        let! html = resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync()
        let mutable index1 = html.IndexOf("window['__espnfitt__']=")
        while html[index1] <> '{' do
            index1 <- index1 + 1
        let mutable index2 = html.IndexOf(";</script>")
        while html[index2] <> '}' do
            index2 <- index2 - 1
        let str = html.Substring(index1, index2 - index1 + 1)
        let obj = ESPN.parseContributorData str
        failwithf "%A" obj
    }
