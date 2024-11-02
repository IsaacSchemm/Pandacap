﻿using Pandacap.Data;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateStatusUpdateViewModel : ICreatePostViewModel
    {
        [Required]
        [DisplayName("Body (Markdown)")]
        public string MarkdownBody { get; set; } = "";

        [DisplayName("Tags (space-separated, e.g. tag1 tag2 tag3)")]
        public string? Tags { get; set; }

        PostType ICreatePostViewModel.PostType => PostType.StatusUpdate;

        string? ICreatePostViewModel.Title => null;

        Guid? ICreatePostViewModel.UploadId => null;

        string? ICreatePostViewModel.AltText => null;

        bool ICreatePostViewModel.FocusTop => false;
    }
}
