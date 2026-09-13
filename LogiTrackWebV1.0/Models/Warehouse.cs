using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace LogiTrackWebV1._0.Models
{
    public class Warehouse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        [JsonIgnore]
        public virtual ICollection<Shipment>? Shipments { get; set; } = new List<Shipment>();
    }
}