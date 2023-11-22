using System.Text.Json;
using WarshipRegistryAPI;

namespace WarshipEnrichment.Converters
{
	public class NationalityConverter
	{
		private readonly INationalityAPI _nationalityAPI;
		private List<Tuple<string, Nationality>>? _nationLookup;

		public NationalityConverter(INationalityAPI nationalityAPI)
		{
			_nationalityAPI = nationalityAPI;
		}

		public async Task<Nationality?> FindNationality(string[] html)
		{
			var nationLookup = await GetNationLookup();

			List<Nationality> operators = new List<Nationality>();
			foreach (var line in html)
			{
				foreach (var kvp in nationLookup)
				{
					if (line.Contains(kvp.Item1, StringComparison.OrdinalIgnoreCase))
					{
						operators.Add(kvp.Item2);
					}
				}
			}

			return operators.OrderBy(x => x.Tier).FirstOrDefault();
		}

		private async Task<List<Tuple<string, Nationality>>> GetNationLookup()
		{
			if (_nationLookup == null)
			{
				var nations = await _nationalityAPI.GetAll();
				if (nations?.Count() > 0)
				{
					var nationLookup = new List<Tuple<string, Nationality>>();

					foreach (var nation in nations)
					{
						nationLookup.Add(Tuple.Create(nation.ID.ToLowerInvariant(), nation));
						nationLookup.Add(Tuple.Create(nation.DisplayName.ToLowerInvariant(), nation));

						if (!string.IsNullOrEmpty(nation.Aliases))
						{
							var aliases = JsonSerializer.Deserialize<string[]>(nation.Aliases);
							if (aliases != null)
							{
								foreach (var alias in aliases)
									nationLookup.Add(Tuple.Create(alias.ToLowerInvariant(), nation));
							}
						}
					}

					_nationLookup = nationLookup;
				}
			}

			return _nationLookup ?? new List<Tuple<string, Nationality>>();
		}
	}
}