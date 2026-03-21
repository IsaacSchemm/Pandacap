namespace Pandacap.Platforms

type PlatformBadge = {
    Text: string
    Background: string
    Color: string
}

module PlatformBadge =
    let Create text bgcolor color = {
        Text = text
        Background = bgcolor
        Color = color
    }

    let WithParenthetical text badge = {
        badge with Text = $"{badge.Text} ({text})"
    }
