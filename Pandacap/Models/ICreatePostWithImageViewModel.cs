using Pandacap.Data;

namespace Pandacap.Models
{
    public interface ICreatePostViewModel
    {
        PostType PostType { get; }

        string? Title { get; }

        string MarkdownBody { get; }

        string? Tags { get; }

        Guid? UploadId { get; }

        string? AltText { get; }

        bool FocusTop { get; }
    }
}
