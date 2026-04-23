using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;
using System.Text.Json;
using System.Text; // Thêm thư viện này để dùng Encoding.UTF8

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;

    public string SessionId { get; private set; }
    public string DeviceId { get; private set; }

    // Biến lưu trạng thái nghe Audio dùng chung cho toàn App
    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
        // 1. Cấu hình vượt rào SSL
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        // 2. Cấu hình URL động
        string baseUrl = "";

#if DEBUG
        // --- CẤU HÌNH KHI CHẠY DEBUG TẠI LOCAL ---
        // Dùng IP LAN của máy tính để app trên điện thoại gọi được API
        baseUrl = "http://192.168.2.6:5229/";
#else
        // --- CẤU HÌNH KHI PUBLISH / CHẤM ĐỒ ÁN (SERVER THẬT) ---
        baseUrl = "https://pl-tour-production.up.railway.app/";
#endif

        // Nối thêm endpoint của Analytics
        _apiUrl = $"{baseUrl}api/analytics/track";

        // 3. Khởi tạo định danh thiết bị
        SessionId = Guid.NewGuid().ToString();
        DeviceId = Preferences.Get("UniqueDeviceId", string.Empty);

        if (string.IsNullOrEmpty(DeviceId))
        {
            DeviceId = Guid.NewGuid().ToString();
            Preferences.Set("UniqueDeviceId", DeviceId);
        }
    }

    // Viết hàm gọi API thực tế và hiển thị Popup để Test trên điện thoại thật
    public async Task TrackEventAsync(string eventType, AnalyticsEventDto data = null)
    {
        try
        {
            if (data == null) data = new AnalyticsEventDto();

            data.EventType = eventType;
            data.SessionId = SessionId;
            data.DeviceId = DeviceId;
            data.Platform = DeviceInfo.Current.Platform.ToString();

            // Ép luôn gửi giờ chuẩn UTC từ điện thoại lên để tránh lỗi PostgreSQL
            data.Timestamp = DateTime.UtcNow;

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Gửi dữ liệu POST lên Backend
            var response = await _httpClient.PostAsync(_apiUrl, content);

            // BẬT POPUP HIỂN THỊ KẾT QUẢ TRÊN MÀN HÌNH ĐIỆN THOẠI
            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync();
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current?.MainPage?.DisplayAlert("Lỗi Server", $"Mã lỗi: {response.StatusCode}\nChi tiết: {errorDetail}", "Đóng");
                });
            }
            else
            {
                // Tạm thời bật thông báo thành công để nghiệm thu, khi nào chấm đồ án thì bạn xóa/comment dòng này đi
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current?.MainPage?.DisplayAlert("Thành công", $"Dữ liệu ({eventType}) đã lưu vào Neon Tech!", "Tuyệt vời");
                });
            }
        }
        catch (Exception ex)
        {
            // NẾU LỖI MẠNG (KHÔNG TÌM THẤY SERVER), CŨNG BẬT POPUP LÊN
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Application.Current?.MainPage?.DisplayAlert("Lỗi Kết Nối", $"Không thể gửi dữ liệu.\nLỗi: {ex.Message}\nURL: {_apiUrl}", "Đóng");
            });
        }
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