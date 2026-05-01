namespace PLTour.Shared.Models.DTO;

public class MonitorHeartbeatDto
{
    public string? DeviceId { get; set; }
    public string? SessionId { get; set; }
    public string? Reason { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public string? Platform { get; set; }
    public int? BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}

public class ActiveDeviceDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime FirstSeen { get; set; }
    public string? Status { get; set; }
}
