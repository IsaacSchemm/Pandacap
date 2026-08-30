namespace Pandacap.ATProto.Feeds

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ATProto.Feeds.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddATProtoFeeds(serviceCollection: IServiceCollection) =
        serviceCollection.AddScoped<IATProtoFeedProvider, ATProtoFeedProvider>()
