using System.Text.Json;
using WarshipEnrichment.Interfaces;
using WarshipRegistryAPI.Classification;

namespace WarshipEnrichment.Converters
{
    public class WarshipClassificationConverter : FlatListConverter<WarshipClassification>, IWarshipClassificationConverter
	{
		private readonly IWarshipClassificationAPI _warshipClassificationAPI;

		public WarshipClassificationConverter(IWarshipClassificationAPI warshipClassificationAPI) 
		{
			_warshipClassificationAPI = warshipClassificationAPI;
		}	

		public string? FindKey(string text)
		{
			var shipClass = Find(new string[] { text }).Result;

			return shipClass?.ID;
		}

		public async Task<string?> FindKeyAsync(IEnumerable<string> text)
		{
			return (await Find(text))?.ID;
		}

		protected override IEnumerable<string> AliasSelector(WarshipClassification type)
		{
			yield return type.ID.ToLowerInvariant();
			yield return type.DisplayName.ToLowerInvariant();

			if (!string.IsNullOrEmpty(type.Aliases))
			{
				var aliases = JsonSerializer.Deserialize<string[]>(type.Aliases);
				if (aliases != null)
				{
					foreach (var alias in aliases)
						yield return alias.ToLowerInvariant();
				}
			}
		}

		protected override Task<IEnumerable<WarshipClassification>> GetDataSource()
		{
			return _warshipClassificationAPI.GetAll();
		}

		protected override object TierSelector(WarshipClassification nation) => nation.ClassRank;
	}

}
