namespace WarshipEnrichmentAPI
{
	public interface IShipIdentity
	{
		Guid? ID { get; }
		string? WikiLink { get; }
		int? ShiplistKey { get; }
	}
}