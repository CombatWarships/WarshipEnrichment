using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace WarshipEnrichmentAPI
{
	public sealed class WarshipProcessor : IWarshipProcessor, IDisposable
	{
		private readonly ServiceBusClient _client;
		private readonly ServiceBusSender _sender;

		public WarshipProcessor(string connectionString)
		{
			if (string.IsNullOrEmpty(connectionString))
				throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));

			var clientOptions = new ServiceBusClientOptions()
			{
				TransportType = ServiceBusTransportType.AmqpWebSockets
			};
			_client = new ServiceBusClient(connectionString, clientOptions);
			_sender = _client.CreateSender("WarshipEnrichment");
		}

		public async Task PostWarships(IEnumerable<IShip> ships)
		{
			// create a batch 
			ServiceBusMessageBatch messageBatch = await _sender.CreateMessageBatchAsync();
			try
			{
				foreach (var ship in ships)
				{
					var json = JsonSerializer.Serialize(ship);

					// if it is too large for the batch, send the batch as it is.
					if (!messageBatch.TryAddMessage(new ServiceBusMessage(json)))
					{

						// Batch is full, send what we have.
						await _sender.SendMessagesAsync(messageBatch);
						Console.WriteLine($"A parital batch of {ships.Count()} ships has been published to the queue.");

						// Create a new batch and continue processing.
						messageBatch.Dispose();
						messageBatch = await _sender.CreateMessageBatchAsync();
					}
				}

				// All messages have been queued, send them.
				await _sender.SendMessagesAsync(messageBatch);
				Console.WriteLine($"The final batch of {ships.Count()} ships has been published to the queue.");
			}
			finally
			{
				messageBatch.Dispose();
			}
		}

		void IDisposable.Dispose()
		{
			_sender.DisposeAsync().GetAwaiter().GetResult();
			_client.DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
