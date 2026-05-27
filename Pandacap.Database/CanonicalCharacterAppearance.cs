namespace Pandacap.Database
{
    public class CanonicalCharacterAppearance
    {
        public Guid Id { get; set; }

        public Guid PostId { get; set; }

        public Guid CharacterId { get; set; }

        public Guid? SpeciesId { get; set; }

        public bool Background { get; set; }
    }
}
