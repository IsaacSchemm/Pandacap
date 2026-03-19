module private Http

open System
open System.Net.Http

let handler = lazy new SocketsHttpHandler(
    UseCookies = false,
    PooledConnectionLifetime = TimeSpan.FromMinutes(5L))
