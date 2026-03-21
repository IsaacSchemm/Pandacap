namespace Pandacap.Platforms

type PostPlatform =
| ActivityPub
| ATProto
| Bluesky
| DeviantArt
| Feeds
| FurAffinity
| FurryNetwork
| Pandacap
| Reddit
| SheezyArt
| Weasyl
| WhiteWind
| Leaflet

module PostPlatform =
    let GetBadge platform =
        match platform with
        | ActivityPub -> PlatformBadge.Create "ActivityPub" "#f1007e" "white"
        | ATProto -> PlatformBadge.Create "ATProto" "#397EF6" "white"
        | Bluesky -> PlatformBadge.Create "Bluesky" "#397EF6" "white"
        | DeviantArt -> PlatformBadge.Create "DeviantArt" "#00e59b" "black"
        | Feeds -> PlatformBadge.Create "Feeds" "#e8e8e8" "black"
        | FurAffinity -> PlatformBadge.Create "Fur Affinity" "#2E3B41" "#cfcfcf"
        | FurryNetwork -> PlatformBadge.Create "Furry Network" "#2e76b4" "white"
        | Leaflet -> PlatformBadge.Create "Leaflet" "blue" "white"
        | Pandacap -> PlatformBadge.Create "Pandacap" "purple" "white"
        | Reddit -> PlatformBadge.Create "Reddit" "#ff4500" "white"
        | SheezyArt -> PlatformBadge.Create "Sheezy.Art" "rgb(91, 118, 145)" "rgb(201, 216, 225)"
        | Weasyl -> PlatformBadge.Create "Weasyl" "#990000" "white"
        | WhiteWind -> PlatformBadge.Create "WhiteWind" "rgb(243, 244, 246)" "rgb(55, 65, 81)" 
