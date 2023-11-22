using AutoMapper;
using Serilog;
using System.Text.Json;
using WarshipEnrichment.Converters;
using WarshipEnrichment.Interfaces;
using WarshipImport.Data;
using WarshipImport.Interfaces;

namespace WarshipImport.Managers
{

	public class IrcwccShipList : IShipList
	{
		private readonly HttpClient _client = new HttpClient();
		private readonly IWarshipClassificationConverter _warshipClassification;
		private readonly INationalityConverter _nationalityConverter;
		private List<Ship>? _ships;

		public IrcwccShipList(IWarshipClassificationConverter warshipClassification, INationalityConverter nationalityConverter)
		{
			_warshipClassification = warshipClassification;
			_nationalityConverter = nationalityConverter;
		}

		public async Task<List<Ship>> GetShips()
		{
			if (_ships != null)
				return _ships;

			string jsonShipData = string.Empty;

			var result = await _client.GetAsync("https://ircwcc.org/common/shiplist/ships.php");

			if (!result.IsSuccessStatusCode)
			{
				Log.Error($"IRCWCC Ship List result: {result.ReasonPhrase}");
				return new List<Ship>();
			}

			jsonShipData = await result.Content.ReadAsStringAsync();

			var options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
			var shipData = JsonSerializer.Deserialize<List<IrcwccShip>>(jsonShipData, options);

			if (!(shipData?.Count > 0))
			{
				Log.Warning("No IRCWCC ship data records");
				return new List<Ship>(); ;
			}

				var mapperConfig = new MapperConfiguration(cfg =>
			cfg.CreateMap<IrcwccShip, Ship>()
				  .ForMember(dest => dest.SpeedIrcwcc, opt => opt.MapFrom(src => src.Speed))
				  .ForMember(dest => dest.LengthFt, opt => opt.MapFrom(src => src.Loa))
				  .ForMember(dest => dest.BeamFt, opt => opt.MapFrom(src => src.Beam))
				  .ForMember(dest => dest.ClassType, opt => opt.MapFrom(src => _warshipClassification.Find(src.ClassType)))
				  .ForMember(dest => dest.Nation, opt => opt.MapFrom(src => _nationalityConverter.Find(new string[] { src.Nation })))
				  .ForMember(dest => dest.Launched, opt => opt.MapFrom(src => FindFirstYear(src.Launched)))
				  .ForMember(dest => dest.LastYearBuilt, opt => opt.MapFrom(src => FindFirstYear(src.Completed) ?? FindSecondYear(src.Launched)))
			);

			var mapper = mapperConfig.CreateMapper();

			var ships = new List<Ship>();
			foreach (var jsonShip in shipData)
				ships.Add(mapper.Map<Ship>(jsonShip));
			_ships = ships;

			Log.Information($"IRCWCC ship list returned {ships.Count} ships");

			return _ships;
		}

		private int? FindFirstYear(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			RegExHelper.FindYear(text, out int? firstYear, out int? secondYear);
			return firstYear;
		}
		private int? FindSecondYear(string? text)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			RegExHelper.FindYear(text, out int? firstYear, out int? secondYear);
			return secondYear;
		}
	}
}