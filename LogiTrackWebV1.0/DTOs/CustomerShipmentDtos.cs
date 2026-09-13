using System.ComponentModel.DataAnnotations;

namespace LogiTrackWebV1._0.DTOs
{
    // Input a customer submits to price or create a shipment request.
    public class ShipmentRequestInputDto
    {
        [Range(0.01, 100000, ErrorMessage = "Weight must be greater than zero.")]
        public decimal Weight { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public int EndStationId { get; set; }

        // The date the customer wants to receive the shipment (drives the surcharge).
        [Required]
        public DateTime ReceiveDate { get; set; }
    }

    // Transparent price breakdown returned by the quote and create endpoints.
    public class PriceQuoteDto
    {
        public double DistanceKm { get; set; }
        public decimal BaseFee { get; set; }
        public decimal WeightCharge { get; set; }
        public decimal DistanceCharge { get; set; }
        public decimal Subtotal { get; set; }

        public int DaysUntilReceive { get; set; }
        public bool DateFactorApplied { get; set; }
        public decimal DateSurchargePercent { get; set; }
        public decimal DateSurchargeAmount { get; set; }

        public decimal Total { get; set; }
        public string Currency { get; set; } = "EGP";
    }

    // One row in the customer's list of requests.
    public class CustomerRequestListItemDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = "N/A";
        public string EndStationName { get; set; } = "N/A";
        public DateTime? ReceiveDate { get; set; }
        public decimal? Price { get; set; }
        public DateTime RequestedAt { get; set; }
        public bool CanCancel { get; set; }
    }

    // Full detail for tracking a single request.
    public class CustomerRequestDetailsDto : CustomerRequestListItemDto
    {
        public List<TrackingEventDto> Checkpoints { get; set; } = new();
    }

    public class TrackingEventDto
    {
        public int Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Dropdown data for the "new request" form.
    public class CustomerLookupsDto
    {
        public List<LookupDto> Warehouses { get; set; } = new();
        public List<LookupDto> Stations { get; set; } = new();
    }
}
