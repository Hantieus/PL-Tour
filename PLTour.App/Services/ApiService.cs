using System.Net.Http.Json;
using PLTour.App.Models;
using PLTour.Shared.Models.DTO;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices; // Bắt buộc thêm dòng này để dùng DeviceInfo

namespace PLTour.App.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    // Base URL của Backend (Server)
    private readonly string _baseUrl;

    public ApiService()
    {
#if DEBUG
        // --- CẤU HÌNH KHI CHẠY DEBUG TẠI LOCAL ---
        // IP LAN của máy tính: dùng cho điện thoại thật bắt chung mạng wifi với máy tính
        _baseUrl = "http://192.168.2.6:5229/";
        //P: 192.168.100.123:5229
        //L: 192.168.2.6:5229
#else
        // --- CẤU HÌNH KHI PUBLISH / CHẤM ĐỒ ÁN (SERVER THẬT) ---
        // Thay bằng domain hoặc IP server thật của bạn
        _baseUrl = "https://pl-tour-production.up.railway.app/"; 
#endif

        System.Diagnostics.Debug.WriteLine($"[API_LOG] App đang kết nối tới: {_baseUrl}");

        var handler = new HttpClientHandler
        {
            // Bỏ qua lỗi chứng chỉ SSL khi chạy HTTP ở local
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<List<TourModel>> GetToursAsync()
    {
        try
        {
            var tourDtos = await _httpClient.GetFromJsonAsync<List<TourDto>>("api/tours");
            if (tourDtos == null || !tourDtos.Any()) return new List<TourModel>();

            return tourDtos.Select(MapToTourModel).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetTours: {ex}");
            return new List<TourModel>();
        }
    }

    public async Task<List<PoiModel>> GetAllLocationsAsync()
    {
        try
        {
            var locDtos = await _httpClient.GetFromJsonAsync<List<PLTour.Shared.Models.DTO.LocationDto>>("api/Locations");
            if (locDtos != null && locDtos.Any())
                return locDtos.Select(loc => MapToPoiModel(loc)).ToList();

            System.Diagnostics.Debug.WriteLine("[API_LOG] api/Locations returned empty. Falling back to tours locations.");

            var tours = await GetToursAsync();
            var fallbackPois = tours
                .Where(t => t.Pois != null)
                .SelectMany(t => t.Pois)
                .Where(p => p != null)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .ToList();

            System.Diagnostics.Debug.WriteLine($"[API_LOG] Fallback POIs from tours: {fallbackPois.Count}");
            return fallbackPois;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetAllLocations: {ex}");

            try
            {
                var tours = await GetToursAsync();
                return tours
                    .Where(t => t.Pois != null)
                    .SelectMany(t => t.Pois)
                    .Where(p => p != null)
                    .GroupBy(p => p.Id)
                    .Select(g => g.First())
                    .ToList();
            }
            catch (Exception fallbackEx)
            {
                System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetAllLocations fallback failed: {fallbackEx}");
                return new List<PoiModel>();
            }
        }
    }

    public async Task<string> GetAudioLinkAsync(string text, string langCode, int narrationId)
    {
        try
        {
            string url = $"api/Audio/generate?text={Uri.EscapeDataString(text)}&langCode={langCode}&narrationId={narrationId}";
            var response = await _httpClient.GetFromJsonAsync<AudioResponseDto>(url);
            return response?.Url;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] Lỗi gọi API tạo Audio: {ex.Message}");
            return null;
        }
    }

    // --- Hàm bổ trợ để Map dữ liệu ---
    private TourModel MapToTourModel(TourDto dto)
    {
        var poisList = dto.Locations?.Select(MapToPoiModel).ToList() ?? new List<PoiModel>();

        return new TourModel
        {
            Id = dto.TourId.ToString(),
            Name = dto.Name,
            Duration = dto.Duration,
            IntroText = dto.IntroText,
            ImageUrl = FormatImageUrl(dto.ImageUrl),
            Pois = poisList,
            Latitude = poisList.Any() ? poisList.First().Lat : 0,
            Longitude = poisList.Any() ? poisList.First().Lng : 0
        };
    }

    private PoiModel MapToPoiModel(PLTour.Shared.Models.DTO.LocationDto loc)
    {
        string selectedLangCode = Preferences.Default.Get("UserLanguage", "vi");

        var narration = loc.Narrations?.FirstOrDefault(n => n.LanguageCode == selectedLangCode)
                        ?? loc.Narrations?.FirstOrDefault(n => n.LanguageId == 1)
                        ?? loc.Narrations?.FirstOrDefault();

        string poiImageUrl = "tour_thumb.jpg";
        if (!string.IsNullOrEmpty(loc.ImageUrl))
        {
            if (loc.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                poiImageUrl = loc.ImageUrl;
            }
            else
            {
                var cleanPath = loc.ImageUrl.Replace("\\", "/").TrimStart('/');
                poiImageUrl = $"{_baseUrl.TrimEnd('/')}/{cleanPath}";
            }
        }

        System.Diagnostics.Debug.WriteLine($"[DEBUG_IMAGE] Link ảnh cuối cùng: {poiImageUrl}");

        return new PoiModel
        {
            Id = loc.LocationId,
            ImageUrl = poiImageUrl,
            NarrationId = narration?.NarrationId ?? 0,
            AudioUrl = narration?.AudioUrl,
            FullContent = narration?.Content,
            LanguageName = narration?.LanguageName ?? "Tiếng Việt",
            LanguageId = narration?.LanguageId ?? 1,
            LanguageCode = narration?.LanguageCode ?? "vi",
            Name = loc.Name,
            Lat = loc.Latitude,
            Lng = loc.Longitude,
            Radius = loc.Radius > 0 ? loc.Radius : 150,
            Description = loc.Description ?? "",
            Address = loc.Address ?? "",
            CategoryId = loc.CategoryId,
            Category = !string.IsNullOrEmpty(loc.CategoryName) ? loc.CategoryName : MapCategoryName(loc.CategoryId),
            PinColor = GetPinColor(loc.CategoryId)
        };
    }

    /// <summary>
    /// Hàm xử lý logic nối chuỗi URL hình ảnh
    /// </summary>
    private string FormatImageUrl(string rawUrl)
    {
        if (string.IsNullOrEmpty(rawUrl))
            return "tour_thumb.jpg"; // Ảnh fallback nếu data trống

        if (rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return rawUrl;

        // Xử lý trường hợp DB trả về "/uploads/locations/..."
        // Đảm bảo không bị dư dấu "/" khi nối với _baseUrl
        return $"{_baseUrl.TrimEnd('/')}/{rawUrl.TrimStart('/')}";
    }

    private string MapCategoryName(int categoryId) => categoryId switch
    {
        1 => "Tham quan",
        2 => "Ăn uống",
        3 => "Sự kiện",
        _ => "Tham quan"
    };

    private MapsuiColor GetPinColor(int categoryId) => categoryId switch
    {
        1 => MapsuiColor.Red,
        2 => MapsuiColor.Orange,
        3 => MapsuiColor.Purple,
        _ => MapsuiColor.Blue
    };

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/tours");
            System.Diagnostics.Debug.WriteLine($"[API_LOG] TestConnection status: {(int)response.StatusCode} {response.StatusCode}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] TestConnection: {ex}");
            return false;
        }
    }
}

public class AudioResponseDto
{
    public string Url { get; set; }
}