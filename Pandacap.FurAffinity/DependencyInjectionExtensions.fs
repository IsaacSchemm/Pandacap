namespace Pandacap.FurAffinity

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.FurAffinity.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddFurAffinityClient(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddSingleton<IFurAffinityHttpHandlerProvider, FurAffinityHttpHandlerProvider>()
            .AddScoped<IFurAffinityClientFactory, FurAffinityClientFactory>()
