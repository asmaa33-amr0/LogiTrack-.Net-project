using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LogiTrackWebV1._0.Models
{
    public class RouteCheckpoint
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ShipmentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string StationName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Info { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey(nameof(ShipmentId))]
        public virtual Shipment? Shipment { get; set; }
    }
}