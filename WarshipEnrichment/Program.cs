using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using WarshipEnrichment;


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


var connectionString = "Endpoint=sb://warshipservicebus.servicebus.windows.net/;SharedAccessKeyName=ListenPolicy;SharedAccessKey=vr3DMhgdaFTSzw5H5YUi4Oi4pJ5MQnvmM+ASbPYFd4g=;EntityPath=warshipenrichment";

using (var consumer = new ServiceBusConsumer(connectionString, new EnrichmentProcessor().ProcessMessage))
	await consumer.Run();