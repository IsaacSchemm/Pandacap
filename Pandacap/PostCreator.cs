using FSharp.Data;
using Microsoft.EntityFrameworkCore;
using Pandacap.Data;
using Pandacap.Html;

namespace Pandacap
{
    public class PostCreator(
        DeliveryInboxCollector deliveryInboxCollector,
        PandacapDbContext context,
        IHttpClientFactory httpClientFactory,
        ActivityPub.PostTranslator postTranslator)
    {
        public interface IViewModel
        {
            PostType PostType { get; }

            string? Title { get; }

            string? MarkdownBody { get; }

            string? Tags { get; }

            Guid? UploadId { get; }

            string? LinkUrl { get; }

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

                if (upload.Raster is Guid r)
                {
                    var raster = await context.Uploads
                        .Where(i => i.Id == r)
                        .SingleAsync(cancellationToken);

                    renditions.Add(new()
                    {
                        Id = raster.Id,
                        ContentType = raster.ContentType
                    });

                    context.Remove(raster);
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

            if (model.LinkUrl is string linkUrl)
            {
                try
                {
                    using var client = httpClientFactory.CreateClient();
                    using var resp = await client.GetAsync(linkUrl, cancellationToken);
                    var html = await resp.EnsureSuccessStatusCode().Content.ReadAsStringAsync(cancellationToken);

                    var metadata = Scraper.GetOpenGraphMetadata(html);

                    post.Links = [new()
                    {
                        Url = linkUrl,
                        SiteName = metadata.TryGetValue("og:site_name", out string? siteName)
                            ? siteName
                            : resp.RequestMessage?.RequestUri?.Host,
                        Title = metadata.TryGetValue("og:title", out string? title)
                            ? title
                            : Scraper.GetTitleFromHtml(html),
                        Image = metadata.TryGetValue("og:image", out string? image)
                            ? image
                            : null,
                        Description = metadata.TryGetValue("og:description", out string? description)
                            ? description
                            : null
                    }];

                    await context.SaveChangesAsync(cancellationToken);
                }
                catch (Exception)
                {
                    post.Links = [new()
                    {
                        Url = new Uri(linkUrl).Host
                    }];
                }
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
