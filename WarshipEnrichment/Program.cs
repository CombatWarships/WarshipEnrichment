using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using WarshipEnrichment;
using WarshipEnrichment.Converters;
using WarshipImport.Interfaces;
using WarshipImport.Managers;
using WarshipRegistryAPI;

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

var connectionString = "Endpoint=sb://warshipservicebus.servicebus.windows.net/;SharedAccessKeyName=ListenPolicy;SharedAccessKey=vr3DMhgdaFTSzw5H5YUi4Oi4pJ5MQnvmM+ASbPYFd4g=;EntityPath=warshipenrichment";


builder.ConfigureServices(services =>
{
	services.AddSingleton<IWarshipClassificationAPI, WarshipClassificationAPI>((sp) =>
	{
		return new WarshipClassificationAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("WarshipRegistryUrl")!);
	});
	services.AddSingleton<INationalityAPI, NationalityAPI>((sp) =>
	{
		return new NationalityAPI(sp.GetRequiredService<IConfiguration>().GetConnectionString("WarshipRegistryUrl")!);
	});

	services.AddSingleton<IShipList, IrcwccShipList>();
	services.AddSingleton<IWikiShipFactory, WikiShipFactory>();
	services.AddSingleton<INationalityConverter, NationalityConverter>();

	services.AddSingleton<IServiceBusConsumer, ServiceBusConsumer>((sp) =>
	{
		return new ServiceBusConsumer(sp.GetRequiredService<IConfiguration>().GetConnectionString("EnrichmentServiceBus")!, new EnrichmentProcessor().ProcessMessage);
	});
});

var app = builder.Build();
var consumer = app.Services.GetRequiredService<IServiceBusConsumer>();
await consumer.Run();