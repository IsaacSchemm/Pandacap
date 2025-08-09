using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using System;
using static System.Reflection.Metadata.BlobBuilder;

namespace Pandacap
{
    public class PostCreator(
        BlobServiceClient blobServiceClient,
        DeliveryInboxCollector deliveryInboxCollector,
        PandacapDbContext context,
        ActivityPub.PostTranslator postTranslator,
        SvgRenderer svgRenderer)
    {
        public interface IViewModel
        {
            PostType PostType { get; }

            string? Title { get; }

            string MarkdownBody { get; }

            string? Tags { get; }

            Guid? UploadId { get; }

            string? AltText { get; }

            bool FocusTop { get; }
        }

        public async Task<Guid> CreatePostAsync(
            IViewModel model,
            CancellationToken cancellationToken = default)
        {
            Guid id = Guid.NewGuid();

            var tags = (model.Tags ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.TrimStart('#'))
                .Select(tag => tag.TrimEnd(','))
                .Distinct();

            var post = new Post
            {
                Body = model.MarkdownBody,
                Id = id,
                Images = [],
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = [.. tags],
                Title = model.Title,
                Type = model.PostType
            };

            if (model.UploadId is Guid uid)
            {
                var upload = await context.Uploads
                    .Where(i => i.Id == uid)
                    .SingleAsync(cancellationToken);

                context.Remove(upload);

                List<PostBlobRef> renditions = [new()
                {
                    Id = upload.Id,
                    ContentType = upload.ContentType
                }];

                if (upload.ContentType == "image/svg+xml")
                {
                    var response = await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{upload.Id}")
                        .DownloadStreamingAsync(cancellationToken: cancellationToken);

                    using var svgStream = response.Value.Content;
                    using var pngStream = new MemoryStream();

                    svgRenderer.RenderPng(svgStream, pngStream);

                    pngStream.Position = 0;

                    var pngBlobId = Guid.NewGuid();

                    await blobServiceClient
                        .GetBlobContainerClient("blobs")
                        .GetBlobClient($"{pngBlobId}")
                        .UploadAsync(pngStream, cancellationToken);

                    renditions.Add(new()
                    {
                        Id = pngBlobId,
                        ContentType = "image/png"
                    });
                }

                post.Images = [new()
                {
                    Renditions = renditions,
                    AltText = model.AltText ?? upload.AltText,
                    FocalPoint = new()
                    {
                        Horizontal = 0,
                        Vertical = model.FocusTop ? 1 : 0
                    }
                }];
            }

            context.Posts.Add(post);

            if (post.Type != PostType.Scraps)
            {
                foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                    isCreate: true,
                    cancellationToken: cancellationToken))
                {
                    context.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        JsonBody = ActivityPub.Serializer.SerializeWithContext(
                            postTranslator.BuildObjectCreate(
                                post)),
                        Inbox = inbox,
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            return id;
        }
    }
}
