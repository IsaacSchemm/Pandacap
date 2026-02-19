namespace Pandacap.ActivityPub

type IActivityPubLink =
    abstract member Href: string
    abstract member MediaType: string
