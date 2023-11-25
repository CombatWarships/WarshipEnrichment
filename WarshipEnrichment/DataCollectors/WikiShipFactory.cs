using ShipDomain;
using WarshipEnrichment.Interfaces;
using WarshipImport.Interfaces;

namespace WarshipImport.Managers
{
	public class WikiShipFactory : IWikiShipFactory
	{
		private readonly INationalityConverter _nationalityConverter;
		private readonly IWarshipClassificationConverter _warshipClassification;

		public WikiShipFactory(INationalityConverter nationalityConverter, IWarshipClassificationConverter warshipClassification)
		{
			_nationalityConverter = nationalityConverter;
			_warshipClassification = warshipClassification;
		}

		public async Task<Ship?> Create(string url)
		{
			var analyzer = new WikiDataAnalyzer(_nationalityConverter, _warshipClassification);

			var validShip = await analyzer.Load(url);

			if (!validShip)
				return null;

			Ship ship = new Ship()
			{
				Nation = analyzer.Nationality,
				ClassName = analyzer.ClassName,
				ClassType = analyzer.WarshipType,
				NumberInClass = analyzer.NumberInClass,
				Launched = analyzer.FirstYearBuilt,
				LastYearBuilt = analyzer.LastYearBuilt,

				SpeedKnots = analyzer.Speed,
				LengthFt = analyzer.Length,
				BeamFt = analyzer.Beam,
				StandardWeight = analyzer.StandardWeight,
				FullWeight = analyzer.FullWeight,
				Guns = analyzer.NumberOfGuns,
				GunDiameter = analyzer.GunCaliber,
				Armor = analyzer.Armor,

				Rudders = analyzer.Rudders,
				Shafts = analyzer.Shafts,

				WikiLink = analyzer.Url
			};

			return ship;
		}

		//public bool ValidateShip(Ship ship)
		//{
		//	if (!HasData)
		//		return false;

		//	int dataConfirmations = 0;

		//	var built = FirstYearBuilt;

		//	if (built < 1900 || built > 1946)
		//		return false;

		//	var shipDate = ship.Launched ?? ship.LastYearBuilt;
		//	if (shipDate.HasValue && built.HasValue)
		//	{
		//		if (!(Math.Abs(shipDate.Value - built.Value) < 10))
		//			return false;
		//		dataConfirmations++;
		//	}

		//	var beam = Beam;
		//	if (ship.BeamFt.HasValue && beam.HasValue)
		//	{
		//		if (!(Math.Abs(ship.BeamFt.Value - beam.Value) < 5))
		//			return false;
		//		dataConfirmations++;
		//	}

		//	var length = Length;
		//	if (ship.LengthFt.HasValue && length.HasValue)
		//	{
		//		if (!(Math.Abs(ship.LengthFt.Value - length.Value) < 10))
		//			return false;
		//		dataConfirmations++;
		//	}

		//	// type
		//	var classType = WarshipType;
		//	if (classType != null)
		//	{
		//		if (classType.ID != ship.ClassType)
		//			return false;
		//		dataConfirmations++;
		//	}

		//	if (dataConfirmations < 3)
		//		return false;

		//	return true;
		//}
	}
}