namespace Pandacap.ActivityPub.Services

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ActivityPub.Services.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddActivityPubServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IActivityPubProfileTranslator, ActivityPubProfileTranslator>()
            .AddScoped<IActivityPubPostTranslator, ActivityPubPostTranslator>()
            .AddScoped<IActivityPubRelationshipTranslator, ActivityPubRelationshipTranslator>()
            .AddScoped<IActivityPubInteractionTranslator, ActivityPubInteractionTranslator>()
            .AddScoped<IJsonLdExpansionService, JsonLdExpansionService>()
            .AddScoped<IActivityPubRequestHandler, ActivityPubRequestHandler>()
