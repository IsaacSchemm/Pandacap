namespace Pandacap.Audio

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.Audio.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddAudioServices(serviceCollection: IServiceCollection) =
        serviceCollection
            .AddScoped<IAudioSplitter, AudioSplitter>()
