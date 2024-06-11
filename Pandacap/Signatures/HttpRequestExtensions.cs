// Copyright (c) 2021, Unisys
//
// Adapted from NSign, used under the terms of the MIT license
// https://github.com/Unisys/NSign/commit/660b2412cd523ed175d387cf32f549065b3cc56f

using Microsoft.AspNetCore.Http.Extensions;
using NSign.Signatures;
using static NSign.Constants;

namespace Pandacap.Signatures;

internal static class HttpRequestExtensions
{
    public static string GetDerivedComponentValue(this HttpRequest request, DerivedComponent derivedComponent)
    {
        Lazy<Uri> uri = new(() => new(request.GetEncodedUrl()));

        return derivedComponent.ComponentName switch
        {
            DerivedComponents.SignatureParams =>
                throw new NotSupportedException("The '@signature-params' component cannot be included explicitly."),
            DerivedComponents.Method =>
                request.Method,
            DerivedComponents.TargetUri =>
                uri.Value.OriginalString,
            DerivedComponents.Authority =>
                uri.Value.Authority.ToLower(),
            DerivedComponents.Scheme =>
                uri.Value.Scheme.ToLower(),
            DerivedComponents.RequestTarget =>
                uri.Value.PathAndQuery,
            DerivedComponents.Path =>
                uri.Value.AbsolutePath,
            DerivedComponents.Query =>
                string.IsNullOrWhiteSpace(uri.Value.Query)
                    ? "?"
                    : uri.Value.Query,
            DerivedComponents.QueryParam =>
                throw new NotSupportedException("The '@query-param' component must have the 'name' parameter set."),
            DerivedComponents.Status =>
                throw new NotSupportedException("The '@status' component cannot be included in request signatures."),
            _ =>
                throw new NotSupportedException(
                    $"Non-standard derived signature component '{derivedComponent.ComponentName}' cannot be retrieved."),
        };
    }
}