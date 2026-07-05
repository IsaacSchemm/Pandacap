namespace Pandacap.ActivityPub.Services

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ActivityPub.Services.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddActivityPubInboundServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IActivityPubInboxRequestHandler, ActivityPubInboxRequestHandler>()

    [<Extension>]
    let AddActivityPubOutboundServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IActivityPubProfileTranslator, ActivityPubProfileTranslator>()
            .AddScoped<IActivityPubPostTranslator, ActivityPubPostTranslator>()
            .AddScoped<IActivityPubRelationshipTranslator, ActivityPubRelationshipTranslator>()
            .AddScoped<IActivityPubInteractionTranslator, ActivityPubInteractionTranslator>()
            .AddScoped<IActivityPubRequestHandler, ActivityPubRequestHandler>()
