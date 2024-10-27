namespace Pandacap.LowLevel

type Bridge = {
    /// The ActivityPub ID of an actor that a user can follow to enable the bridge.
    Bot: string

    /// Activities that normally go to followers will not go to these domains unless the Pandacap user follows the corresponding bot.
    Domains: string list
} with
    static member All = [
        {
            Bot = "https://bsky.brid.gy/bsky.brid.gy"
            Domains = [
                "bsky.brid.gy"
                "web.brid.gy"
            ]
        }
    ]
