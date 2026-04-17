namespace Pandacap.UI.Elements
{
    public interface IFavorite : IPost
    {
        DateTimeOffset FavoritedAt { get; }
        DateTimeOffset? HiddenAt { get; set; }
    }
}
