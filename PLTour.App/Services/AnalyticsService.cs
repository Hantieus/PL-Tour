using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;
using System.Text.Json;
using System.Text;

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public string SessionId { get; private set; }
    public string DeviceId { get; private set; }

    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        string baseUrl = "";
#if DEBUG
        baseUrl = "http://192.168.2.6:5229/";
#else
        baseUrl = "https://pl-tour-production.up.railway.app/";
#endif

        _apiUrl = $"{baseUrl}api/analytics/track";

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

            // XỬ LÝ HIỂN THỊ THÔNG BÁO THEO CHUẨN MAUI MỚI (FIX LỖI OBSOLETE)
            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync();
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage != null)
                    {
                        await currentPage.DisplayAlert("Lỗi Server", $"Mã lỗi: {response.StatusCode}\nChi tiết: {errorDetail}", "Đóng");
                    }
                });
            }
            else
            {
                // Thông báo thành công để nghiệm thu
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var currentPage = Application.Current?.Windows[0]?.Page;
                    if (currentPage != null)
                    {
                        await currentPage.DisplayAlert("Thành công", $"Dữ liệu ({eventType}) đã lưu vào hệ thống!", "Đóng");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                var currentPage = Application.Current?.Windows[0]?.Page;
                if (currentPage != null)
                {
                    await currentPage.DisplayAlert("Lỗi Kết Nối", $"Không thể gửi dữ liệu.\nLỗi: {ex.Message}", "Đóng");
                }
            });
        }
    }

    public async Task TrackLocationPingAsync(double lat, double lng)
    {
        await TrackEventAsync("location_ping", new AnalyticsEventDto
        {
            Latitude = lat,
            Longitude = lng
        });
    }

    public async Task TrackPoiViewAsync(int locationId)
    {
        await TrackEventAsync("view_location", new AnalyticsEventDto { LocationId = locationId });
    }

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

            _playbackStartTime = null;
            _currentTrackedLocationId = 0;
        }
    }
}