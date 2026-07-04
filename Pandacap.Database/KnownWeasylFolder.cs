using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class KnownWeasylFolder
    {
        [Key]
        public int FolderId { get; set; }

        public string Name { get; set; } = "";
    }
}
