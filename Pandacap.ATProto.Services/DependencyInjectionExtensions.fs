namespace Pandacap.ATProto.Services

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ATProto.Services.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddATProtoServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IATProtoRequestHandler, ATProtoRequestHandler>()
            .AddScoped<IDIDResolver, DIDResolver>()
            .AddScoped<IATProtoService, ATProtoService>()
            .AddScoped<IBlueskyService, BlueskyService>()
            .AddScoped<IConstellationService, ConstellationService>()
