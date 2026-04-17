namespace Pandacap.FurAffinity.Models

[<RequireQualifiedAccess>]
type FavoritesPage =
| First
| After of int64
| Before of int64
