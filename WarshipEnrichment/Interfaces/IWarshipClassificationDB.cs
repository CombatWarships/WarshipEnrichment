using WarshipImport.Data;

namespace WarshipImport.Interfaces
{
	public interface IWarshipClassificationDB
	{
		string? FindWarshipType(List<string> html);
		string? FindWarshipType(string text);
		List<WarshipClassification> GetFullList();
	}
}
