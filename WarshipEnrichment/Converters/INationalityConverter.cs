using WarshipRegistryAPI;

namespace WarshipEnrichment.Converters
{
	public interface INationalityConverter
	{
		Task<Nationality?> FindNationality(string[] html);
	}
}