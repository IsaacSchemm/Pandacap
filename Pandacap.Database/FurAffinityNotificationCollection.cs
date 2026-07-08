using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class FurAffinityNotificationCollection
    {
        [Key]
        public Guid Id { get; set; }

        public class Notification
        {
            public DateTimeOffset Time { get; set; }

            public string? Text { get; set; }
        }

        public List<Notification> Notifications { get; set; } = [];

        public class Note
        {
            public int NoteId { get; set; }

            public DateTimeOffset Time { get; set; }

            public string? UserDisplayName { get; set; }
        }

        public List<Note> Notes { get; set; } = [];
    }
}
