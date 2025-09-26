namespace Pandacap.ActivityPub

type IImage =
    abstract member GetUrl: appInfo: HostInformation -> string
    abstract member AltText: string
    abstract member MediaType: string
    abstract member HorizontalFocalPoint: decimal option
    abstract member VerticalFocalPoint: decimal option
