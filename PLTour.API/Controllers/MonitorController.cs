using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.DTO;
using PLTour.Shared.Models.Entities;

namespace PLTour.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MonitorController : ControllerBase
{
    private readonly PLTourDbContext _context;

    public MonitorController(PLTourDbContext context)
    {
        _context = context;
    }

    [HttpPost("heartbeat")]
    public async Task<IActionResult> Heartbeat([FromBody] MonitorHeartbeatDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.DeviceId))
            return BadRequest("DeviceId is required.");

        var now = DateTime.UtcNow;
        var batteryLevel = dto.BatteryLevel.HasValue
            ? Math.Clamp(dto.BatteryLevel.Value, 0, 100)
            : (int?)null;
        var status = GetStatus(now);

        var device = await _context.ActiveDevices.FirstOrDefaultAsync(x => x.DeviceId == dto.DeviceId);
        if (device == null)
        {
            device = new ActiveDevice
            {
                DeviceId = dto.DeviceId,
                FirstSeen = now,
                LastHeartbeat = now
            };
            _context.ActiveDevices.Add(device);
        }

        device.DeviceName = dto.DeviceName;
        device.DeviceModel = dto.DeviceModel;
        device.OsVersion = dto.OsVersion;
        device.AppVersion = dto.AppVersion;
        device.Latitude = dto.Latitude;
        device.Longitude = dto.Longitude;
        device.BatteryLevel = batteryLevel;
        device.IsCharging = dto.IsCharging;
        device.LastHeartbeat = now;
        device.Status = status;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Heartbeat saved.",
            deviceId = device.DeviceId,
            status = device.Status
        });
    }

    [HttpPost("event")]
    public async Task<IActionResult> TrackEvent([FromBody] AnalyticsEventDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid payload.");

        var newEvent = new AnalyticsEvent
        {
            session_id = dto.SessionId,
            device_id = dto.DeviceId,
            event_type = dto.EventType,
            location_id = dto.LocationId,
            tour_id = dto.TourId,
            language_code = dto.LanguageCode,
            duration = dto.Duration,
            keyword = dto.Keyword,
            platform = dto.Platform,
            has_audio = dto.HasAudio,
            latitude = dto.Latitude,
            longitude = dto.Longitude,
            timestamp = DateTime.UtcNow
        };

        _context.AnalyticsEvents.Add(newEvent);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Event saved." });
    }

    [HttpGet("active-devices")]
    public async Task<IActionResult> GetActiveDevices()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-2);

        var devices = await _context.ActiveDevices
            .OrderByDescending(x => x.LastHeartbeat)
            .Select(x => new ActiveDeviceDto
            {
                Id = x.Id,
                DeviceId = x.DeviceId,
                DeviceName = x.DeviceName,
                DeviceModel = x.DeviceModel,
                OsVersion = x.OsVersion,
                AppVersion = x.AppVersion,
                Latitude = x.Latitude,
                Longitude = x.Longitude,
                BatteryLevel = x.BatteryLevel,
                IsCharging = x.IsCharging,
                LastHeartbeat = DateTime.SpecifyKind(x.LastHeartbeat, DateTimeKind.Utc),
                FirstSeen = DateTime.SpecifyKind(x.FirstSeen, DateTimeKind.Utc),
                Status = x.LastHeartbeat >= cutoff ? x.Status : "offline"
            })
            .ToListAsync();

        return Ok(devices);
    }

    [HttpGet("active-devices/{deviceId}")]
    public async Task<IActionResult> GetDeviceById(string deviceId)
    {
        var device = await _context.ActiveDevices.FirstOrDefaultAsync(x => x.DeviceId == deviceId);
        if (device == null)
            return NotFound();

        return Ok(new ActiveDeviceDto
        {
            Id = device.Id,
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            DeviceModel = device.DeviceModel,
            OsVersion = device.OsVersion,
            AppVersion = device.AppVersion,
            Latitude = device.Latitude,
            Longitude = device.Longitude,
            BatteryLevel = device.BatteryLevel,
            IsCharging = device.IsCharging,
            LastHeartbeat = DateTime.SpecifyKind(device.LastHeartbeat, DateTimeKind.Utc),
            FirstSeen = DateTime.SpecifyKind(device.FirstSeen, DateTimeKind.Utc),
            Status = device.Status
        });
    }

    [HttpPost("mark-offline")]
    public async Task<IActionResult> MarkOfflineDevices()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-10);
        var staleDevices = await _context.ActiveDevices
            .Where(x => x.LastHeartbeat < cutoff && x.Status != "offline")
            .ToListAsync();

        foreach (var device in staleDevices)
        {
            device.Status = "offline";
        }

        await _context.SaveChangesAsync();
        return Ok(new { updated = staleDevices.Count });
    }

    private static string GetStatus(DateTime lastHeartbeat)
    {
        var minutes = (DateTime.UtcNow - lastHeartbeat).TotalMinutes;
        if (minutes <= 2)
            return "online";

        if (minutes <= 10)
            return "stale";

        return "offline";
    }
}
