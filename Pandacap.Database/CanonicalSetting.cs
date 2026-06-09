namespace Pandacap.Database
{
    public class CanonicalSetting
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public bool Original { get; set; }

        public bool Fan { get; set; }
    }
}
