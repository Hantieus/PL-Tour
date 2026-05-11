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
    private readonly IConfiguration _configuration;

    public MonitorController(PLTourDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        var cutoffOnline = DateTime.UtcNow.AddMinutes(-1);
        var cutoffStale = DateTime.UtcNow.AddMinutes(-2);

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

        var githubReleasesUrl = _configuration.GetSection("AppUpdate")["GitHubReleasesUrl"] ?? string.Empty;

        ViewBag.GitHubReleasesUrl = githubReleasesUrl;

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

        var cutoffOnline = DateTime.UtcNow.AddMinutes(-1);
        var cutoffStale = DateTime.UtcNow.AddMinutes(-2);
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

}
