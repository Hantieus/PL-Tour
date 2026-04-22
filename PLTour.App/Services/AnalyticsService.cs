using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;
using System.Text.Json;

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl = "https://your-api-url.com/api/analytics/track";

    public string SessionId { get; private set; }
    public string DeviceId { get; private set; }

    // Biến lưu trạng thái nghe Audio dùng chung cho toàn App
    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
        _httpClient = new HttpClient();
        SessionId = Guid.NewGuid().ToString();
        DeviceId = Preferences.Get("UniqueDeviceId", string.Empty);
        if (string.IsNullOrEmpty(DeviceId))
        {
            DeviceId = Guid.NewGuid().ToString();
            Preferences.Set("UniqueDeviceId", DeviceId);
        }
    }

    public async Task TrackEventAsync(string eventType, AnalyticsEventDto data = null)
    {
        // ... (Giữ nguyên logic hàm TrackEventAsync cũ của bạn) ...
    }

    // 1. Lưu tuyến di chuyển & Heatmap
    public async Task TrackLocationPingAsync(double lat, double lng)
    {
        await TrackEventAsync("location_ping", new AnalyticsEventDto
        {
            Latitude = lat,
            Longitude = lng
        });
    }

    // 2. Track xem chi tiết POI
    public async Task TrackPoiViewAsync(int locationId)
    {
        await TrackEventAsync("view_location", new AnalyticsEventDto { LocationId = locationId });
    }

    // 3. Track sự kiện bắt đầu nghe (Top địa điểm nghe nhiều nhất)
    public async Task TrackAudioStartAsync(int locationId, string languageCode, bool isOnSite)
    {
        _playbackStartTime = DateTime.UtcNow;
        _currentTrackedLocationId = locationId;

        string eventType = isOnSite ? "listen_onsite" : "listen_remote";
        await TrackEventAsync(eventType, new AnalyticsEventDto
        {
            LocationId = locationId,
            LanguageCode = languageCode,
            HasAudio = true
        });
    }

    // 4. Track sự kiện kết thúc nghe (Thời gian trung bình nghe 1 POI)
    public async Task TrackAudioStopAsync()
    {
        if (_playbackStartTime.HasValue && _currentTrackedLocationId > 0)
        {
            int secondsListened = (int)(DateTime.UtcNow - _playbackStartTime.Value).TotalSeconds;

            await TrackEventAsync("listen_duration", new AnalyticsEventDto
            {
                LocationId = _currentTrackedLocationId,
                Duration = secondsListened
            });

            // Reset trạng thái
            _playbackStartTime = null;
            _currentTrackedLocationId = 0;
        }
    }
}