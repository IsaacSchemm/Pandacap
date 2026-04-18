namespace Pandacap.DeviantArt

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.DeviantArt.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddDeviantArtServices(serviceCollection: IServiceCollection) =
        serviceCollection.AddScoped<IDeviantArtFeedProvider, DeviantArtFeedProvider>()
