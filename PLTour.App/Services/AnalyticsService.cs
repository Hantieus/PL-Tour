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

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public string SessionId { get; private set; }
    public string DeviceId { get; private set; }

    // Lưu trạng thái nghe audio
    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
        // Bỏ qua SSL certificate (chỉ dùng cho debug)
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Địa chỉ API - ANH PHẢI SỬA IP NÀY ĐÚNG
        string baseUrl = "http://192.168.100.123:5229/";
        _apiUrl = $"{baseUrl}api/analytics/track";

        // Khởi tạo SessionId (mới mỗi lần mở app)
        SessionId = Guid.NewGuid().ToString();

        // Lấy hoặc tạo DeviceId (lưu vĩnh viễn)
        DeviceId = Preferences.Get("AnalyticsDeviceId", string.Empty);
        if (string.IsNullOrEmpty(DeviceId))
        {
            DeviceId = Guid.NewGuid().ToString();
            Preferences.Set("AnalyticsDeviceId", DeviceId);
        }
    }

    /// <summary>
    /// Gửi sự kiện lên server (bất đồng bộ, không chờ, không ảnh hưởng UI)
    /// </summary>
    private void TrackEvent(string eventType, AnalyticsEventDto? data = null)
    {
        // Chạy fire-and-forget, không await để không block UI
        Task.Run(async () =>
        {
            try
            {
                if (data == null) data = new AnalyticsEventDto();

                data.EventType = eventType;
                data.SessionId = SessionId;
                data.DeviceId = DeviceId;
                data.Platform = DeviceInfo.Current.Platform.ToString();
                data.Timestamp = DateTime.UtcNow;

                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Analytics] {eventType} sent");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[Analytics] {eventType} failed: {response.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Analytics] {eventType} error: {ex.Message}");
            }
        });
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