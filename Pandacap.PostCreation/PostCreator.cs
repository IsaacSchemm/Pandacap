using Microsoft.EntityFrameworkCore;
using Pandacap.ActivityPub.Outbox.Interfaces;
using Pandacap.ActivityPub.Services.Interfaces;
using Pandacap.Database;
using Pandacap.PostCreation.Interfaces;
using Pandacap.Text;
using Pandacap.VectorSearch.Interfaces;

namespace Pandacap.PostCreation
{
    internal class PostCreator(
        IDeliveryInboxCollector deliveryInboxCollector,
        IHttpClientFactory httpClientFactory,
        PandacapDbContext pandacapDbContext,
        IActivityPubPostTranslator postTranslator,
        IVectorSearchIndexClient vectorSearchIndexClient) : IPostCreator
    {
        public async Task<Guid> CreatePostAsync(
            INewPost model,
            CancellationToken cancellationToken)
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
                var upload = await pandacapDbContext.Uploads
                    .Where(i => i.Id == uid)
                    .SingleAsync(cancellationToken);

                pandacapDbContext.Remove(upload);

                List<Post.Image.BlobRef> renditions = [new()
                {
                    Id = upload.Id,
                    ContentType = upload.ContentType
                }];

                if (upload.Raster is Guid r)
                {
                    var raster = await pandacapDbContext.Uploads
                        .Where(i => i.Id == r)
                        .SingleAsync(cancellationToken);

                    renditions.Add(new()
                    {
                        Id = raster.Id,
                        ContentType = raster.ContentType
                    });

                    pandacapDbContext.Remove(raster);
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

                    var metadata = HtmlScraper.GetOpenGraphMetadata(html);

                    post.Links = [new()
                    {
                        Url = linkUrl,
                        SiteName = metadata.TryGetValue("og:site_name", out string? siteName)
                            ? siteName
                            : resp.RequestMessage?.RequestUri?.Host,
                        Title = metadata.TryGetValue("og:title", out string? title)
                            ? title
                            : HtmlScraper.GetTitle(html),
                        Image = metadata.TryGetValue("og:image", out string? image)
                            ? image
                            : null,
                        Description = metadata.TryGetValue("og:description", out string? description)
                            ? description
                            : null
                    }];

                    await pandacapDbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception)
                {
                    post.Links = [new()
                    {
                        Url = new Uri(linkUrl).Host
                    }];
                }
            }

            pandacapDbContext.Posts.Add(post);

            if (post.Type != Post.PostType.Scraps)
            {
                foreach (string inbox in await deliveryInboxCollector.GetDeliveryInboxesAsync(
                    isCreate: true,
                    cancellationToken: cancellationToken))
                {
                    pandacapDbContext.ActivityPubOutboundActivities.Add(new()
                    {
                        Id = Guid.NewGuid(),
                        JsonBody = postTranslator.BuildObjectCreate(post),
                        Inbox = inbox,
                        StoredAt = DateTimeOffset.UtcNow
                    });
                }
            }

            await pandacapDbContext.SaveChangesAsync(cancellationToken);

            await vectorSearchIndexClient.IndexAllAsync(
                AsyncEnumerable.Repeat(post, 1),
                cancellationToken);

            return id;
        }
    }
}
