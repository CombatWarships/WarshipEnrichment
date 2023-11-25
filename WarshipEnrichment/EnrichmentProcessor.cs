using Serilog;
using Serilog.Context;
using ServiceBus.Core;
using ShipDomain;
using System.Reflection;
using System.Text.Json;
using WarshipConflictsAPI;
using WarshipEnrichment.DTOs;
using WarshipEnrichmentAPI;
using WarshipImport.Interfaces;
using WarshipRegistryAPI.Warships;

namespace WarshipEnrichment
{

	public class EnrichmentProcessor : IMessageProcessor
	{
		private readonly IWarshipAPI _warshipAPI;
		private readonly IShipList _shipList;
		private readonly IWikiShipFactory _wikiShipFactory;
		private readonly IConflictProcessorAPI _conflictProcessorAPI;
		private readonly IAddWarshipAPI _addWarshipAPI;

		public EnrichmentProcessor(IWarshipAPI warshipAPI, IShipList shipList, IWikiShipFactory wikiShipFactory, IConflictProcessorAPI conflictProcessorAPI, IAddWarshipAPI addWarshipAPI)
		{
			_warshipAPI = warshipAPI;
			_shipList = shipList;
			_wikiShipFactory = wikiShipFactory;
			_conflictProcessorAPI = conflictProcessorAPI;
			_addWarshipAPI = addWarshipAPI;
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

				// If the ship has a pre-existing ID in CombatWarships, then take it as the basis.
				// Await here, since we can't gaurantee order
				var finalShip = (await _warshipAPI.GetShip(shipIdentity.ID, shipIdentity.ShiplistKey, shipIdentity.WikiLink)) ?? new Ship();

					// GetAccepted Conflicts
				

				List<Conflict> conflicts = new List<Conflict>();
				List<Task> tasks = new List<Task>();
				if (shipIdentity.ShiplistKey != null)
				{
					var t = _shipList.FindShip(shipIdentity.ShiplistKey.Value).ContinueWith(res =>
					{
						Ship? ircShip = res.Result;

						if (ircShip != null)
							conflicts.AddRange(Merge(finalShip, ircShip, ConflictSource.IrcwccShipList));
					});
					tasks.Add(t);
				}

				if (shipIdentity.WikiLink != null)
				{
					var t = _wikiShipFactory.Create(shipIdentity.WikiLink).ContinueWith(res =>
					{
						Ship? wikiShip = res.Result;

						if (wikiShip != null)
							conflicts.AddRange(Merge(finalShip, wikiShip, ConflictSource.Wiki));
					});
					tasks.Add(t);
				}

				await Task.WhenAll(tasks);

				// Remove any previously accepted conflicts.
				// RemoveAcceptedConflicts();

				if (!conflicts.Any())
				{
					// publish ship.
					await _addWarshipAPI.PostWarship(finalShip);
				}
				else
				{
					var conflictedShip = new WarshipConflict()
					{
						Ship = finalShip,
						Conflicts = conflicts
					};
					await _conflictProcessorAPI.PostWarship(conflictedShip);
				}
			}
		}

		private List<Conflict> Merge(Ship finalShip, Ship enrichmentShip, ConflictSource conflictSource)
		{
			List<Conflict> conflicts = new List<Conflict>();
			var properties = typeof(Ship).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			lock (finalShip)
			{
				foreach (var property in properties)
				{
					var existingValue = property.GetValue(finalShip);
					var newValue = property.GetValue(enrichmentShip);

					if (IsDefault(newValue))
						continue;

					if (IsDefault(existingValue))
					{
						property.SetValue(finalShip, newValue);
						continue;
					}

					if (newValue.Equals(existingValue))
						continue;

					// Otherwise, we have a conflict
					conflicts.Add(new Conflict(property.Name, conflictSource));
				}
			}

			return conflicts;
		}

		private static bool IsDefault(object? value)
		{
			if (value == null)
				return true;

			var type = value.GetType();
			if (type.IsValueType)
			{
				var defaultValue = Activator.CreateInstance(type);
				return value.Equals(defaultValue);
			}

			return false;
		}
	}
}
