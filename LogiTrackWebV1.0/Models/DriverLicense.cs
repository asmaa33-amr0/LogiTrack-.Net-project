using System.Text.Json.Serialization;

namespace LogiTrackWebV1._0.Models
{
    public class DriverLicense
    {
        public int DriverId { get; set; }   // PK and FK
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }

        [JsonIgnore]
        public virtual Driver Driver { get; set; } = null!;
    }
}