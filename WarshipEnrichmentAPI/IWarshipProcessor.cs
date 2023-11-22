namespace WarshipEnrichmentAPI
{
	public interface IWarshipProcessor
	{
		Task PostWarship(IShipIdentity ship);
		Task PostWarships(IEnumerable<IShipIdentity> ships);
	}
}