using Serilog;
using Serilog.Context;
using ServiceBus.Core;
using ShipDomain;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
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
			ShipIdentity? shipIdentity;
			using (LogContext.PushProperty("MessageJSON", message))
			{
				try
				{
					Log.Information("Enrichment process started.");

					shipIdentity = JsonSerializer.Deserialize<ShipIdentity>(message);

					if (shipIdentity == null)
					{
						Log.Error("ShipIdentity was null");
						return;
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Exception during deserialization");
					return;
				}
			}

			using (LogContext.PushProperty("ID", shipIdentity.ID))
			using (LogContext.PushProperty("ShiplistKey", shipIdentity.ShiplistKey))
			using (LogContext.PushProperty("WikiLink", shipIdentity.WikiLink))
			{
				try
				{
					Log.Information("Enriching Ship");

					// If the ship has a pre-existing ID in CombatWarships, then take it as the basis.
					// Await here, since we can't gaurantee order
					var tasks = new List<Task>();

					Ship existingShip = new Ship();

					var t = _warshipAPI.GetShip(shipIdentity.ID, shipIdentity.ShiplistKey, shipIdentity.WikiLink)
						.ContinueWith(res =>
						{
							Ship ship = res.Result;

							Log.Information($"Existing ship existed: {ship != null}");

							if (ship != null)
								existingShip = ship;
						});
					tasks.Add(t);

					var shipData = new List<Tuple<ConflictSource, Ship>>();
					if (shipIdentity.ShiplistKey != null)
					{
						Log.Information($"IRCWCC Ship list key exists");
						t = _shipList.FindShip(shipIdentity.ShiplistKey.Value)
							.ContinueWith(res =>
							{
								Ship? ship = res.Result;

								Log.Information($"IRCWCC ship existed: {ship != null}");

								if (ship != null)
									shipData.Add(Tuple.Create(ConflictSource.IrcwccShipList, ship));
							});
						tasks.Add(t);
					}

					if (shipIdentity.WikiLink != null)
					{
						Log.Information($"WIKI URL exists");
						t = _wikiShipFactory.Create(shipIdentity.WikiLink)
							.ContinueWith(res =>
							{
								Ship? ship = res.Result;

								Log.Information($"WIKI ship existed: {ship != null}");

								if (ship != null)
									shipData.Add(Tuple.Create(ConflictSource.Wiki, ship));
							});
						tasks.Add(t);
					}

					// TODO: GetAccepted Conflicts


					// Wait until all source have been retrieved.
					await Task.WhenAll(tasks);

					Log.Information($"All data sources complete, starting merge.");

					var conflicts = Merge(existingShip, shipData, out bool updateFinalShipRequired);

					Log.Information($"Merge complete, {conflicts.Count} merge conflicts identified");


					if (conflicts.Any())
					{
						var conflictedShip = new WarshipConflict()
						{
							Ship = existingShip,
							Conflicts = conflicts
						};
						await _conflictProcessorAPI.PostWarship(conflictedShip);
					}

					if (updateFinalShipRequired)
					{
						Log.Information($"Posting ship to Warship API");
						// publish ship.
						await _addWarshipAPI.PostWarship(existingShip);
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Unexpected Exception during Enrichment");
				}
			}
		}

		private List<Conflict> Merge(Ship finalShip, List<Tuple<ConflictSource, Ship>> shipData, out bool updateFinalShipRequired)
		{
			var conflicts = new List<Conflict>();
			bool updateFinalShip = false;

			var properties = typeof(Ship).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			lock (finalShip)
			{
				foreach (PropertyInfo property in properties)
				{
					List<Tuple<ConflictSource, object>> newValues = GetValue(property, shipData);

					// Step 1 - Remove all values which have been overruled.

					// Step 2 - Find all unique values
					object newValue = null;
					bool propertyConflict = false;
					foreach (var proposedValue in newValues.Select(t => t.Item2))
					{
						if (IsConflicted(newValue, proposedValue, (v) => newValue = v))
						{
							propertyConflict = true;
							break;
						}
					}

					// Step 3 - determind if we can accept it into existingValue;
					if (!propertyConflict)
					{
						var existingValue = property.GetValue(finalShip);

						propertyConflict = IsConflicted(existingValue, newValue,
							(v) =>
							{
								updateFinalShip = true;
								property.SetValue(finalShip, v);
							});
					}

					// Step 4 - add it to the list of conflicts.
					if (propertyConflict)
					{
						conflicts.AddRange(newValues.Select(v =>
						{
							var jsonValue = JsonSerializer.Serialize(v.Item2);
							return new Conflict(v.Item1, property.Name, jsonValue);
						}));
					}
				}
			}

			updateFinalShipRequired = updateFinalShip;
			return conflicts;
		}

		private static bool IsConflicted(object? existingValue, object? newValue, Action<object> setValue)
		{
			if (IsDefault(newValue))
				return false;

			if (IsDefault(existingValue))
			{
				setValue(newValue!);
				return false;
			}

			if (existingValue!.Equals(newValue))
				return false;

			// Otherwise, we have a conflict
			return true;
		}

		private static List<Tuple<ConflictSource, object>> GetValue(PropertyInfo propertyInfo, List<Tuple<ConflictSource, Ship>> shipData)
		{
			var values = new List<Tuple<ConflictSource, object>>();
			foreach (var kvp in shipData)
			{
				var conflictSource = kvp.Item1;
				var ship = kvp.Item2;
				var value = propertyInfo.GetValue(ship);

				if (!IsDefault(value))
				{
					values.Add(Tuple.Create(conflictSource, value!));
				}
			}
			return values;
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
