namespace Pandacap.JsonLd

open System.Net

type Addressee =
| Public
| FoundActor of RemoteActor
| NotFoundActor of id: string * statusCode: HttpStatusCode option
