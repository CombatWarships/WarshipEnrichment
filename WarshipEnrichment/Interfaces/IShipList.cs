using ShipDomain;
using WarshipRegistryAPI.Warships;

namespace WarshipImport.Interfaces
{
    public interface IShipList
    {
		Task<Ship?> FindShip(int shipListKey);
		Task<List<Ship>> GetShips();
    }
}