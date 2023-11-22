using System.Text.Json;
using WarshipEnrichment.Interfaces;
using WarshipRegistryAPI;

namespace WarshipEnrichment.Converters
{
    public class NationalityConverter : FlatListConverter<Nationality>, INationalityConverter
	{
		private INationalityAPI _nationalityAPI;

		public NationalityConverter(INationalityAPI nationalityAPI)
		{
			_nationalityAPI = nationalityAPI;
		}
	
	protected override object TierSelector(Nationality nation) => nation.Tier;

		protected override IEnumerable<string> AliasSelector(Nationality nation)
		{
			yield return nation.ID.ToLowerInvariant();
			yield return nation.DisplayName.ToLowerInvariant();

			if (!string.IsNullOrEmpty(nation.Aliases))
			{
				var aliases = JsonSerializer.Deserialize<string[]>(nation.Aliases);
				if (aliases != null)
				{
					foreach (var alias in aliases)
						yield return alias;
				}
			}
		}

		protected override Task<IEnumerable<Nationality>> GetDataSource()
		{
			return _nationalityAPI.GetAll();
		}
	}
}