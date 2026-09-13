using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiTrackWebV1._0.Models
{
    public class Station
    {
        public int Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public virtual ICollection<ShipmentRoute_mtm> ShipmentRoutes { get; set; } = new List<ShipmentRoute_mtm>();
    }
}