namespace Pandacap.LowLevel

type ExternalPlatformAdditionalLink = {
    Text: string
    Url: string
}

type ExternalPlatform = {
    SiteName: string
    Username: string
    ProfileUrl: string
    AdditionalLinks: ExternalPlatformAdditionalLink list
}
