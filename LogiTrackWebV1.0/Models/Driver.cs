using System.Text.Json.Serialization;

namespace LogiTrackWebV1._0.Models
{
    public class Driver
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? UserId { get; set; }

        [JsonIgnore]
        public virtual DriverLicense? License { get; set; }

        [JsonIgnore]
        public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}