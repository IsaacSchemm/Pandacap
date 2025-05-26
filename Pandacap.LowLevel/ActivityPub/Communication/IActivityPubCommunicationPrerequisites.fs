namespace Pandacap.ActivityPub.Communication

open System
open System.Threading.Tasks

type IActivityPubCommunicationPrerequisites =
    abstract member UserAgent: string
    abstract GetPublicKeyAsync: unit -> Task<String>
    abstract SignRsaSha256Async: byte[] -> Task<byte[]>
