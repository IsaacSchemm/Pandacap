using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class WeasylNotification
    {
        [Key]
        public Guid Id { get; set; }

        public string? NotificationId { get; set; }

        public string? PostUrl { get; set; }

        public DateTimeOffset Time { get; set; }

        public string? UserName { get; set; }

        public string? UserUrl { get; set; }
    }
}
