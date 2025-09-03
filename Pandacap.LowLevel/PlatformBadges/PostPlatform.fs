namespace Pandacap.PlatformBadges

type PostPlatform =
| ActivityPub
| ATProto
| Bluesky
| DeviantArt
| FurAffinity
| FurryNetwork
| Pandacap
| Reddit
| RSS_Atom
| SheezyArt
| Weasyl
| WhiteWind

module PostPlatform =
    let GetBadge platform =
        match platform with
        | ActivityPub -> Badge.Create "ActivityPub" "#f1007e" "white"
        | ATProto -> Badge.Create "ATProto" "#397EF6" "white"
        | Bluesky -> Badge.Create "Bluesky" "#397EF6" "white"
        | DeviantArt -> Badge.Create "DeviantArt" "#00e59b" "black"
        | FurAffinity -> Badge.Create "Fur Affinity" "#2E3B41" "#cfcfcf"
        | FurryNetwork -> Badge.Create "Furry Network" "#2e76b4" "white"
        | Pandacap -> Badge.Create "Pandacap" "purple" "white"
        | Reddit -> Badge.Create "Reddit" "#ff4500" "white"
        | RSS_Atom -> Badge.Create "RSS / Atom" "#f99000" "white"
        | SheezyArt -> Badge.Create "Sheezy.Art" "rgb(91, 118, 145)" "rgb(201, 216, 225)"
        | Weasyl -> Badge.Create "Weasyl" "#990000" "white"
        | WhiteWind -> Badge.Create "WhiteWind" "rgb(243, 244, 246)" "rgb(55, 65, 81)" 
