using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using ServiceBus.Core;
using WarshipEnrichment;
using WarshipEnrichment.Converters;
using WarshipEnrichment.DataCollectors;
using WarshipEnrichment.Interfaces;
using WarshipEnrichmentAPI;
using WarshipImport.Interfaces;
using WarshipImport.Managers;
using WarshipRegistryAPI.Classification;
using WarshipRegistryAPI.Nationality;
using WarshipRegistryAPI.Warships;


var appInsightsConnection = "InstrumentationKey=09ce9924-198e-4315-b6c8-7885f28ec8e9;IngestionEndpoint=https://southcentralus-3.in.applicationinsights.azure.com/;LiveEndpoint=https://southcentralus.livediagnostics.monitor.azure.com/";

if (string.IsNullOrEmpty(appInsightsConnection))
	Log.Error("Application Insights Connection is NULL");

Log.Logger = new LoggerConfiguration()
	.Enrich.FromLogContext()
	.Enrich.WithProperty("ApplicationName", "WarshipImport")
	.WriteTo.ApplicationInsights(appInsightsConnection, new TraceTelemetryConverter())
	.WriteTo.Console()
	.CreateLogger();

Log.Information("Application started & Logger attached");


var builder = Host.CreateDefaultBuilder();

builder.ConfigureServices(services =>
{
	services.AddSingleton<IWarshipClassificationAPI, WarshipClassificationAPI>((sp) =>
	{
		return new WarshipClassificationAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("ClassificationAPI")!);
	});
	services.AddSingleton<INationalityAPI, NationalityAPI>((sp) =>
	{
		return new NationalityAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("NationalityAPI")!);
	});
	services.AddSingleton<IWarshipAPI, WarshipAPI>((sp) =>
	{
		return new WarshipAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("WarshipAPI")!);
	});
	services.AddSingleton<IConflictProcessorAPI, ConflictProcessorAPI>((sp) =>
	{
		return new ConflictProcessorAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("WarshipConflictsServiceBus")!);
	});
	services.AddSingleton<IAddWarshipAPI, AddWarshipAPI>((sp) =>
	{
		return new AddWarshipAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("AddShipServiceBus")!);
	});

	services.AddSingleton<IShipList, IrcwccShipList>();
	services.AddSingleton<IWikiShipFactory, WikiShipFactory>();
	services.AddSingleton<INationalityConverter, NationalityConverter>();
	services.AddSingleton<IWarshipClassificationConverter, WarshipClassificationConverter>();
	services.AddSingleton<IMessageProcessor, EnrichmentProcessor>();

	services.AddHostedService<WarshipEnrichmentConsumerHost>();
});

IHost app = builder.Build();
app.Start();