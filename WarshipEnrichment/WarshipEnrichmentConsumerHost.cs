using Microsoft.Extensions.Configuration;
using ServiceBus.Core;

namespace WarshipEnrichment
{
	public class WarshipEnrichmentConsumerHost : ServiceBusConsumerHost
	{
		public WarshipEnrichmentConsumerHost(IConfiguration configuration, IMessageProcessor messageProcessor)
			: base(configuration.GetConnectionString("EnrichmentServiceBus"), "warshipenrichment", messageProcessor)
		{
		}
	}
}
