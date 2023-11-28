using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;
using ServiceBus.Core;
using System.Diagnostics;
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

//Log.Logger = new LoggerConfiguration()
//		 .WriteTo.Console()
//		 .CreateBootstrapLogger();
//Log.Information("Application Started");

if (Debugger.IsAttached)
	await Task.Delay(5000);

IHostBuilder builder = Host.CreateDefaultBuilder();
//Log.Information("Builder created");


builder.ConfigureAppConfiguration((context, cd) =>
{
	var settings = cd.Build();

	TokenCredential keyvaultCredential = new DefaultAzureCredential();
	if (Debugger.IsAttached)
	{
		var tenantID = settings.GetSection("tenantID").Value;
		var clientID = settings.GetSection("clientID").Value;
		var clientSecret = settings.GetSection("clientSecret").Value;

		keyvaultCredential = new ClientSecretCredential(tenantID, clientID, clientSecret);
	}

	var kvName = settings.GetSection("KeyVaultName").Value;
	var url = $"https://{kvName}.vault.azure.net/";
	cd.AddAzureKeyVault(new Uri(url), keyvaultCredential);

	settings = cd.Build();

	var appInsightsConnection = settings.GetSection("AppInsights").Value;

	if (string.IsNullOrEmpty(appInsightsConnection))
		Log.Error("Application Insights Connection is NULL");

	Log.Logger = new LoggerConfiguration()
		.Enrich.FromLogContext()
		.Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name)
		.WriteTo.ApplicationInsights(appInsightsConnection, new TraceTelemetryConverter())
		.WriteTo.Console()
		.CreateLogger();
});



builder.ConfigureServices(services =>
{
	

	Log.Information("Application started & Logger attached");

	services.AddSingleton<IWarshipClassificationAPI, WarshipClassificationAPI>((sp) =>
	{
		return new WarshipClassificationAPI(sp.GetRequiredService<IConfiguration>()["ClassificationAPI"]!);
	});
	services.AddSingleton<INationalityAPI, NationalityAPI>((sp) =>
	{
		return new NationalityAPI(sp.GetRequiredService<IConfiguration>()["NationalityAPI"]!);
	});
	services.AddSingleton<IWarshipAPI, WarshipAPI>((sp) =>
	{
		return new WarshipAPI(sp.GetRequiredService<IConfiguration>()["WarshipAPI"]!);
	});
	services.AddSingleton<IConflictProcessorAPI, ConflictProcessorAPI>((sp) =>
	{
		return new ConflictProcessorAPI(sp.GetRequiredService<IConfiguration>()["WarshipConflictsServiceBus"]!);
	});
	services.AddSingleton<IAddWarshipAPI, AddWarshipAPI>((sp) =>
	{
		return new AddWarshipAPI(sp.GetRequiredService<IConfiguration>()["AddShipsServiceBusPost"]!);
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