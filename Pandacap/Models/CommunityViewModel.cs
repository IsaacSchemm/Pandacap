﻿using Microsoft.FSharp.Collections;
using Pandacap.LowLevel;

namespace Pandacap.Models
{
    public record CommunityViewModel(
        string Host,
        Lemmy.Community Community,
        FSharpList<Lemmy.PostView> PostObjects);
}
