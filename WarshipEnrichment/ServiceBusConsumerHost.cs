﻿using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WarshipEnrichment
{
	public interface IMessageProcessor
	{
		Task ProcessMessage(string message);
	}

	public sealed class ServiceBusConsumerHost : IHostedService
	{
		private readonly ServiceBusClient _client;
		private readonly ServiceBusProcessor _processor;

		private readonly TaskCompletionSource _isRunning = new TaskCompletionSource();
		private readonly IMessageProcessor _messageProcessor;

		public ServiceBusConsumerHost(IConfiguration configuration, IMessageProcessor messageProcessor)
		{
			string? connectionString = configuration.GetConnectionString("EnrichmentServiceBus");

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


		async Task IHostedService.StartAsync(CancellationToken cancellationToken)
		{
			// add handler to process messages
			_processor.ProcessMessageAsync += MessageHandler;

			// add handler to process any errors
			_processor.ProcessErrorAsync += ErrorHandler;

			// start processing 
			await _processor.StartProcessingAsync();

			await _isRunning.Task;
		}

		async Task IHostedService.StopAsync(CancellationToken cancellationToken)
		{
			try
			{
				Console.WriteLine("\nStopping the receiver...");
				_processor.StopProcessingAsync().Wait();
				Console.WriteLine("Stopped receiving messages");
			}
			finally
			{
				await _processor.DisposeAsync();
				await _client.DisposeAsync();

				_isRunning.SetResult();
			}
		}


		// handle received messages
		private async Task MessageHandler(ProcessMessageEventArgs args)
		{
			string body = args.Message.Body.ToString();
			Console.WriteLine($"Received: {body}");

			await _messageProcessor.ProcessMessage(body);

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
	}
}