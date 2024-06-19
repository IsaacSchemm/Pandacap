namespace Pandacap
{
    public class AltTextSentinel
    {
        private readonly Dictionary<Guid, string?> _altText = [];

        private readonly Guid _guid = Guid.NewGuid();

        public bool TryGetAltText(Guid guid, out string? altText)
        {
            return _altText.TryGetValue(guid, out altText);
        }

        public void Add(Guid guid, string? altText)
        {
            _altText[guid] = altText;
        }

        public override string ToString()
        {
            return $"{base.ToString()} ({_guid})";
        }
    }
}
