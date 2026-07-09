using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class WeasylNote
    {
        [Key]
        public Guid Id { get; set; }

        public DateTimeOffset Time { get; set; }

        public string? Sender { get; set; }

        public string? SenderUrl { get; set; }
    }
}
