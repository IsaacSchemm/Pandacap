namespace Pandacap.LowLevel

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.ConfigurationObjects

[<Extension>]
module DependencyInjectionExtension =
    type private HostInformationProvider(appInfo: ApplicationInformation) = 
        interface Pandacap.ActivityPub.IHostInformationProvider with
            member _.ApplicationHostname = appInfo.ApplicationHostname

    [<Extension>]
    let AddLowLevelServices(services: IServiceCollection) =
        services
            .AddScoped<Pandacap.ActivityPub.Mapper>()
            .AddScoped<Pandacap.ActivityPub.ProfileTranslator>()
            .AddScoped<Pandacap.ActivityPub.PostTranslator>()
            .AddScoped<Pandacap.ActivityPub.RelationshipTranslator>()
            .AddScoped<Pandacap.ActivityPub.InteractionTranslator>()
            .AddScoped<ComputerVisionProvider>()
            .AddScoped<HostInformationProvider>()
