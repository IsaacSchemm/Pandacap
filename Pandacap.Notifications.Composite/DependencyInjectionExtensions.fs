namespace Pandacap.Notifications.Composite

open System.Runtime.CompilerServices
open Microsoft.Extensions.DependencyInjection
open Pandacap.Notifications.Composite.Interfaces

[<Extension>]
module DependencyInjectionExtensions =
    [<Extension>]
    let AddCompositeNotificationHandler(serviceCollection: IServiceCollection) =
        serviceCollection.AddScoped<ICompositeNotificationHandler, CompositeNotificationHandler>()
