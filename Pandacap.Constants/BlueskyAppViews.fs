namespace Pandacap.Constants

module BlueskyAppViews =
    let private appView (name: string) (host: string) (icon: string) = {|
        name = name
        host = host
        icon = icon
    |}

    let Bluesky = appView "Bluesky" "bsky.app" "bluesky.png"
    let Blacksky = appView "Blacksky" "blacksky.community" "blacksky.png"
    let RedDwarf = appView "Red Dwarf" "reddwarf.app" "reddwarf.ico"

    let All = [Bluesky; Blacksky; RedDwarf]
