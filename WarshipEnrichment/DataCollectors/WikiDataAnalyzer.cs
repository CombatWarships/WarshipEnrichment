using HtmlAgilityPack;
using Serilog;
using Serilog.Context;
using WarshipEnrichment.Converters;
using WarshipRegistryAPI;

namespace WarshipImport.Managers
{
	public class WikiDataAnalyzer
	{
		private const string _wikiImageBaseUrl = "//upload.wikimedia.org/wikipedia/commons/thumb/";

		private Dictionary<string, string[]> _data = new Dictionary<string, string[]>();
		private readonly IWarshipClassificationAPI _warshipClassificationDB;

		public WikiDataAnalyzer(IWarshipClassificationAPI warshipClassificationDB)
		{
			_warshipClassificationDB = warshipClassificationDB;
		}

		public string Url { get; private set; }
		public bool HasData => _data.Count > 0;

		public async Task<bool> Load(string url)
		{
			try
			{
				Url = url;

				_data = await Extract(url);

				bool isValid = ProcessShip();

				Log.Information($"Ship data is {isValid}");
				return isValid;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Error Parsing {Url}");
				return false;
			}
		}

		private static async Task<Dictionary<string, string[]>> Extract(string url)
		{
			List<string> parentWarshipType = new List<string>();

			Dictionary<string, string[]> data = new Dictionary<string, string[]>();
			Log.Information($"Loading WikiDataAnalyzer {url}");

			data.Clear();

			if (string.IsNullOrEmpty(url))
				return data;

			HtmlWeb web = new HtmlWeb();
			HtmlDocument htmlDoc = await web.LoadFromWebAsync(url);

			try
			{
				var infobox = htmlDoc.DocumentNode.Descendants(0)
					  .FirstOrDefault(n => n.HasClass("infobox"))?.SelectSingleNode("tbody");

				if (infobox == null)
				{
					Log.Warning($"Wiki URL {url} does not have a infobox");
					return data;
				}

				int rowId = 0;

				foreach (var row in infobox.ChildNodes.Where(n => n.Name == "tr"))
				{
					rowId++;

					string key = string.Empty;
					string[] value = null;
					try
					{
						if (rowId == 1)
						{
							// This might be the photo.

							foreach (var image in row.ChildNodes.Descendants().Where(n => n.Name == "img"))
							{
								using (LogContext.PushProperty("image", image.OuterHtml))
								{
									try
									{
										HtmlAttribute src = image.Attributes["src"];

										if (src == null || string.IsNullOrEmpty(src.Value))
											continue;

										var imgUrl = src.Value;

										if (!imgUrl.StartsWith(_wikiImageBaseUrl))
											continue;

										var imageUrlSplice = imgUrl.Split('/');
										var adjustableFilename = imageUrlSplice[imageUrlSplice.Length - 1];
										var uniqueFilenameSplit = adjustableFilename.Split("px");

										var uniqueFileName = "{0}px" + uniqueFilenameSplit[1];

										imgUrl = imgUrl.Replace(adjustableFilename, uniqueFileName);
										imgUrl = $"https:{imgUrl}";
										data.TryAdd("photo", new string[] { imgUrl });
									}
									catch (Exception ex)
									{
										Log.Error(ex, "Error parsing image");
										continue;
									}
								}
							}
						}

						if (row.ChildNodes.Count != 2)
							continue;
						if (row.ChildNodes[0].Name != "td")
							continue;
						if (row.ChildNodes[1].Name != "td")
							continue;

						// We have our Key Value Pair!
						key = ConvertToText(row.ChildNodes[0].InnerText).ToLowerInvariant();
						key = NormalizeKeys(key);

						if (key == "classandtype" || key == "type")
						{
							using (LogContext.PushProperty("ClassAndType", row.ChildNodes[1]))
							{
								Log.Warning($"This is a ship in the class, not a warship class.");
								var classRef = row.ChildNodes[1].ChildNodes[0];
								var classUrl = classRef.Attributes["href"].Value;

								if (classUrl.StartsWith("/wiki/"))
								{
									classUrl = $"https://en.wikipedia.org{classUrl}";
									parentWarshipType.Add(classUrl);
								}
							}
						}

						value = row.ChildNodes[1].InnerHtml.Split('\n');
					}
					catch (Exception ex)
					{
						Log.Error(ex, $"Error parsing {row?.InnerHtml}");
						continue;
					}

					try
					{
						bool duplicateKeyExists = data.TryAdd(key, value);
					}
					catch (Exception ex)
					{
						Log.Error(ex, $"Error in try add {key}");
					}
				}

				foreach (var parentUrl in parentWarshipType)
				{
					var parentData = await Extract(parentUrl);

					foreach (var kvp in parentData)
					{
						if (!data.ContainsKey(kvp.Key))
							data.TryAdd(kvp.Key, kvp.Value);
						else
						{
							var mergedValues = new List<string>(data[kvp.Key]);
							mergedValues.AddRange(kvp.Value);
							data[kvp.Key] = mergedValues.ToArray();
						}
					}
				}
				Log.Information($"Data has been parsed from Wiki {url}.");

				return data;
			}
			catch (Exception ex)
			{
				Log.Error(ex, $"Error Parsing {url}");
				throw;
			}
		}


		private bool ProcessShip()
		{
			if (!HasData)
			{
				Log.Warning("No data found");
				return false;
			}

			ClassName = GetText("name");
			if (string.IsNullOrEmpty(ClassName))
			{
				Log.Warning("No classname");

			}

			WarshipType = GetWarshipType();
			if (WarshipType == null)
			{
			}

			Nationality = GetNationality();

			Speed = GetValue("speed", RegExHelper.FindKnots);
			if (NotInRange(Speed, 0, 60))
			{
			}

			Length = (int?)GetValue("length", RegExHelper.FindFeet);
			if (NotInRange(Length, 0, 1000))
			{
			}

			Beam = (int?)GetValue("beam", RegExHelper.FindFeet);
			if (NotInRange(Beam, 0, 500))
			{
			}

			Armor = GetArmor();
			if (NotInRange(Armor, 0, 30))
			{
			}

			GetYear(out int? firstYear, out int? secondYear);
			FirstYearBuilt = firstYear;
			LastYearBuilt = secondYear;
			if (NotInRange(FirstYearBuilt, 1800, 1946))
			{
			}

			GetWeight(out int? standardWeight, out int? fullWeight);
			StandardWeight = standardWeight;
			FullWeight = fullWeight;
			if (NotInRange(StandardWeight, 0, 1000000))
			{
			}

			GetPrimaryGuns(out int? caliber, out int? quantity);
			NumberOfGuns = quantity;
			GunCaliber = caliber;
			if (NotInRange(NumberOfGuns, 0, 20))
			{
			}

			if (NotInRange(GunCaliber, 0, 20))
			{
			}

			Shafts = GetValue("propulsion", RegExHelper.FindShafts);
			if (NotInRange(Shafts, 0, 5))
			{
			}

			NumberInClass = GetNumber("completed");


			return true;
		}

		public string ClassName { get; private set; }


		public string? WarshipType { get; private set; }

		private string? GetWarshipType()
		{
			var allLines = new List<string>();
			if (_data.TryGetValue("type", out string[] lines))
				allLines.AddRange(lines.Select(l => ConvertToText(l)));

			if (_data.TryGetValue("classandtype", out lines))
				allLines.AddRange(lines.Select(l => ConvertToText(l)));

			return _warshipClassificationDB.FindWarshipType(allLines);
		}

		public int? NumberInClass { get; private set; }
		public int? FirstYearBuilt { get; private set; }
		public int? LastYearBuilt { get; private set; }
		public int? Beam { get; private set; }

		public int? Length { get; private set; }

		public double? Speed { get; private set; }
		public double? Armor { get; private set; }
		public int? StandardWeight { get; private set; }

		private double? GetArmor()
		{
			if (!_data.TryGetValue("armor", out string[] value))
				return null;

			foreach (var line in value)
			{
				if (line.Contains("belt", StringComparison.OrdinalIgnoreCase))
				{
					var text = ConvertToText(line);
					return RegExHelper.FindLargestInchFromRange(text);
				}
			}
			return null;
		}

		public int? FullWeight { get; private set; }

		public int? NumberOfGuns { get; private set; }

		internal int? GunCaliber { get; private set; }
		public int? Rudders { get; private set; }
		public int? Shafts { get; private set; }
		public string Nationality { get; private set; }

		private string GetNationality()
		{
			if (!_data.TryGetValue("operators", out string[] value))
				return null;

			var nation = NationalityConverter.FindNationality(value);

			if (nation == Data.Nationality.Unknown)
				return null;

			return nation.ToString();
		}

		private void GetYear(out int? firstYear, out int? secondYear)
		{
			firstYear = null;
			secondYear = null;
			if (_data.TryGetValue("built", out string[] value))
			{
				foreach (var line in value)
				{
					var text = ConvertToText(value.FirstOrDefault());
					RegExHelper.FindYear(text, out firstYear, out secondYear);

					if (firstYear != null || secondYear != null)
						return;
				}
			}

			if (firstYear == null)
			{
				if (!_data.TryGetValue("incommission", out value))
					return;

				foreach (var line in value)
				{
					var text = ConvertToText(line);
					RegExHelper.FindYear(text, out firstYear, out int? doNotUse);

					if (firstYear != null || secondYear != null)
						return;
				}
			}
		}

		private void GetWeight(out int? standardWeight, out int? fullWeight)
		{
			standardWeight = null;
			fullWeight = null;
			if (!_data.TryGetValue("displacement", out string[] value))
				return;

			int? fullWeightIndex = null;
			int index = 0;
			foreach (var line in value)
			{
				if (line.Contains("standard", StringComparison.OrdinalIgnoreCase)
					 || line.Contains("normal", StringComparison.OrdinalIgnoreCase)
					 || line.Contains("actual", StringComparison.OrdinalIgnoreCase))
				{
					var text = ConvertToText(line);
					standardWeight = RegExHelper.FindTons(text);
				}
				if (line.Contains("full", StringComparison.OrdinalIgnoreCase)
					 || line.Contains("deep", StringComparison.OrdinalIgnoreCase))
				{
					var text = ConvertToText(line);
					fullWeight = RegExHelper.FindTons(text);
					fullWeightIndex = index;
				}
				index++;
			}

			if (standardWeight == null && fullWeight == null)
			{
				foreach (var line in value)
				{
					RegExHelper.FindTonRange(line, out int? smallestWeight, out int? largestWeight);

					bool found = false;
					if (smallestWeight != null)
					{
						standardWeight = smallestWeight;
						found = true;
					}
					if (largestWeight != null)
					{
						fullWeight = largestWeight;
						found = true;
					}
					if (found)
						break;
				}
			}

			if (standardWeight == null && fullWeightIndex > 0)
			{
				var line = value[fullWeightIndex.Value - 1];
				var text = ConvertToText(line);
				standardWeight = RegExHelper.FindTons(text);
			}
		}

		internal void GetPrimaryGuns(out int? caliber, out int? quantity)
		{
			caliber = null;
			quantity = null;

			if (!_data.TryGetValue("armament", out string[] value))
				return;

			string largestGunRow = string.Empty;
			foreach (var line in value)
			{
				var text = ConvertToText(line);
				if (text.Contains("torpedo", StringComparison.OrdinalIgnoreCase))
					continue;

				int? cal = (int?)RegExHelper.FindInches(text);
				if (caliber == null && cal != null || cal > caliber)
				{
					caliber = cal;
					largestGunRow = text;
				}
			}

			quantity = RegExHelper.FindGunQuantity(largestGunRow, caliber);
		}

		private T? GetValue<T>(string key, Func<string, T?> regexHelper)
			  where T : struct, IComparable
		{
			if (!_data.TryGetValue(key, out string[] lines))
				return null;

			T? value = null;
			foreach (var html in lines)
			{
				var line = ConvertToText(html);
				T? temp = regexHelper(line);
				if (temp != null && temp.Value.CompareTo(value) > 0)
				{
					value = temp;
					break;
				}
			}
			return value;
		}

		private int? GetNumber(string key)
		{
			var text = GetText(key);
			if (!int.TryParse(text, out int number))
				return null;
			return number;
		}

		private string GetText(string key)
		{
			if (!_data.TryGetValue(key, out string[] lines))
				return null;

			string value = null;
			foreach (var html in lines)
			{
				var line = ConvertToText(html);
				if (!string.IsNullOrEmpty(line))
				{
					value = line;
					break;
				}
			}
			return value;
		}

		private static string NormalizeKeys(string key)
		{
			key = key.Replace(" ", string.Empty);

			switch (key)
			{
				case "laiddown":
				case "built":
					return "built";
				case "inservice":
				case "incommission":
					return "incommission";
				case "armour":
					return "armor";
				default:
					return key;
			}
		}

		private static string ConvertToText(string innerText)
		{
			HtmlDocument d = new HtmlDocument();
			d.LoadHtml(innerText);
			if (d.ParseErrors.Count() == 0)
				innerText = d.DocumentNode.InnerText;

			innerText = innerText.Replace("&#160;", " ");
			innerText = innerText.Replace("&#8260;", "/");
			innerText = innerText.Replace("&#8211;", "-");
			innerText = innerText.Replace("&#8211;", "-");
			innerText = innerText.Replace("–", "-");
			innerText = innerText.Replace("-", "-");

			innerText = innerText.Replace("+1/2", ".5");
			innerText = innerText.Replace("+1/4", ".25");
			return innerText;
		}


		private static bool NotInRange(int? value, int min, int max)
		{
			return value == null || value < min || value > max;
		}
		private static bool NotInRange(double? value, double min, double max)
		{
			return value == null || value < min || value > max;
		}

	}
}