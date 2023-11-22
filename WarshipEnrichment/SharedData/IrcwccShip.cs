namespace WarshipImport.Data
{
	internal class IrcwccShip
	{
		public string Nation { get; set; }
		public string ClassName { get; set; }
		public string ClassType { get; set; }


		public int? ShipClass { get; set; }
		public double? Units { get; set; }
		public int? NumberInClass { get; set; }
		public int? Speed { get; set; }


		public int? Loa { get; set; }
		public int? Beam { get; set; }
		public int? StandardWeight { get; set; }
		public int? FullWeight { get; set; }


		public int? Guns { get; set; }
		public double? GunDiameter { get; set; }
		public double? Armor { get; set; }
		public int? Rudders { get; set; }
		public string? RudderType { get; set; }
		public string? RudderStyle { get; set; }
		public int? Shafts { get; set; }


		public int? ShiplistKey { get; set; }
		public string? Comment { get; set; }
		public string? WikiLink { get; set; }
		public string? NavyLink { get; set; } //{ get => navyLink; set => navyLink = value?.Replace(@"\/", "/"); }
		public string? AdditionalLink { get; set; }
		public string? Notes { get; set; }
		public string? Unknown0 { get; set; }

		public string? Launched { get; set; }
		public string? Completed { get; set; }

	}
}
