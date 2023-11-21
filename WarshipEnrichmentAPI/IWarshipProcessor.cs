namespace WarshipEnrichmentAPI
{
	public interface IWarshipProcessor
	{
		Task PostWarships(IEnumerable<IShip> ships);
	}
}