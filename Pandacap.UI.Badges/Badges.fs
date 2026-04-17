namespace Pandacap.UI.Badges

module Badges =
    let private createBadge w x y z = {
        Platform = w
        Text = x
        Background = y
        Color = z
    }

    let ActivityPub = createBadge ActivityPub "ActivityPub" "#f1007e" "white"
    let ATProto = createBadge ATProto "ATProto" "#397EF6" "white"
    let DeviantArt = createBadge DeviantArt "DeviantArt" "#00e59b" "black"
    let Feeds = createBadge Feeds "Feeds" "#e8e8e8" "black"
    let FurAffinity = createBadge FurAffinity "Fur Affinity" "#2E3B41" "#cfcfcf"
    let Pandacap = createBadge Pandacap "Pandacap" "purple" "white"
    let Weasyl = createBadge Weasyl "Weasyl" "#990000" "white"
