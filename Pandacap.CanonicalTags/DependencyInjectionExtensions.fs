namespace Pandacap.CanonicalTags

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.CanonicalTags.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddCanonicalTagServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<ICanonicalTagTreeService, CanonicalTagTreeService>()
