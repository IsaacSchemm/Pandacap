namespace Pandacap.UI.Badges

type Badge = {
    Platform: Platform
    Text: string
    Background: string
    Color: string
} with
    member this.WithText(text) = { this with Text = text }
