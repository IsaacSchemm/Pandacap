namespace Pandacap.CanonicalTags.Tree

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.CanonicalTags.Tree.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddCanonicalTagTreeService(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<ICanonicalTagTreeService, CanonicalTagTreeService>()
