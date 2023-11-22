using Azure.Messaging.ServiceBus;

namespace WarshipEnrichment
{
	public sealed class ServiceBusConsumer : IDisposable
	{
		private readonly ServiceBusClient _client;
		private readonly ServiceBusProcessor _processor;

		private readonly TaskCompletionSource _isRunning = new TaskCompletionSource();
		private readonly Func<string, Task> _messageProcessor;

		public ServiceBusConsumer(string connectionString, Func<string, Task> messageProcessor)
		{
			if (string.IsNullOrEmpty(connectionString))
				throw new ArgumentException($"'{nameof(connectionString)}' cannot be null or empty.", nameof(connectionString));
			
			_messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));


			var clientOptions = new ServiceBusClientOptions()
			{
				TransportType = ServiceBusTransportType.AmqpWebSockets
			};
			_client = new ServiceBusClient(connectionString, clientOptions);
			_processor = _client.CreateProcessor("WarshipEnrichment", new ServiceBusProcessorOptions());
		}

		public async Task Run()
		{
			// add handler to process messages
			_processor.ProcessMessageAsync += MessageHandler;

			// add handler to process any errors
			_processor.ProcessErrorAsync += ErrorHandler;

			// start processing 
			await _processor.StartProcessingAsync();

			await _isRunning.Task;
		}

		// handle received messages
		private async Task MessageHandler(ProcessMessageEventArgs args)
		{
			string body = args.Message.Body.ToString();
			Console.WriteLine($"Received: {body}");

			await _messageProcessor.Invoke(body);

			// complete the message. message is deleted from the queue. 
			await args.CompleteMessageAsync(args.Message);
		}

		// handle any errors when receiving messages
		private Task ErrorHandler(ProcessErrorEventArgs args)
		{
			// TODO: Add Logger
			Console.WriteLine(args.Exception.ToString());
			return Task.CompletedTask;
		}

		void IDisposable.Dispose()
		{
			try
			{
				Console.WriteLine("\nStopping the receiver...");
				_processor.StopProcessingAsync().Wait();
				Console.WriteLine("Stopped receiving messages");
			}
			finally
			{
				_processor.DisposeAsync().GetAwaiter().GetResult();
				_client.DisposeAsync().GetAwaiter().GetResult();

				_isRunning.SetResult();
			}
		}
	}
}