using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class KnownFurAffinityFolder
    {
        [Key]
        public long FolderId { get; set; }

        public string Name { get; set; } = "";
    }
}
