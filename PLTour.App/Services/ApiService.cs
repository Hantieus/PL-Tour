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
    //Dùng DevTunnelUrl không cần phải dùng chung 1 mạng wifi của máy tính và điện thoạii nhưng vẫn chạy được
    private const string DevTunnelUrl = "https://cr7jqdb9-7291.asse.devtunnels.ms/";

    //Không cân bật api nhưng vẫn chạy được app
    private const string RenderUrl = "https://pl-tour.onrender.com/";

    public ApiService()
    {
        // Dùng chung cho cả DEBUG và RELEASE/PUBLISH:
        // - PLTOUR_USE_DEVTUNNEL=true/1 => DevTunnelUrl
        // - ngược lại => RenderUrl
        var useDevTunnel = Environment.GetEnvironmentVariable("PLTOUR_USE_DEVTUNNEL")
            ?.Trim()
            .Equals("true", StringComparison.OrdinalIgnoreCase) == true
            || Environment.GetEnvironmentVariable("PLTOUR_USE_DEVTUNNEL")?.Trim() == "1";

        _baseUrl = useDevTunnel ? DevTunnelUrl : DevTunnelUrl;

        // Cho phép đổi sang DevTunnel / Render bằng biến môi trường.
        // PLTOUR_API_MODE = devtunnel | render
        var apiMode = Environment.GetEnvironmentVariable("PLTOUR_API_MODE")?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(apiMode))
        {
            _baseUrl = apiMode switch
            {
                "render" => RenderUrl,
                _ => DevTunnelUrl
            };
        }

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

    public async Task<string?> GetAudioLinkAsync(string text, string langCode, int narrationId)
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
        string selectedLangCode = Preferences.Default.Get("UserLanguage", "vi");
        System.Diagnostics.Debug.WriteLine($"[API_LOG] Mapping Tour {dto.TourId}: SelectedLang={selectedLangCode}");

        var narration = dto.TourNarrations?.FirstOrDefault(n => 
                            !string.IsNullOrEmpty(n.LanguageCode) && 
                            n.LanguageCode.StartsWith(selectedLangCode, StringComparison.OrdinalIgnoreCase))
                        ?? dto.TourNarrations?.FirstOrDefault(n => n.IsDefault && selectedLangCode == "vi")
                        ?? dto.TourNarrations?.FirstOrDefault(n => n.LanguageId == 1 && selectedLangCode == "vi");

        // Nếu chọn tiếng Anh mà không thấy English Narration, cố gắng tìm bất kỳ cái nào không phải tiếng Việt hoặc cái đầu tiên
        if (selectedLangCode != "vi" && narration == null)
        {
            narration = dto.TourNarrations?.FirstOrDefault(n => n.LanguageCode != "vi" && n.LanguageId != 1)
                        ?? dto.TourNarrations?.FirstOrDefault();
        }

        var poisList = dto.Locations?.Select(MapToPoiModel).ToList() ?? new List<PoiModel>();

        var model = new TourModel
        {
            Id = dto.TourId.ToString(),
            Name = narration?.Title ?? dto.Name,
            Duration = dto.Duration,
            IntroText = narration?.Content ?? dto.IntroText,
            IntroAudioUrl = FormatAudioUrl(narration?.AudioUrl),
            ImageUrl = FormatImageUrl(dto.ImageUrl),
            Pois = poisList,
            Latitude = poisList.Any() ? poisList.First().Lat : 0,
            Longitude = poisList.Any() ? poisList.First().Lng : 0
        };

        System.Diagnostics.Debug.WriteLine($"[API_LOG] Tour {dto.TourId} IntroText Length: {model.IntroText?.Length ?? 0}, HasAudio: {!string.IsNullOrEmpty(model.IntroAudioUrl)}");
        return model;
    }

    public string BaseUrlForDebug => _baseUrl;

    public async Task<List<ProductDto>> GetProductsForPoiAsync(PoiModel poi)
    {
        try
        {
            var byLocation = await _httpClient.GetFromJsonAsync<List<ProductDto>>($"api/products/by-location/{poi.Id}");
            var results = byLocation?.Where(p => p.IsAvailable).ToList() ?? new List<ProductDto>();
            System.Diagnostics.Debug.WriteLine($"[API_MENU] GET api/products/by-location/{poi.Id} => {results.Count}");
            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetProductsForPoi: {ex}");
            return new List<ProductDto>();
        }
    }

    private PoiModel MapToPoiModel(PLTour.Shared.Models.DTO.LocationDto loc)
    {
        string selectedLangCode = LocalizationService.Instance.CurrentLanguageCode;

        var narration = loc.Narrations?.FirstOrDefault(n =>
                            !string.IsNullOrEmpty(n.LanguageCode) &&
                            n.LanguageCode.StartsWith(selectedLangCode, StringComparison.OrdinalIgnoreCase));

        if (selectedLangCode != "vi" && narration == null)
        {
            narration = loc.Narrations?.FirstOrDefault(n => n.LanguageCode != "vi" && n.LanguageId != 1)
                        ?? loc.Narrations?.FirstOrDefault();
        }
        else if (narration == null)
        {
            narration = loc.Narrations?.FirstOrDefault(n => n.LanguageId == 1)
                        ?? loc.Narrations?.FirstOrDefault();
        }

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
            AudioUrl = FormatAudioUrl(narration?.AudioUrl),
            FullContent = narration?.Content ?? string.Empty,
            LanguageName = narration?.LanguageName ?? "Tiếng Việt",
            LanguageId = narration?.LanguageId ?? 1,
            LanguageCode = narration?.LanguageCode ?? "vi",
            Name = loc.Name,
            Lat = loc.Latitude,
            Lng = loc.Longitude,
            Radius = loc.Radius > 0 ? loc.Radius : 150,
            Description = loc.Description ?? string.Empty,
            Address = loc.Address ?? string.Empty,
            CategoryId = loc.CategoryId,
            Category = MapCategoryName(loc.CategoryId),
            VendorId = loc.CategoryId == 2 ? loc.LocationId : 0,
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

    private string? FormatAudioUrl(string? rawUrl)
    {
        if (string.IsNullOrEmpty(rawUrl))
            return null;

        if (rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return rawUrl;

        return $"{_baseUrl.TrimEnd('/')}/{rawUrl.TrimStart('/')}";
    }

    private string MapCategoryName(int categoryId) => categoryId switch
    {
        1 => LocalizationService.Instance["CategoryTourism"],
        2 => LocalizationService.Instance["CategoryFood"],
        3 => LocalizationService.Instance["CategoryEvent"],
        _ => LocalizationService.Instance["CategoryTourism"]
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
    public string Url { get; set; } = string.Empty;
}