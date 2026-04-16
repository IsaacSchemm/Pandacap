namespace Pandacap.PlatformLinks

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.PlatformLinks.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddPlatformLinkProvider(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IPlatformLinkProvider, PlatformLinkProvider>()
