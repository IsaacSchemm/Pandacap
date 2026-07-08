using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class FurAffinityNotification
    {
        [Key]
        public Guid Id { get; set; }

        public DateTimeOffset Time { get; set; }

        public string? Text { get; set; }
    }
}
