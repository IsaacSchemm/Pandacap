namespace Pandacap.Database.Extensions

open System
open System.Net
open System.Runtime.CompilerServices
open Pandacap.Database

[<Extension>]
module PostExtensions =
    [<Extension>]
    let GenerateLinksHtml(post: Post) = String.concat "" [
        if not (isNull post.Links) then
            for link in post.Links do
                "<blockquote>"

                if not (String.IsNullOrEmpty(link.Title)) then
                    "<p>"
                    "<strong>"
                    WebUtility.HtmlEncode(link.Title)
                    "</strong>"
                    "</p>"

                if not (String.IsNullOrEmpty(link.Description)) then
                    "<p>"
                    WebUtility.HtmlEncode(link.Description)
                    "</p>"

                "<p>"
                $"<a href='{link.Url}' target='_blank'>"
                WebUtility.HtmlEncode(link.Url)
                "</a>"
                "</p>"

                "</blockquote>"
    ]

    [<Extension>]
    let GenerateShortPlainText(post: Post) = String.concat "\n\n" [
        if not (String.IsNullOrWhiteSpace(post.Title)) then
            $"{post.Title}"
        else if not (String.IsNullOrWhiteSpace(post.Body)) then
            post.Body

        String.concat " " [
            for tag in post.Tags do
                $"#{tag}"
        ]
    ]

    [<Extension>]
    let GenerateLongPlainText(post: Post) = String.concat "\n\n" [
        if not (String.IsNullOrWhiteSpace(post.Title)) then
            $"{post.Title}"

        for image in post.Images do
            if not (String.IsNullOrWhiteSpace(image.AltText)) then
                $"{image.AltText}"

        if not (String.IsNullOrWhiteSpace(post.Body)) then
            post.Body

        for link in post.Links do
            $"Link: {link.Title} ({link.Url})"

        String.concat " " [
            for tag in post.Tags do
                $"#{tag}"
        ]
    ]
