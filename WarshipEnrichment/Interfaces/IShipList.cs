using WarshipImport.Data;

namespace WarshipImport.Interfaces
{
    public interface IShipList
    {
        Task<List<Ship>> GetShips();
    }
}