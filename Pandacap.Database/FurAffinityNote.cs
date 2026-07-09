using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class FurAffinityNote
    {
        [Key]
        public Guid Id { get; set; }

        public int NoteId { get; set; }

        public DateTimeOffset Time { get; set; }

        public string? UserDisplayName { get; set; }
    }
}
