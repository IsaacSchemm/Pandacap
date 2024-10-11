namespace Pandacap.Types

type PostPlatform = ActivityPub | ATProto | DeviantArt | Pandacap | RSS_Atom | Weasyl

module PostPlatform =
    let GetBadge platform =
        match platform with
        | ActivityPub -> Badge.Create "ActivityPub" "#f1007e" "white"
        | ATProto -> Badge.Create "Bluesky" "#397EF6" "white"
        | DeviantArt -> Badge.Create "DeviantArt" "#00e59b" "black"
        | Pandacap -> Badge.Create "Pandacap" "black" "white"
        | RSS_Atom -> Badge.Create "RSS / Atom" "#f99000" "white"
        | Weasyl -> Badge.Create "Weasyl" "#990000" "white"
