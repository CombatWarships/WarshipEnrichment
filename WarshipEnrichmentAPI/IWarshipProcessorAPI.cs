using ShipDomain;

namespace WarshipEnrichmentAPI
{
	public interface IWarshipProcessorAPI
	{
		Task PostWarship(IShipIdentity ship);
		Task PostWarships(IEnumerable<IShipIdentity> ships);
	}
}