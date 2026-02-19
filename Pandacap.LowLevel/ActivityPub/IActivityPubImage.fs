namespace Pandacap.ActivityPub

type IActivityPubImage =
    abstract member GetUrl: appInfo: ActivityPubHostInformation -> string
    abstract member AltText: string
    abstract member MediaType: string
    abstract member HorizontalFocalPoint: decimal option
    abstract member VerticalFocalPoint: decimal option
