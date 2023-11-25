
namespace WarshipEnrichment.Interfaces
{
    public interface INationalityConverter
	{
		string? FindKey(IEnumerable<string> text);
		Task<string?> FindKeyAsync(IEnumerable<string> text);
	}
}