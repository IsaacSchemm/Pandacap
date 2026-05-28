namespace Pandacap.Database
{
    public class CanonicalSpecies
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? Description { get; set; }

        public bool Original { get; set; }

        public bool Fan { get; set; }

        public class ParentSpecies
        {
            public Guid OtherSpeciesId { get; set; }
        }

        public List<ParentSpecies> PartOf { get; set; } = [];

        public string? ShortCode { get; set; }
    }
}
