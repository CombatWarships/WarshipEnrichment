using WarshipRegistryAPI;

namespace WarshipEnrichment.Interfaces
{
    public interface IWarshipClassificationConverter
    {
        Task<WarshipClassification?> Find(string text);
        Task<WarshipClassification?> Find(IEnumerable<string> text);
    }
}