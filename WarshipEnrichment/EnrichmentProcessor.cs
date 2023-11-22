using Serilog;
using Serilog.Context;
using System.Text.Json;
using WarshipEnrichment.DTOs;
using WarshipImport.Managers;

namespace WarshipEnrichment
{

	public class EnrichmentProcessor
	{
		private readonly IrcwccShipList _ircShipList = new IrcwccShipList();

		private IEnumerable<Ship> GetShipList()
		{
			_ircShipList = new IrcwccShipList();

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

				if(shipIdentity.ShiplistKey != null) 
				{
				var ircShip = 
				}


				if(shipIdentity.WikiLink != null) { }

				if(shipIdentity.ID != null) { }	
				return;
			}
		}
	}
}
