using LogiTrackWebV1._0.DTOs;
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogiTrackWebV1._0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DriversController : ControllerBase
    {
        private readonly LT_DBContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DriversController(LT_DBContext context,
                                 UserManager<IdentityUser> userManager,
                                 RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Get()
        {
            var drivers = await _context.Drivers
                .Select(d => new DriverDTO
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    Phone = d.Phone,
                    LicenseNumber = d.License != null ? d.License.LicenseNumber : "N/A",
                    ExpiryDate = d.License != null ? (DateTime?)d.License.ExpiryDate : null,
                    UserId = d.UserId,
                    Username = d.UserId == null
                        ? null
                        : _userManager.Users
                            .Where(u => u.Id == d.UserId)
                            .Select(u => u.UserName)
                            .FirstOrDefault()
                })
                .ToListAsync();

            return Ok(drivers);
        }

        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetById(int id)
        {
            var driver = await _context.Drivers
                .Where(d => d.Id == id)
                .Select(d => new DriverDTO
                {
                    Id = d.Id,
                    FullName = d.FullName,
                    Phone = d.Phone,
                    LicenseNumber = d.License != null ? d.License.LicenseNumber : "N/A",
                    ExpiryDate = d.License != null ? (DateTime?)d.License.ExpiryDate : null,
                    UserId = d.UserId,
                    Username = d.UserId == null
                        ? null
                        : _userManager.Users
                            .Where(u => u.Id == d.UserId)
                            .Select(u => u.UserName)
                            .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            return driver == null
                ? NotFound(new { message = $"Driver with id {id} not found." })
                : Ok(driver);
        }

        [HttpGet("{id:int}/shipments")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDriverShipments(int id)
        {
            if (!await _context.Drivers.AnyAsync(d => d.Id == id))
                return NotFound(new { message = $"Driver with id {id} not found." });

            var shipments = await _context.Shipments
                .Where(s => s.DriverId == id)
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

            return Ok(shipments);
        }

        [HttpGet("search")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Search([FromQuery] string? name,
                                                [FromQuery] string? phone,
                                                [FromQuery] int? id)
        {
            var query = _context.Drivers.AsQueryable();
            if (id.HasValue) query = query.Where(d => d.Id == id.Value);
            if (!string.IsNullOrWhiteSpace(name)) query = query.Where(d => d.FullName.Contains(name));
            if (!string.IsNullOrWhiteSpace(phone)) query = query.Where(d => d.Phone.Contains(phone));

            var drivers = await query.Select(d => new DriverDTO
            {
                Id = d.Id,
                FullName = d.FullName,
                Phone = d.Phone,
                LicenseNumber = d.License != null ? d.License.LicenseNumber : "N/A",
                ExpiryDate = d.License != null ? (DateTime?)d.License.ExpiryDate : null,
                UserId = d.UserId,
                Username = d.UserId == null
                    ? null
                    : _userManager.Users
                        .Where(u => u.Id == d.UserId)
                        .Select(u => u.UserName)
                        .FirstOrDefault()
            }).ToListAsync();

            return Ok(drivers);
        }

        [HttpGet("available-accounts")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAvailableAccounts()
        {
            var linkedIds = await _context.Drivers
                .Where(d => d.UserId != null)
                .Select(d => d.UserId!)
                .ToListAsync();

            var users = await _userManager.GetUsersInRoleAsync("Driver");

            var available = users
                .Where(u => !linkedIds.Contains(u.Id))
                .Select(u => new { id = u.Id, username = u.UserName, email = u.Email })
                .ToList();

            return Ok(available);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Post([FromBody] DriverDTO dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
                return BadRequest(new { message = "Full name and phone are required." });

            if (!string.IsNullOrWhiteSpace(dto.LicenseNumber) &&
                await _context.Drivers.AnyAsync(d => d.License != null && d.License.LicenseNumber == dto.LicenseNumber))
                return Conflict(new { message = "License number already exists." });

            var driver = new Driver
            {
                FullName = dto.FullName.Trim(),
                Phone = dto.Phone.Trim()
            };

            // Only attach a license if one was provided (LicenseNumber or ExpiryDate).
            if (!string.IsNullOrWhiteSpace(dto.LicenseNumber) || dto.ExpiryDate.HasValue)
            {
                driver.License = new DriverLicense
                {
                    LicenseNumber = (dto.LicenseNumber ?? string.Empty).Trim(),
                    ExpiryDate = dto.ExpiryDate ?? DateTime.UtcNow.AddYears(1)
                };
            }

            _context.Drivers.Add(driver);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = driver.Id }, new { driver.Id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Put(int id, [FromBody] DriverDTO dto)
        {
            var driver = await _context.Drivers
                .Include(d => d.License)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (driver == null) return NotFound(new { message = $"Driver with id {id} not found." });

            driver.FullName = (dto.FullName ?? string.Empty).Trim();
            driver.Phone = (dto.Phone ?? string.Empty).Trim();

            if (!string.IsNullOrWhiteSpace(dto.LicenseNumber) || dto.ExpiryDate.HasValue)
            {
                if (driver.License == null)
                    driver.License = new DriverLicense { DriverId = driver.Id };

                driver.License.LicenseNumber = (dto.LicenseNumber ?? string.Empty).Trim();
                driver.License.ExpiryDate = dto.ExpiryDate ?? DateTime.UtcNow.AddYears(1);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/account")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateOrLinkAccount(int id, [FromBody] CreateDriverAccountDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver == null) return NotFound(new { message = "Driver not found." });

            if (driver.UserId != null)
                return Conflict(new { message = "This driver already has a login account." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Username, email and password are required." });

            if (await _userManager.FindByNameAsync(dto.Username.Trim()) != null ||
                await _userManager.FindByEmailAsync(dto.Email.Trim()) != null)
                return Conflict(new { message = "Username or email is already in use." });

            if (!await _roleManager.RoleExistsAsync("Driver"))
                await _roleManager.CreateAsync(new IdentityRole("Driver"));

            var user = new IdentityUser
            {
                UserName = dto.Username.Trim(),
                Email = dto.Email.Trim(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "Driver account creation failed.", errors = result.Errors });

            var roleResult = await _userManager.AddToRoleAsync(user, "Driver");
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                return BadRequest(new { message = "Could not assign Driver role.", errors = roleResult.Errors });
            }

            driver.UserId = user.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Driver account linked successfully.", username = user.UserName });
        }

        [HttpPut("{id:int}/account")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> LinkExistingAccount(int id, [FromBody] LinkDriverAccountDto dto)
        {
            var driver = await _context.Drivers.FirstOrDefaultAsync(d => d.Id == id);
            if (driver == null) return NotFound(new { message = "Driver not found." });
            if (driver.UserId != null) return Conflict(new { message = "This driver already has an account." });

            if (dto == null || string.IsNullOrWhiteSpace(dto.UserId))
                return BadRequest(new { message = "userId is required." });

            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound(new { message = "User account not found." });

            if (!await _userManager.IsInRoleAsync(user, "Driver"))
                return BadRequest(new { message = "Selected account is not a Driver account." });

            if (await _context.Drivers.AnyAsync(d => d.UserId == user.Id))
                return Conflict(new { message = "That account is already linked to another driver." });

            driver.UserId = user.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Driver account linked successfully.", username = user.UserName });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null) return NotFound(new { message = $"Driver with id {id} not found." });

            if (await _context.Shipments.AnyAsync(s => s.DriverId == id))
                return Conflict(new { message = "Driver still has shipments assigned and cannot be deleted." });

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class LinkDriverAccountDto
    {
        public string UserId { get; set; } = string.Empty;
    }
}