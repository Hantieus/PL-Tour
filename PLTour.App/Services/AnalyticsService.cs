using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;
using System.Text;
using System.Text.Json;

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService? _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly DeviceMonitorService _monitorService = DeviceMonitorService.Instance ?? new DeviceMonitorService();

    public string SessionId => _monitorService.SessionId;
    public string DeviceId => _monitorService.DeviceId;

    // Lưu trạng thái nghe audio
    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
    }

    /// <summary>
    /// Gửi sự kiện lên server (bất đồng bộ, không chờ, không ảnh hưởng UI)
    /// </summary>
    private void TrackEvent(string eventType, AnalyticsEventDto? data = null)
    {
        _ = _monitorService.TrackEventAsync(eventType, data);
    }

    // ==================== CÁC PHƯƠNG THỨC GỌI TỪ APP ====================

    /// <summary> Gửi vị trí hiện tại (dùng cho heatmap) </summary>
    public async Task TrackLocationPingAsync(double lat, double lng)
    {
        await Task.Run(() => TrackEvent("location_ping", new AnalyticsEventDto { Latitude = lat, Longitude = lng }));
    }

    public async Task TrackPoiViewAsync(int locationId)
    {
        await Task.Run(() => TrackEvent("view_location", new AnalyticsEventDto { LocationId = locationId }));
    }

    public async Task TrackAudioStartAsync(int locationId, string languageCode, bool isOnSite)
    {
        _playbackStartTime = DateTime.UtcNow;
        _currentTrackedLocationId = locationId;
        string eventType = isOnSite ? "listen_onsite" : "listen_remote";
        await Task.Run(() => TrackEvent(eventType, new AnalyticsEventDto { LocationId = locationId, LanguageCode = languageCode, HasAudio = true }));
    }

    public async Task TrackAudioStopAsync()
    {
        if (_playbackStartTime.HasValue && _currentTrackedLocationId > 0)
        {
            int seconds = (int)(DateTime.UtcNow - _playbackStartTime.Value).TotalSeconds;
            if (seconds > 0)
            {
                await Task.Run(() => TrackEvent("listen_duration", new AnalyticsEventDto { LocationId = _currentTrackedLocationId, Duration = seconds }));
            }
            _playbackStartTime = null;
            _currentTrackedLocationId = 0;
        }
    }

}