using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class WeasylNotificationCollection
    {
        [Key]
        public Guid Id { get; set; }

        public class Notification
        {
            public string? NotificationId { get; set; }

            public string? PostUrl { get; set; }

            public DateTimeOffset Time { get; set; }

            public string? UserName { get; set; }

            public string? UserUrl { get; set; }
        }

        public List<Notification> Notifications { get; set; } = [];

        public class Note
        {
            public DateTimeOffset Time { get; set; }

            public string? Sender { get; set; }

            public string? SenderUrl { get; set; }
        }

        public List<Note> Notes { get; set; } = [];
    }
}
