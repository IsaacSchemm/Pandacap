namespace Pandacap.ActivityPub.RemoteObjects

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ActivityPub.RemoteObjects.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddActivityPubRemoteObjectServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IActivityPubRemoteActorService, ActivityPubRemoteActorService>()
            .AddScoped<IActivityPubRemotePostService, ActivityPubRemotePostService>()
            .AddScoped<IWebFingerService, WebFingerService>()
