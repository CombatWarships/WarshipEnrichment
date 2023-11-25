
using ShipDomain;

namespace WarshipImport.Interfaces
{
	public interface IWikiShipFactory
	{
		Task<Ship> Create(string url);
	}
}
