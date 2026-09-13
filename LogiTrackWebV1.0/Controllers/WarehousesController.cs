// Controllers/WarehousesController.cs
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly LT_DBContext _context;

        public WarehousesController(LT_DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var warehouses = await _context.Warehouses.ToListAsync();
            return Ok(warehouses);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchWarehouse([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return await Get();

            var warehouses = await _context.Warehouses
                .Where(w => w.Name.Contains(query) || w.City.Contains(query))
                .ToListAsync();

            return Ok(warehouses);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateWarehouse([FromBody] Warehouse warehouse)
        {
            if (warehouse == null || string.IsNullOrWhiteSpace(warehouse.Name))
                return BadRequest(new { message = "Warehouse name is required." });

            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = warehouse.Id }, warehouse);
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] Warehouse updatedWarehouse)
        {
            if (updatedWarehouse == null)
                return BadRequest(new { message = "Body is required." });

            if (updatedWarehouse.Id != 0 && updatedWarehouse.Id != id)
                return BadRequest(new { message = "Route id does not match the warehouse id in the body" });

            if (!await WarehouseExistsAsync(id))
                return NotFound(new { message = "Warehouse not found" });

            updatedWarehouse.Id = id;
            _context.Entry(updatedWarehouse).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _context.Warehouses.FindAsync(id);
            if (warehouse == null)
                return NotFound(new { message = "Warehouse not found" });

            _context.Warehouses.Remove(warehouse);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Warehouse deleted successfully" });
        }

        [HttpGet("{id}/shipments")]
        public async Task<IActionResult> GetWarehouseShipments(int id)
        {
            if (!await WarehouseExistsAsync(id))
                return NotFound(new { message = "Warehouse not found" });

            var shipments = await _context.Shipments
                .Where(s => s.WarehouseId == id)
                .Select(s => new
                {
                    s.Id,
                    s.TrackingNumber,
                    s.Weight,
                    s.Status,
                    DestinationName = s.EndStation != null ? s.EndStation.StationName : "N/A"
                })
                .ToListAsync();

            return Ok(shipments);
        }

        private Task<bool> WarehouseExistsAsync(int id)
        {
            return _context.Warehouses.AnyAsync(e => e.Id == id);
        }
    }
}