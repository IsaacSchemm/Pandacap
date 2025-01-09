namespace Pandacap.LowLevel

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
module DependencyInjectionExtension =
    [<Extension>]
    let AddLowLevelServices(services: IServiceCollection) =
        services
            .AddScoped<Pandacap.ActivityPub.Mapper>()
            .AddScoped<Pandacap.ActivityPub.ProfileTranslator>()
            .AddScoped<Pandacap.ActivityPub.PostTranslator>()
            .AddScoped<Pandacap.ActivityPub.RelationshipTranslator>()
            .AddScoped<Pandacap.ActivityPub.InteractionTranslator>()
            .AddScoped<ComputerVisionProvider>()
