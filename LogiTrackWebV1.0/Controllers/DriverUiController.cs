using LogiTrackWebV1._0;
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace LogiTrackWebV1._0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Driver,Admin")]
    public class DriverUiController : ControllerBase
    {
        private readonly LT_DBContext _context;

        public DriverUiController(LT_DBContext context)
        {
            _context = context;
        }

        private string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private async Task<int?> GetMyDriverIdAsync()
        {
            if (string.IsNullOrEmpty(CurrentUserId)) return null;
            return await _context.Drivers
                .Where(d => d.UserId == CurrentUserId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        [HttpGet("driver/{driverId:int}")]
        public async Task<IActionResult> GetDriverDashboard(int driverId)
        {
            if (User.IsInRole("Driver"))
            {
                var mine = await GetMyDriverIdAsync();
                if (mine != driverId) return Forbid();
            }

            var driver = await _context.Drivers
                .Where(d => d.Id == driverId)
                .Select(d => new { d.Id, d.FullName })
                .FirstOrDefaultAsync();

            if (driver == null)
                return NotFound(new { message = "Driver not found." });

            var shipments = await _context.Shipments
                .Where(s => s.DriverId == driverId)
                .Select(s => new
                {
                    s.Id,
                    s.TrackingNumber,
                    s.Weight,
                    s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    DestinationName = s.EndStation != null ? s.EndStation.StationName : "N/A"
                })
                .ToListAsync();

            return Ok(new
            {
                driverId = driver.Id,
                driverName = driver.FullName,
                shipments
            });
        }

        [HttpPut("shipment/{id:int}/status")]
        public async Task<IActionResult> UpdateShipmentStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Status))
                return BadRequest(new { message = "Status is required." });

            var allowed = new[] { "Pending", "In Transit", "Delivered", "Cancelled" };
            if (!allowed.Contains(dto.Status))
                return BadRequest(new { message = $"Invalid status '{dto.Status}'." });

            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null) return NotFound(new { message = "Shipment not found." });

            if (User.IsInRole("Driver"))
            {
                var mine = await GetMyDriverIdAsync();
                if (shipment.DriverId != mine) return Forbid();
            }

            shipment.Status = dto.Status;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Status updated." });
        }

        [HttpPost("shipment/{id:int}/checkpoint")]
        public async Task<IActionResult> AddCheckpoint(int id, [FromBody] AddCheckpointDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.StationName))
                return BadRequest(new { message = "Station name is required." });

            var shipment = await _context.Shipments.FirstOrDefaultAsync(s => s.Id == id);
            if (shipment == null) return NotFound(new { message = "Shipment not found." });

            if (User.IsInRole("Driver"))
            {
                var mine = await GetMyDriverIdAsync();
                if (shipment.DriverId != mine) return Forbid();
            }

            _context.RouteCheckpoints.Add(new RouteCheckpoint
            {
                ShipmentId = id,
                StationName = dto.StationName,
                Info = dto.Info ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { message = "Checkpoint added." });
        }

        [HttpGet("shipment/{id:int}")]
        public async Task<IActionResult> GetShipment(int id)
        {
            var data = await _context.Shipments
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.TrackingNumber,
                    s.Weight,
                    s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    DestinationName = s.EndStation != null ? s.EndStation.StationName : "N/A"
                })
                .FirstOrDefaultAsync();

            return data == null ? NotFound() : Ok(data);
        }

        [HttpGet("shipment/{id:int}/checkpoints")]
        public async Task<IActionResult> GetCheckpoints(int id)
        {
            var cps = await _context.RouteCheckpoints
                .Where(c => c.ShipmentId == id)
                .OrderBy(c => c.Timestamp)
                .Select(c => new { c.Id, c.StationName, c.Info, c.Timestamp })
                .ToListAsync();
            return Ok(cps);
        }

        [HttpGet("lookups")]
        [AllowAnonymous]
        public IActionResult GetWarehousesAndStations()
        {
            var warehouses = _context.Warehouses
                .Select(w => new { w.Id, w.Name, w.City }).ToList();
            var stations = _context.Stations
                .Select(s => new { s.Id, s.StationName, s.Latitude, s.Longitude }).ToList();
            return Ok(new { warehouses, stations });
        }

        public class UpdateStatusDto { public string Status { get; set; } = string.Empty; }
        public class AddCheckpointDto
        {
            public string StationName { get; set; } = string.Empty;
            public string? Info { get; set; }
        }
    }
}