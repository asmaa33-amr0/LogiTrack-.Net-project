using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LogiTrackWebV1._0.Models
{
    public class Shipment
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Status { get; set; } = "Pending";

        public virtual Address? Address { get; set; }

        public int WarehouseId { get; set; }
        public virtual Warehouse Warehouse { get; set; } = null!;

        public int? DriverId { get; set; }
        public virtual Driver? Driver { get; set; }

        public int? EndStationId { get; set; }

        [ForeignKey(nameof(EndStationId))]
        public virtual Station? EndStation { get; set; }

        public DateTime? ReceiveDate { get; set; }
        public decimal Price { get; set; }
        public string CustomerUserId { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ShipmentRoute_mtm> ShipmentRoutes { get; set; } = new List<ShipmentRoute_mtm>();

        [JsonIgnore]
        public virtual ICollection<RouteCheckpoint> RouteCheckpoints { get; set; } = new List<RouteCheckpoint>();
    }
}