using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LogiTrackWebV1._0.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LogiTrackWebV1._0.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly LT_DBContext _context;
        private readonly IConfiguration _configuration;

        // Roles ordered by privilege — the frontend routes to the portal of the first match.
        private static readonly string[] RolePriority =
            { "Admin", "Warehouse", "Station", "Driver", "Customer" };

        public AuthController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            LT_DBContext context,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _configuration = configuration;
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Username) ||
                string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Username, email and password are required." });

            // Public registration can ONLY create customers.
            if (await _userManager.FindByEmailAsync(dto.Email) != null ||
                await _userManager.FindByNameAsync(dto.Username) != null)
                return BadRequest(new { message = "A user with this email or username already exists." });

            var user = new IdentityUser
            {
                Email = dto.Email.Trim(),
                UserName = dto.Username.Trim(),
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = "User creation failed.", errors = result.Errors });

            await EnsureRoleAsync("Customer");
            await _userManager.AddToRoleAsync(user, "Customer");

            return Ok(new { message = "Customer registered successfully." });
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.EmailOrUsername) ||
                string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "Username/email or Driver ID and password are required." });

            IdentityUser? user = null;

            // Normal login by email or username.
            user = await _userManager.FindByEmailAsync(dto.EmailOrUsername.Trim())
                   ?? await _userManager.FindByNameAsync(dto.EmailOrUsername.Trim());

            // Driver login by Driver.Id.
            if (user == null && int.TryParse(dto.EmailOrUsername.Trim(), out var driverId))
            {
                var driver = await _context.Drivers.AsNoTracking()
                    .FirstOrDefaultAsync(d => d.Id == driverId);

                if (driver?.UserId != null)
                    user = await _userManager.FindByIdAsync(driver.UserId);
            }

            if (user == null || !await _userManager.CheckPasswordAsync(user, dto.Password))
                return Unauthorized(new { message = "Invalid credentials." });

            var userRoles = await _userManager.GetRolesAsync(user);

            if (userRoles.Count == 0)
                return Unauthorized(new { message = "User has no assigned role." });

            // Order roles by privilege so the frontend picks the most privileged portal.
            var orderedRoles = userRoles
                .OrderBy(r =>
                {
                    var idx = Array.IndexOf(RolePriority, r);
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ToList();

            // Lookup driverId (if this user is linked to a driver)
            var driverIdForToken = await _context.Drivers
                .Where(d => d.UserId == user.Id)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            if (driverIdForToken.HasValue)
                authClaims.Add(new Claim("DriverId", driverIdForToken.Value.ToString()));

            // Claims in the same priority order.
            foreach (var role in orderedRoles)
                authClaims.Add(new Claim(ClaimTypes.Role, role));

            var token = GenerateJwtToken(authClaims);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                expiration = token.ValidTo,
                user = new
                {
                    id = user.Id,
                    userName = user.UserName,
                    email = user.Email,
                    roles = orderedRoles
                },
                driverId = driverIdForToken
            });
        }

        private async Task EnsureRoleAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole(role));
        }

        private JwtSecurityToken GenerateJwtToken(List<Claim> authClaims)
        {
            var jwtSecret = _configuration["Jwt:Key"];
            if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
                throw new InvalidOperationException("Jwt:Key must be configured and contain at least 32 bytes.");

            var issuer = _configuration["Jwt:Issuer"];
            var audience = _configuration["Jwt:Audience"];
            if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
                throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
            var minutes = int.TryParse(_configuration["Jwt:DurationInMinutes"], out var value) && value > 0
                ? value : 120;

            return new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                expires: DateTime.UtcNow.AddMinutes(minutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        }
    }

    public class RegisterDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Role { get; set; }   // ignored
    }

    public class LoginDto
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}