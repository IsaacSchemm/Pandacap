namespace Pandacap.Resolvers.Models

[<RequireQualifiedAccess>]
type ResolverResult =
| None
| ActivityPubActor of id: string
| ActivityPubPost of id: string
| BlueskyProfile of did: string
| BlueskyPost of did: string * rkey: string
