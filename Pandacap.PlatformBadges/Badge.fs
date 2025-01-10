namespace Pandacap.PlatformBadges

type Badge = {
    Text: string
    Background: string
    Color: string
}

module Badge =
    let Create text bgcolor color = {
        Text = text
        Background = bgcolor
        Color = color
    }

    let WithParenthetical text badge = {
        badge with Text = $"{badge.Text} ({text})"
    }
