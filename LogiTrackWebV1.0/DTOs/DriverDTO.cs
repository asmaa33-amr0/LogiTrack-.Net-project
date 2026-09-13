using System;

namespace LogiTrackWebV1._0.DTOs
{
    public class DriverDTO
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string? UserId { get; set; }
        public string? Username { get; set; }
    }

    public class CreateDriverAccountDto
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LinkDriverAccountDto
    {
        public string UserId { get; set; } = string.Empty;
    }
}