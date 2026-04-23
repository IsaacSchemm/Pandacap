namespace Pandacap.DeviantArt.Feeds

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.DeviantArt.Feeds.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddDeviantArtFeeds(serviceCollection: IServiceCollection) =
        serviceCollection.AddScoped<IDeviantArtFeedProvider, DeviantArtFeedProvider>()
