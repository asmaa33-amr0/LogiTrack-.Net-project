using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogiTrackWebV1._0.Models
{
    public class ShipmentRoute_mtm
    {
        public int ShipmentId { get; set; }
        public virtual Shipment Shipment { get; set; } = null!;

        public int StationId { get; set; }
        public virtual Station Station { get; set; } = null!;

        public DateTime ArrivalDate { get; set; }
        public int RouteOrder { get; set; }
    }
}
