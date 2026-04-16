namespace Pandacap.PlatformLinks.Models

open Pandacap.PlatformLinks.Interfaces

type PlatformLinkContext =
| Profile
| Post of IPlatformLinkPostSource
