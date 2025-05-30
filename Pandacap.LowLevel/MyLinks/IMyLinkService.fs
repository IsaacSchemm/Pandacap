namespace Pandacap.LowLevel.MyLinks

open System.Threading
open System.Threading.Tasks

type IMyLinkService =
    abstract member GetLinksAsync: CancellationToken -> Task<MyLink list>
