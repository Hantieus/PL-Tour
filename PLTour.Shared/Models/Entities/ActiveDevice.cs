using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLTour.Shared.Models.Entities;

[Table("ActiveDevices")]
public class ActiveDevice
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string DeviceId { get; set; } = string.Empty;

    [StringLength(100)]
    public string? DeviceName { get; set; }

    [StringLength(100)]
    public string? DeviceModel { get; set; }

    [StringLength(20)]
    public string? OsVersion { get; set; }

    [StringLength(20)]
    public string? AppVersion { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? BatteryLevel { get; set; }
    public bool IsCharging { get; set; }
    public DateTime LastHeartbeat { get; set; }
    public DateTime FirstSeen { get; set; }

    [StringLength(20)]
    public string? Status { get; set; }
}
