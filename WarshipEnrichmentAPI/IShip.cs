namespace WarshipEnrichmentAPI
{
	public interface IShip
	{
		Guid ID { get; }
		string? WikiLink { get; }
		int? ShiplistKey { get; }


		string? Nation { get; }
		string? ClassName { get; }
		string? ClassType { get; }

		int? LengthFt { get; }
		int? BeamFt { get; }
		int? StandardWeight { get; }
		int? FullWeight { get; }
		int? Launched { get; }
		int? LastYearBuilt { get; }
		int? NumberInClass { get; }

		int? Guns { get; }
		double? GunDiameter { get; }
		double? Armor { get; }

		int? Rudders { get; }
		string? RudderType { get; }
		string? RudderStyle { get; }
		int? Shafts { get; }

		double? SpeedKnots { get; }
		int? SpeedIrcwcc { get; }

		int? ShipClass { get; }
		double? Units { get; }

		string? Comment { get; }
		string? Notes { get; }
	}
}