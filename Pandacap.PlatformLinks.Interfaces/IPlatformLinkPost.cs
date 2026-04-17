namespace Pandacap.PlatformLinks.Interfaces
{
    public interface IPlatformLinkPost
    {
        string? ActivityPubObjectId { get; }

        string? BlueskyDID { get; }
        string? BlueskyRecordKey { get; }

        string? DeviantArtUrl { get; }

        int? FurAffinitySubmissionId { get; }
        int? FurAffinityJournalId { get; }

        int? WeasylSubmitId { get; }
        int? WeasylJournalId { get; }
    }
}
