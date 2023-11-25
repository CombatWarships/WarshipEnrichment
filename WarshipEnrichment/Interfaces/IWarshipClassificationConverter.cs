namespace WarshipEnrichment.Interfaces
{
    public interface IWarshipClassificationConverter
    {
        string? FindKey(string text);

        Task<string?> FindKeyAsync(IEnumerable<string> text);
    }
}