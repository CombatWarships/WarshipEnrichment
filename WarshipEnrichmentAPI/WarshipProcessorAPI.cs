using ServiceBus.Core;
using ShipDomain;

namespace WarshipEnrichmentAPI
{
	public sealed class WarshipProcessorAPI : ServiceBusProducer<IShipIdentity>, IWarshipProcessorAPI
	{
		public WarshipProcessorAPI(string connectionString) :
			base(connectionString, "WarshipEnrichment")
		{ }

		public Task PostWarship(IShipIdentity ship)
		{
			return PostMessage(ship);
		}

		public Task PostWarships(IEnumerable<IShipIdentity> ships)
		{
			return PostMessages(ships);
		}
	}
}
