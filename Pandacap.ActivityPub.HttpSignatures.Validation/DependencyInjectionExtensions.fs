namespace Pandacap.ActivityPub.HttpSignatures.Validation

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ActivityPub.HttpSignatures.Validation.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddActivityPubSignatureValidator(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IActivityPubSignatureValidator, ActivityPubSignatureValidator>()
