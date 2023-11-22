namespace WarshipEnrichment.Converters
{
	public abstract class FlatListConverter<T>
	{
		private List<Tuple<string, T>>? _lookup;

		protected abstract Task<IEnumerable<T>> GetDataSource();

		protected abstract object TierSelector(T item);

		protected abstract IEnumerable<string> AliasSelector(T item);


		protected async Task<T?> Find(IEnumerable<string> text)
		{
			var lookup = await GetLookup();

			List<T> operators = new List<T>();
			foreach (var line in text)
			{
				foreach (var kvp in lookup)
				{
					if (line.Contains(kvp.Item1, StringComparison.OrdinalIgnoreCase))
					{
						operators.Add(kvp.Item2);
					}
				}
			}

			return operators.OrderBy(TierSelector).FirstOrDefault();
		}

		private async Task<List<Tuple<string, T>>> GetLookup()
		{
			if (_lookup == null)
			{
				var fullList = await GetDataSource();
				if (fullList?.Count() > 0)
				{
					var lookup = new List<Tuple<string, T>>();

					foreach (var item in fullList)
					{
						var aliases = AliasSelector(item);

						foreach (string alias in aliases)
							lookup.Add(Tuple.Create(alias.ToLowerInvariant(), item));
					}

					_lookup = lookup;
				}
			}

			return _lookup ?? new List<Tuple<string, T>>();
		}
	}
}