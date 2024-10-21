using System.ComponentModel.DataAnnotations;

namespace Pandacap.Models
{
    public class CreateJournalEntryViewModel : CreatePostViewModel
    {
        [Required]
        public string Title { get; set; } = "";
    }
}
