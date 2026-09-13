using System.Security.Claims;
using LogiTrackWebV1._0.DTOs;
using LogiTrackWebV1._0.Models;
using LogiTrackWebV1._0.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0.Controllers
{
    // Customer-facing endpoints: a logged-in user can price and submit a shipment
    // request, list their requests, track one, and cancel one they still own.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CustomerUiController : ControllerBase
    {
        private readonly LT_DBContext _context;
        private readonly IPricingService _pricing;
        private readonly ILogger<CustomerUiController> _logger;

        // A request can only be cancelled before it is handed to a driver / dispatched.
        private static readonly string[] CancellableStatuses = { "Pending", "Assigned" };

        public CustomerUiController(
            LT_DBContext context,
            IPricingService pricing,
            ILogger<CustomerUiController> logger)
        {
            _context = context;
            _pricing = pricing;
            _logger = logger;
        }

        // GET: api/CustomerUi/lookups
        // Warehouses (origins) and stations (destinations) for the request form.
        [HttpGet("lookups")]
        public async Task<IActionResult> GetLookups()
        {
            var warehouses = await _context.Warehouses
                .Select(w => new LookupDto { Id = w.Id, Name = w.Name })
                .ToListAsync();

            var stations = await _context.Stations
                .Select(st => new LookupDto { Id = st.Id, Name = st.StationName })
                .ToListAsync();

            return Ok(new CustomerLookupsDto { Warehouses = warehouses, Stations = stations });
        }

        // POST: api/CustomerUi/quote
        // Returns a price breakdown without saving anything.
        [HttpPost("quote")]
        public async Task<IActionResult> GetQuote([FromBody] ShipmentRequestInputDto dto)
        {
            var (error, warehouse, station) = await ValidateAndLoadAsync(dto);
            if (error != null) return error;

            try
            {
                double distanceKm = ResolveDistanceKm(warehouse!, station!);
                var quote = _pricing.CalculateQuote(dto.Weight, distanceKm, dto.ReceiveDate);
                return Ok(quote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/CustomerUi/requests
        // Creates a new shipment request for the current customer.
        [HttpPost("requests")]
        public async Task<IActionResult> CreateRequest([FromBody] ShipmentRequestInputDto dto)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var (error, warehouse, station) = await ValidateAndLoadAsync(dto);
            if (error != null) return error;

            PriceQuoteDto quote;
            try
            {
                double distanceKm = ResolveDistanceKm(warehouse!, station!);
                quote = _pricing.CalculateQuote(dto.Weight, distanceKm, dto.ReceiveDate);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var shipment = new Shipment
            {
                TrackingNumber = await GenerateTrackingNumberAsync(),
                Weight = dto.Weight,
                Status = "Pending",
                WarehouseId = warehouse!.Id,
                DriverId = null,                  // assigned later by an admin
                EndStationId = station!.Id,
                ReceiveDate = dto.ReceiveDate,
                Price = quote.Total,
                CustomerUserId = userId,
                RequestedAt = DateTime.UtcNow
            };

            // Record the destination as the first planned route stop so the request
            // plugs into the existing route/tracking model.
            shipment.ShipmentRoutes.Add(new ShipmentRoute_mtm
            {
                StationId = station.Id,
                ArrivalDate = dto.ReceiveDate,
                RouteOrder = 1
            });

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            var details = await BuildDetailsAsync(shipment.Id, userId);
            return CreatedAtAction(nameof(GetRequestById), new { id = shipment.Id }, details);
        }

        // GET: api/CustomerUi/requests
        [HttpGet("requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var requests = await _context.Shipments
                .Where(s => s.CustomerUserId == userId)
                .OrderByDescending(s => s.RequestedAt)
                .Select(s => new CustomerRequestListItemDto
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    Weight = s.Weight,
                    Status = s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    EndStationName = _context.Stations
                        .Where(st => st.Id == s.EndStationId)
                        .Select(st => st.StationName)
                        .FirstOrDefault() ?? "N/A",
                    ReceiveDate = s.ReceiveDate,
                    Price = s.Price,
                    RequestedAt = s.RequestedAt
                })
                .ToListAsync();

            foreach (var r in requests)
                r.CanCancel = CancellableStatuses.Contains(r.Status);

            return Ok(requests);
        }

        // GET: api/CustomerUi/requests/{id}
        [HttpGet("requests/{id:int}")]
        public async Task<IActionResult> GetRequestById(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var details = await BuildDetailsAsync(id, userId);
            if (details == null)
                return NotFound(new { message = "Request not found." });

            return Ok(details);
        }

        // GET: api/CustomerUi/track/{trackingNumber}
        [HttpGet("track/{trackingNumber}")]
        public async Task<IActionResult> Track(string trackingNumber)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var id = await _context.Shipments
                .Where(s => s.TrackingNumber == trackingNumber && s.CustomerUserId == userId)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            if (id == null)
                return NotFound(new { message = "No request found with that tracking number." });

            var details = await BuildDetailsAsync(id.Value, userId);
            return Ok(details);
        }

        // PUT: api/CustomerUi/requests/{id}/cancel
        [HttpPut("requests/{id:int}/cancel")]
        public async Task<IActionResult> CancelRequest(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Could not identify the current user." });

            var shipment = await _context.Shipments
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerUserId == userId);

            if (shipment == null)
                return NotFound(new { message = "Request not found." });

            if (!CancellableStatuses.Contains(shipment.Status))
                return Conflict(new { message = $"A request that is '{shipment.Status}' can no longer be cancelled." });

            shipment.Status = "Cancelled";
            await _context.SaveChangesAsync();

            return Ok(new { message = "Request cancelled successfully." });
        }

        // ---- helpers -------------------------------------------------------

        private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Validates the input and loads the referenced warehouse and station.
        private async Task<(IActionResult? error, Warehouse? warehouse, Station? station)>
            ValidateAndLoadAsync(ShipmentRequestInputDto dto)
        {
            if (dto == null)
                return (BadRequest(new { message = "Request body is required." }), null, null);

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);
            if (warehouse == null)
                return (BadRequest(new { message = "Selected warehouse does not exist." }), null, null);

            var station = await _context.Stations.FirstOrDefaultAsync(st => st.Id == dto.EndStationId);
            if (station == null)
                return (BadRequest(new { message = "Selected destination station does not exist." }), null, null);

            return (null, warehouse, station);
        }

        // === Distance integration point ====================================
        // Distance between the origin warehouse and the destination station.
        // This assumes both models expose double Latitude and double Longitude.
        private double ResolveDistanceKm(Warehouse warehouse, Station station)
        {
            return _pricing.HaversineKm(
                warehouse.Latitude, warehouse.Longitude,
                station.Latitude, station.Longitude);
        }
        // ===================================================================

        private async Task<CustomerRequestDetailsDto?> BuildDetailsAsync(int id, string userId)
        {
            var details = await _context.Shipments
                .Where(s => s.Id == id && s.CustomerUserId == userId)
                .Select(s => new CustomerRequestDetailsDto
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    Weight = s.Weight,
                    Status = s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    EndStationName = _context.Stations
                        .Where(st => st.Id == s.EndStationId)
                        .Select(st => st.StationName)
                        .FirstOrDefault() ?? "N/A",
                    ReceiveDate = s.ReceiveDate,
                    Price = s.Price,
                    RequestedAt = s.RequestedAt,
                    Checkpoints = _context.RouteCheckpoints
                        .Where(c => c.ShipmentId == s.Id)
                        .OrderBy(c => c.Timestamp)
                        .Select(c => new TrackingEventDto
                        {
                            Id = c.Id,
                            StationName = c.StationName,
                            Info = c.Info,
                            Timestamp = c.Timestamp
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (details != null)
                details.CanCancel = CancellableStatuses.Contains(details.Status);

            return details;
        }

        private async Task<string> GenerateTrackingNumberAsync()
        {
            string tracking;
            do
            {
                tracking = $"LT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
            }
            while (await _context.Shipments.AnyAsync(s => s.TrackingNumber == tracking));

            return tracking;
        }
    }
}