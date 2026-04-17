using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Share.Models;
using PLTour.Shared.Models.DTO; // Đã đổi sang Shared và DTO
using System.Text.Json;

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl = "https://your-api-url.com/api/analytics/track"; // Thay bằng URL API thật của bạn

    public string SessionId { get; private set; }
    public string DeviceId { get; private set; }

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
        try
        {
            if (data == null) data = new AnalyticsEventDto();

            data.EventType = eventType;
            data.SessionId = SessionId;
            data.DeviceId = DeviceId;
            data.Platform = DeviceInfo.Current.Platform.ToString();
            data.Timestamp = DateTime.UtcNow;

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            _ = _httpClient.PostAsync(_apiUrl, content);

            System.Diagnostics.Debug.WriteLine($"[TRACKING] Gửi sự kiện: {eventType} - Dữ liệu: {json}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TRACKING LỖI] {ex.Message}");
        }
    }
}