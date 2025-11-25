namespace Pandacap.ActivityPub

type ILink =
    abstract member Href: string
    abstract member MediaType: string
