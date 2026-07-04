namespace Pandacap.FurAffinity

open System
open System.Net.Http

type internal IFurAffinityHttpHandlerProvider =
    abstract member GetOrCreateHandler: unit -> HttpMessageHandler

type internal FurAffinityHttpHandlerProvider() =
    let handler = lazy new SocketsHttpHandler(
        UseCookies = false,
        PooledConnectionLifetime = TimeSpan.FromMinutes(5L))

    interface IFurAffinityHttpHandlerProvider with
        member _.GetOrCreateHandler() = handler.Value
