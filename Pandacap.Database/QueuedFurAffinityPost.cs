using Pandacap.FurAffinity.Models;
using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class QueuedFurAffinityPost
    {
        [Key]
        public Guid PostId { get; set; }

        public int Cat { get; set; }

        public int Atype { get; set; }

        public int Species { get; set; }

        public int Gender { get; set; }

        public Rating Rating { get; set; }

        public bool Scrap { get; set; }

        public bool LockComments { get; set; }

        public List<long> FolderIds { get; set; } = [];
    }
}
