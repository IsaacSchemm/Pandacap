using Microsoft.EntityFrameworkCore;
using Pandacap.Data;

namespace Pandacap
{
    public class PostCreator(
        PandacapDbContext context)
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

            var post = new Post
            {
                Body = model.MarkdownBody,
                Id = id,
                Images = [],
                PublishedTime = DateTimeOffset.UtcNow,
                Tags = (model.Tags ?? "")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(tag => tag.TrimStart('#'))
                    .Select(tag => tag.TrimEnd(','))
                    .Distinct()
                    .ToList(),
                Title = model.Title,
                Type = model.PostType
            };

            if (model.UploadId is Guid uid)
            {
                var upload = await context.Uploads
                    .Where(i => i.Id == uid)
                    .SingleAsync(cancellationToken);

                context.Remove(upload);

                post.Images = [new()
                {
                    Blob = new()
                    {
                        Id = upload.Id,
                        ContentType = upload.ContentType
                    },
                    AltText = model.AltText ?? upload.AltText,
                    FocalPoint = new()
                    {
                        Horizontal = 0,
                        Vertical = model.FocusTop ? 1 : 0
                    }
                }];
            }

            context.Posts.Add(post);

            await context.SaveChangesAsync(cancellationToken);

            return id;
        }
    }
}
