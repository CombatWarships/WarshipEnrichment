using WarshipRegistryAPI;

namespace WarshipEnrichment.Interfaces
{
    public interface INationalityConverter
    {
        Task<Nationality?> Find(IEnumerable<string> text);
    }
}