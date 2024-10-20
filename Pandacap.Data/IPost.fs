﻿namespace Pandacap.Data

open System
open Pandacap.Types

/// A post to be shown in one of Pandacap's "paged" areas, like the gallery or inbox, using the "List" Razor view.
type IPost =
    abstract member Id: string
    abstract member Username: string
    abstract member Usericon: string
    abstract member ProfileUrl: string
    abstract member Badges: Badge list
    abstract member DisplayTitle: string
    abstract member Timestamp: DateTimeOffset
    abstract member LinkUrl: string
    abstract member ThumbnailUrls: string seq 
