using System.ComponentModel.DataAnnotations;

namespace Pandacap.Database
{
    public class OfflinePlatformDataCacheItem
    {
        public enum CachedPlatformDataType
        {
            FurAffinityGalleryFolders = 1,
            FurAffinityPostOptions = 2,
            WeasylGalleryFolders = 3
        }

        [Key]
        public CachedPlatformDataType Type { get; set; }

        public string Json { get; set; } = "";
    }
}
