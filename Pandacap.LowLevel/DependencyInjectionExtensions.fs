namespace Pandacap.LowLevel

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection

[<Extension>]
module DependencyInjectionExtension =
    [<Extension>]
    let AddLowLevelServices(services: IServiceCollection) =
        services
            .AddScoped<ActivityPubTranslator>()
            .AddScoped<ComputerVisionProvider>()
            .AddScoped<IdMapper>()
