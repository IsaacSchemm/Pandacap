using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace Pandacap.HighLevel.Signatures
{
    /// <summary>
    /// An abstraction of request data used to validate incoming HTTP signatures.
    /// </summary>
    public interface IRequest
    {
        /// <summary>
        /// The HTTP method (e.g. GET, POST).
        /// </summary>
        HttpMethod Method { get; }

        /// <summary>
        /// The request URI (e.g. https://pandacap.example.com/api/actor/inbox).
        /// </summary>
        Uri RequestUri { get; }

        /// <summary>
        /// A collection of request headers.
        /// </summary>
        IHeaderDictionary Headers { get; }
    }
}
