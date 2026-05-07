using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Admin.ViewModels;
using PLTour.Shared.Models.DTO;

namespace PLTour.Admin.Controllers;

[Authorize]
public class MonitorController : Controller
{
    private readonly PLTourDbContext _context;

    public MonitorController(PLTourDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cutoffOnline = DateTime.UtcNow.AddMinutes(-2);
        var cutoffStale = DateTime.UtcNow.AddMinutes(-10);

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
                LastHeartbeat = x.LastHeartbeat,
                FirstSeen = x.FirstSeen,
                Status = x.LastHeartbeat >= cutoffOnline ? "online" : x.LastHeartbeat >= cutoffStale ? "stale" : "offline"
            })
            .ToListAsync();

        var vm = new MonitorDashboardViewModel
        {
            ActiveCount = devices.Count,
            OnlineCount = devices.Count(d => d.Status == "online"),
            StaleCount = devices.Count(d => d.Status == "stale"),
            OfflineCount = devices.Count(d => d.Status == "offline"),
            Devices = devices
        };

        return View(vm);
    }

    public async Task<IActionResult> DeviceDetails(string id)
    {
        var device = await _context.ActiveDevices.FirstOrDefaultAsync(x => x.DeviceId == id);
        if (device == null)
            return NotFound();

        var cutoffOnline = DateTime.UtcNow.AddMinutes(-2);
        var cutoffStale = DateTime.UtcNow.AddMinutes(-10);
        var status = device.LastHeartbeat >= cutoffOnline ? "online" : device.LastHeartbeat >= cutoffStale ? "stale" : "offline";

        return View(new ActiveDeviceDto
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
            LastHeartbeat = device.LastHeartbeat,
            FirstSeen = device.FirstSeen,
            Status = status
        });
    }

    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        var since24Hours = DateTime.UtcNow.AddHours(-24);

        var eventTypeChart = await _context.AnalyticsEvents
            .Where(x => x.timestamp >= since24Hours)
            .GroupBy(x => x.event_type ?? "unknown")
            .Select(g => new { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .ToListAsync();

        var topDevicesChart = await _context.AnalyticsEvents
            .Where(x => x.timestamp >= since24Hours)
            .Where(x => x.device_id != null)
            .GroupBy(x => x.device_id!)
            .Select(g => new { Label = g.Key, Value = g.Count() })
            .OrderByDescending(x => x.Value)
            .Take(10)
            .ToListAsync();

        var heartbeatByHourChart = await _context.AnalyticsEvents
            .Where(x => x.timestamp >= since24Hours)
            .GroupBy(x => x.timestamp.Hour)
            .Select(g => new { Label = g.Key.ToString("00") + ":00", Value = g.Count() })
            .OrderBy(x => x.Label)
            .ToListAsync();

        var onlineByTimeChart = await _context.ActiveDevices
            .Select(x => new { x.DeviceId, x.LastHeartbeat })
            .ToListAsync();

        var onlineTimeline = new List<LabelValuePoint>();
        for (var i = 23; i >= 0; i--)
        {
            var pointTime = DateTime.UtcNow.AddHours(-i);
            var label = pointTime.ToString("HH:00");
            var onlineCount = onlineByTimeChart.Count(x => x.LastHeartbeat >= pointTime.AddMinutes(-2));
            onlineTimeline.Add(new LabelValuePoint { Label = label, Value = onlineCount });
        }

        var vm = new MonitorStatsViewModel
        {
            EventTypeChart = new ChartDataViewModel
            {
                Labels = eventTypeChart.Select(x => x.Label).ToList(),
                Values = eventTypeChart.Select(x => x.Value).ToList()
            },
            TopDevicesChart = new ChartDataViewModel
            {
                Labels = topDevicesChart.Select(x => x.Label).ToList(),
                Values = topDevicesChart.Select(x => x.Value).ToList()
            },
            HeartbeatByHourChart = new ChartDataViewModel
            {
                Labels = heartbeatByHourChart.Select(x => x.Label).ToList(),
                Values = heartbeatByHourChart.Select(x => x.Value).ToList()
            },
            OnlineByTimeChart = new ChartDataViewModel
            {
                Labels = onlineTimeline.Select(x => x.Label).ToList(),
                Values = onlineTimeline.Select(x => x.Value).ToList()
            }
        };

        return Ok(vm);
    }
}
