using WarshipEnrichmentAPI;

namespace WarshipEnrichment.DTOs
{
    public class ShipIdentity : IShipIdentity
    {
        public Guid? ID { get; set; }

        public string? WikiLink { get; set; }

        public int? ShiplistKey { get; set; }
    }
}
