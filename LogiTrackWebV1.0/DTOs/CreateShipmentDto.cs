using System;
using System.Collections.Generic;

namespace LogiTrackWebV1._0.DTOs
{
    public class ShipmentListDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public AddressDto addressDto { get; set; } = new AddressDto();
        public List<CheckpointDto> Checkpoints { get; set; } = new List<CheckpointDto>();
        public int CheckpointsCount { get; set; }
        public string LastCheckpoint { get; set; } = "Not started";
        public string DestinationName { get; set; } = string.Empty;
    }

    public class AddressDto
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
    }

    public class CheckpointDto
    {
        public int Id { get; set; }
        public string StationName { get; set; } = string.Empty;
        public string Info { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class MaintainShipmentDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public double Weight { get; set; }
        public string Status { get; set; } = "Pending";
        public int WarehouseId { get; set; }
        public int? DriverId { get; set; }   // nullable for unassigned customer requests
        public string? WarehouseName { get; set; }
        public string? DriverName { get; set; }
        public List<RouteDto>? Routes { get; set; }
    }

    public class RouteDto
    {
        public int StationId { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int RouteOrder { get; set; }
    }

    public class CreateRouteDto
    {
        public int StationId { get; set; }
        public DateTime ArrivalDate { get; set; }
        public int RouteOrder { get; set; }
    }

    public class LookupDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? StationName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? City { get; set; }
    }

    public class CreateCheckpointDto
    {
        public string StationName { get; set; } = string.Empty;
        public string? Info { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class UpdateCheckpointDto
    {
        public string? StationName { get; set; }
        public string? Info { get; set; }
        public DateTime? Timestamp { get; set; }
    }

    public class ShipmentDetailsDto
    {
        public int Id { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public AddressDto Address { get; set; } = new AddressDto();
        public List<CheckpointDto> Checkpoints { get; set; } = new List<CheckpointDto>();
        public string DestinationName { get; set; } = string.Empty;
    }

    public class UserLoginDto
    {
        public string? username { get; set; }
        public string? password { get; set; }
    }

    public class AuthResponseDto
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? DriverId { get; set; }
    }
}