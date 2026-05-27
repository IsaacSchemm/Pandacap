namespace Pandacap.Database
{
    public class CanonicalCharacter
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = "";

        public string? FullName { get; set; }

        public Guid? SpeciesId { get; set; }

        public string? Gender { get; set; }

        public string? Pronouns { get; set; }

        public class CanonicalCharacterNationality
        {
            public string IsoCode { get; set; } = "";
        }

        public List<CanonicalCharacterNationality> Nationalities { get; set; } = [];

        public string? Description { get; set; }

        public class CanonicalCharacterRelationship
        {
            public Guid OtherCharacterId { get; set; }

            public string RelationshipTypeName { get; set; } = "";
        }

        public List<CanonicalCharacterRelationship> Relationships { get; set; } = [];

        public List<Guid> AlternateVersions { get; set; } = [];

        public string? ShortCode { get; set; }
    }
}
