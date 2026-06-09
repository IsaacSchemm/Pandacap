using Pandacap.ActivityPub.Replies.Interfaces;
using Pandacap.Database;
using Pandacap.PlatformLinks.Interfaces;

namespace Pandacap.Models
{
    public class UserPostViewModel : IProfileHeadingModel
    {
        public required IReadOnlyList<IPlatformLink> PlatformLinks { get; init; }

        public required Post Post { get; init; }

        public required IReadOnlyList<IReply> Replies { get; init; }

        public required IReadOnlyList<CanonicalMediumApplicationModel> MediumApplications { get; init; }

        public required IReadOnlyList<CanonicalCharacterAppearanceModel> CharacterAppearances { get; init; }

        public IEnumerable<CanonicalCharacterAppearanceModel> ForegroundCharacterAppearances =>
            CharacterAppearances.Where(a => !a.Background);

        public IEnumerable<CanonicalCharacterAppearanceModel> BackgroundCharacterAppearances =>
            CharacterAppearances.Where(a => a.Background);
    }
}
