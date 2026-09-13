// Controllers/ShipmentsController.cs
using LogiTrackWebV1._0.DTOs;
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipmentsController : ControllerBase
    {
        private readonly LT_DBContext _context;

        public ShipmentsController(LT_DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetShipments()
        {
            var shipments = await _context.Shipments
                .Select(s => new ShipmentListDto
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    DriverName = s.Driver != null ? s.Driver.FullName : "N/A",
                    Weight = s.Weight,
                    Status = s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    DestinationName = s.EndStation != null ? s.EndStation.StationName : "N/A",
                    CheckpointsCount = s.RouteCheckpoints.Count,
                    LastCheckpoint = s.RouteCheckpoints
                        .OrderByDescending(c => c.Timestamp)
                        .Select(c => c.StationName)
                        .FirstOrDefault() ?? "Not started"
                })
                .ToListAsync();

            return Ok(shipments);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchShipments([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await GetShipments();

            var shipments = await _context.Shipments
                .Where(s => s.TrackingNumber.Contains(query) ||
                            (s.Driver != null && s.Driver.FullName.Contains(query)) ||
                            (s.Warehouse != null && s.Warehouse.Name.Contains(query)))
                .Select(s => new ShipmentListDto
                {
                    Id = s.Id,
                    TrackingNumber = s.TrackingNumber,
                    DriverName = s.Driver != null ? s.Driver.FullName : "N/A",
                    Weight = s.Weight,
                    Status = s.Status,
                    WarehouseName = s.Warehouse != null ? s.Warehouse.Name : "N/A",
                    DestinationName = s.EndStation != null ? s.EndStation.StationName : "N/A",
                    CheckpointsCount = s.RouteCheckpoints.Count,
                    LastCheckpoint = s.RouteCheckpoints
                        .OrderByDescending(c => c.Timestamp)
                        .Select(c => c.StationName)
                        .FirstOrDefault() ?? "Not started"
                })
                .ToListAsync();

            return Ok(shipments);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetShipmentById(int id)
        {
            var s = await _context.Shipments
                .Include(x => x.Warehouse)
                .Include(x => x.Driver)
                .Include(x => x.Address)
                .Include(x => x.ShipmentRoutes).ThenInclude(r => r.Station)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (s == null) return NotFound(new { message = "Shipment not found" });

            var checkpoints = await _context.RouteCheckpoints
                .Where(c => c.ShipmentId == id)
                .OrderBy(c => c.Timestamp)
                .Select(c => new { c.Id, c.StationName, c.Info, c.Timestamp })
                .ToListAsync();

            return Ok(new
            {
                s.Id,
                s.TrackingNumber,
                s.Weight,
                s.Status,
                Warehouse = s.Warehouse == null ? null : new { s.Warehouse.Id, s.Warehouse.Name, s.Warehouse.City },
                Driver = s.Driver == null ? null : new { s.Driver.Id, s.Driver.FullName, s.Driver.Phone },
                Address = s.Address == null ? null : new { s.Address.Building, s.Address.Street, s.Address.City },
                Checkpoints = checkpoints,
                PlannedRoute = s.ShipmentRoutes
                    .OrderBy(r => r.RouteOrder)
                    .Select(r => new
                    {
                        r.StationId,
                        StationName = r.Station != null ? r.Station.StationName : "N/A",
                        r.ArrivalDate,
                        r.RouteOrder
                    }).ToList()
            });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateShipment([FromBody] MaintainShipmentDto dto)
        {
            if (dto == null || dto.Weight <= 0 || dto.WarehouseId <= 0)
                return BadRequest(new { message = "Weight and warehouse are required." });

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);
            if (warehouse == null)
                return BadRequest(new { message = "Selected warehouse does not exist" });

            Driver? driver = null;
            if (dto.DriverId.HasValue)
            {
                driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId.Value);
                if (driver == null)
                    return BadRequest(new { message = "Selected driver does not exist" });
            }

            var shipment = new Shipment
            {
                TrackingNumber = string.IsNullOrWhiteSpace(dto.TrackingNumber)
                    ? await GenerateTrackingNumberAsync()
                    : dto.TrackingNumber,
                Weight = (decimal)dto.Weight,
                Status = dto.Status ?? "Pending",
                WarehouseId = warehouse.Id,
                DriverId = driver?.Id
            };

            if (dto.Routes != null)
            {
                foreach (var route in dto.Routes)
                {
                    shipment.ShipmentRoutes.Add(new ShipmentRoute_mtm
                    {
                        StationId = route.StationId,
                        ArrivalDate = route.ArrivalDate,
                        RouteOrder = route.RouteOrder
                    });
                }
            }

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetShipmentById), new { id = shipment.Id }, new { shipment.Id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateShipment(int id, [FromBody] MaintainShipmentDto dto)
        {
            var existingShipment = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == id);
            if (existingShipment == null)
                return NotFound(new { message = "Shipment not found" });

            if (dto.Weight <= 0 || dto.WarehouseId <= 0)
                return BadRequest(new { message = "Weight and warehouse are required." });

            var warehouse = await _context.Warehouses.FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);
            if (warehouse == null)
                return BadRequest(new { message = "Selected warehouse does not exist" });

            if (dto.DriverId.HasValue)
            {
                var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == dto.DriverId.Value);
                if (driver == null)
                    return BadRequest(new { message = "Selected driver does not exist" });
                existingShipment.DriverId = driver.Id;
            }
            else
            {
                existingShipment.DriverId = null;
            }

            existingShipment.TrackingNumber = string.IsNullOrWhiteSpace(dto.TrackingNumber)
                ? existingShipment.TrackingNumber
                : dto.TrackingNumber;
            existingShipment.Weight = (decimal)dto.Weight;
            existingShipment.Status = dto.Status ?? "Pending";
            existingShipment.WarehouseId = warehouse.Id;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipment updated successfully" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteShipment(int id)
        {
            var shipment = await _context.Shipments.FirstOrDefaultAsync(x => x.Id == id);
            if (shipment == null)
                return NotFound(new { message = "Shipment not found" });

            var checkpoints = _context.RouteCheckpoints.Where(c => c.ShipmentId == id);
            _context.RouteCheckpoints.RemoveRange(checkpoints);

            _context.Shipments.Remove(shipment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Shipment deleted successfully" });
        }

        [HttpGet("warehouses-lookup")]
        public async Task<IActionResult> GetWarehousesLookup()
        {
            var list = await _context.Warehouses
                .Select(w => new LookupDto { Id = w.Id, Name = w.Name })
                .ToListAsync();
            return Ok(list);
        }

        [HttpGet("drivers-lookup")]
        public async Task<IActionResult> GetDriversLookup()
        {
            var list = await _context.Drivers
                .Select(d => new LookupDto { Id = d.Id, Name = d.FullName })
                .ToListAsync();
            return Ok(list);
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