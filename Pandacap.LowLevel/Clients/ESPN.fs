namespace Pandacap.Clients

open System
open System.Text.Json

module ESPN =
    let ContributorDataSample = {|
        page = {|
            content = {|
                contributor = {|
                    feed = [{|
                        headline = ""
                        id = ""
                        published = DateTimeOffset.MinValue
                        byline = ""
                        description = ""
                        hdr = {|
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

    let ParseContributorData (json: string) = List.last [
        ContributorDataSample
        JsonSerializer.Deserialize(json)
    ]

//type ESPNClient(
//    httpClientFactory: IHttpClientFactory
//) =
//    member _.GetContributorAsync(slug: string) = task {
//        use client = httpClientFactory.CreateClient()
//        use! resp = client.GetAsync($"https://www.espn.com/contributor/{Uri.EscapeDataString(slug)}")
//        let! html = resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync()
//        let mutable index1 = html.IndexOf("window['__espnfitt__']=")
//        while html[index1] <> '{' do
//            index1 <- index1 + 1
//        let mutable index2 = html.IndexOf(";</script>")
//        while html[index2] <> '}' do
//            index2 <- index2 - 1
//        let str = html.Substring(index1, index2 - index1 + 1)
//        let obj = ESPN.parseContributorData str
//        failwithf "%A" obj
//    }
