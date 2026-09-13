// Controllers/StationsController.cs
using LogiTrackWebV1._0.DTOs;
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StationsController : ControllerBase
    {
        private readonly LT_DBContext _context;

        public StationsController(LT_DBContext context)
        {
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetStations()
        {
            try
            {
                var stations = await _context.Stations
                    .Select(s => new LookupDto
                    {
                        Id = s.Id,
                        Name = s.StationName,
                        StationName = s.StationName,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude
                    })
                    .ToListAsync();

                if (stations.Count == 0)
                {
                    var defaultStations = new List<Station>
                    {
                        new Station { StationName = "Warehouse", Latitude = 30.0444, Longitude = 31.2357 },
                        new Station { StationName = "Sorting Hub", Latitude = 30.0626, Longitude = 31.2497 },
                        new Station { StationName = "Regional Station", Latitude = 30.1000, Longitude = 31.3000 },
                        new Station { StationName = "Out for Delivery", Latitude = 30.0500, Longitude = 31.2000 },
                        new Station { StationName = "Delivered", Latitude = 30.0000, Longitude = 31.2500 }
                    };

                    _context.Stations.AddRange(defaultStations);
                    await _context.SaveChangesAsync();

                    stations = await _context.Stations
                        .Select(s => new LookupDto
                        {
                            Id = s.Id,
                            Name = s.StationName,
                            StationName = s.StationName,
                            Latitude = s.Latitude,
                            Longitude = s.Longitude
                        })
                        .ToListAsync();
                }

                return Ok(stations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStationById(int id)
        {
            var station = await _context.Stations
                .Where(s => s.Id == id)
                .Select(s => new StationDto
                {
                    Id = s.Id,
                    Name = s.StationName,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude
                })
                .FirstOrDefaultAsync();

            if (station == null)
                return NotFound(new { message = "Station not found" });

            return Ok(station);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateStation([FromBody] CreateStationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.StationName))
                return BadRequest(new { message = "Station name is required" });

            var station = new Station
            {
                StationName = dto.StationName,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude
            };

            _context.Stations.Add(station);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Station created successfully",
                station = new LookupDto
                {
                    Id = station.Id,
                    Name = station.StationName,
                    StationName = station.StationName,
                    Latitude = station.Latitude,
                    Longitude = station.Longitude
                }
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStation(int id, [FromBody] CreateStationDto dto)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null)
                return NotFound(new { message = "Station not found" });

            if (string.IsNullOrWhiteSpace(dto.StationName))
                return BadRequest(new { message = "Station name is required" });

            station.StationName = dto.StationName;
            station.Latitude = dto.Latitude;
            station.Longitude = dto.Longitude;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Station updated successfully" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteStation(int id)
        {
            var station = await _context.Stations.FindAsync(id);
            if (station == null)
                return NotFound(new { message = "Station not found" });

            bool inUse = await _context.Shipments.AnyAsync(s => s.EndStationId == id);
            if (inUse)
                return Conflict(new { message = "Station is used by existing shipments and cannot be deleted." });

            _context.Stations.Remove(station);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Station deleted successfully" });
        }
    }

    public class CreateStationDto
    {
        public string StationName { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class StationDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}