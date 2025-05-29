namespace Pandacap.LowLevel.Txt

open System.Text

module FeedBuilder =
    let BuildFeed (feed: Feed) =
        [
            for x in feed.metadata.url do
                $"# url = {x.OriginalString}"

            for x in feed.metadata.nick do
                $"# nick = {x}"

            for x in feed.metadata.avatar do
                $"# avatar = {x}"

            for f in feed.metadata.follow do
                $"# follow = {f.text} {f.url.OriginalString}"

            for l in feed.metadata.link do
                $"# link = {l.text} {l.url.OriginalString}"

            for x in feed.metadata.refresh do
                $"# refresh = {x}"

            for twt in feed.twts do
                String.concat "" [
                    HashGenerator.GetDateTimeString(twt.timestamp)

                    "\t"

                    match twt.replyContext with
                    | Hash hash ->
                        $"(#{hash}) "
                    | NoReplyContext -> ()

                    twt.text
                        .Replace("\r", "")
                        .Replace('\n', char 0x2028)
                ]
        ]
        |> String.concat "\n"
        |> Encoding.UTF8.GetBytes
