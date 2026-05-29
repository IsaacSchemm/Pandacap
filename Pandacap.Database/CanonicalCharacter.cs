namespace Pandacap.Database
{
    public class CanonicalCharacter
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? FullName { get; set; }

        public Guid? SpeciesId { get; set; }

        public Guid? SettingId { get; set; }

        public string? Gender { get; set; }

        public string? Pronouns { get; set; }

        public List<string> NationalityIsoCodes { get; set; } = [];

        public string? Description { get; set; }

        public bool Original { get; set; }

        public bool Fan { get; set; }

        public class Relationship
        {
            public Guid OtherCharacterId { get; set; }

            public string RelationshipTypeName { get; set; } = "";
        }

        public List<Relationship> Relationships { get; set; } = [];

        public class AlternateVersion
        {
            public Guid OtherCharacterId { get; set; }
        }

        public List<AlternateVersion> AlternateVersions { get; set; } = [];

        public string? ShortCode { get; set; }
    }
}
