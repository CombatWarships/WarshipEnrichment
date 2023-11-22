using Serilog;
using Serilog.Context;
using System.Text.Json;
using WarshipEnrichment.DTOs;
using WarshipImport.Data;
using WarshipImport.Interfaces;
using WarshipImport.Managers;

namespace WarshipEnrichment
{

	public class EnrichmentProcessor:IMessageProcessor
	{
		private readonly IShipList _shipList;
		private readonly IWikiShipFactory _wikiShipFactory;

		public EnrichmentProcessor(IShipList shipList, IWikiShipFactory wikiShipFactory)
		{
			_shipList = shipList;
			_wikiShipFactory = wikiShipFactory;
		}

		public async Task ProcessMessage(string message)
		{
			using (LogContext.PushProperty("MessageJSON", message))
			{
				var shipIdentity = JsonSerializer.Deserialize<ShipIdentity>(message);

				if (shipIdentity == null)
				{
					Log.Error("ShipIdentity was null");
					return;
				}
				Log.Information("Deserialized to Ship Identity");

				var finalShip = new Ship();
				bool isValidShip = true;
				List<Task> tasks = new List<Task>();

				if(shipIdentity.ShiplistKey != null) 
				{
					var t =_shipList.FindShip(shipIdentity.ShiplistKey.Value).ContinueWith(res =>
					{
						var ircShip = res.Result;

						if (ircShip != null)
							isValidShip &= Merge(finalShip, ircShip);
					});
					tasks.Add(t);
				}

				if(shipIdentity.WikiLink != null) 
				{
					var t = _wikiShipFactory.Create(shipIdentity.WikiLink).ContinueWith(res =>
					{
						var wikiShip = res.Result;	

						if (wikiShip != null)
							isValidShip &= Merge(finalShip, wikiShip);
					});
					tasks.Add(t);
				}

				if (shipIdentity.ID != null) { }

				await Task.WhenAll(tasks);

				if (isValidShip)
				{
					// publish ship.
				}
				else
				{
					// send to conflicted ships
				}
			}
		}

		private bool Merge(Ship finalShip, Ship enrichmentShip)
		{
			return true;
		}
	}
}
