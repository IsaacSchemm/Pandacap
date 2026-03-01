namespace Pandacap.Resolvers

[<RequireQualifiedAccess>]
type ResolverResult =
| ActivityPubActor of id: string
| ActivityPubPost of id: string
| BlueskyProfile of did: string
| BlueskyPost of did: string * rkey: string
