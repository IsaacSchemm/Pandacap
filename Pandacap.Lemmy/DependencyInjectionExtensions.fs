namespace Pandacap.Lemmy

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.Lemmy.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddLemmyServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<ILemmyClient, LemmyClient>()
