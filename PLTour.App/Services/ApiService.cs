using System.Net.Http.Json;
using PLTour.App.Models;
using PLTour.Shared.Models.DTO;
using Mapsui.Styles;
using MapsuiColor = Mapsui.Styles.Color;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Devices;

namespace PLTour.App.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService()
    {
        string httpPort = "5229";
        string apiUrl = "";

        // CHIẾN THUẬT: SỬ DỤNG LOCALHOST CHO TẤT CẢ (KẾT HỢP ADB REVERSE)
        if (DeviceInfo.Platform == DevicePlatform.Android)
        {
            // Nếu là máy ảo: Dùng 10.0.2.2 (vốn trỏ về localhost của máy tính)
            if (DeviceInfo.DeviceType == DeviceType.Virtual)
            {
                apiUrl = $"http://10.0.2.2:{httpPort}/";
            }
            // Nếu là điện thoại thật: Dùng localhost (kết hợp với lệnh adb reverse tcp:5229 tcp:5229)
            else
            {
                apiUrl = $"http://localhost:{httpPort}/";
            }
        }
        else
        {
            // Chạy trực tiếp trên Windows
            apiUrl = $"http://localhost:{httpPort}/";
        }

        System.Diagnostics.Debug.WriteLine($"[API_LOG] App đang kết nối tới: {apiUrl}");

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(apiUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public async Task<List<TourModel>> GetToursAsync()
    {
        try
        {
            var tourDtos = await _httpClient.GetFromJsonAsync<List<TourDto>>("api/tours");
            if (tourDtos == null || !tourDtos.Any()) return new List<TourModel>();

            var tours = new List<TourModel>();
            foreach (var dto in tourDtos)
            {
                var tour = new TourModel
                {
                    Id = dto.TourId.ToString(),
                    Name = dto.Name,
                    Duration = dto.Duration,
                    IntroText = dto.IntroText,
                    ImageUrl = dto.ImageUrl ?? "tour_thumb.jpg",
                    Pois = dto.Locations.Select(loc => MapToPoiModel(loc)).ToList()
                };

                if (tour.Pois.Any())
                {
                    tour.Latitude = tour.Pois.First().Lat;
                    tour.Longitude = tour.Pois.First().Lng;
                }
                tours.Add(tour);
            }
            return tours;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetTours: {ex.Message}");
            return new List<TourModel>();
        }
    }

    public async Task<List<PoiModel>> GetAllLocationsAsync()
    {
        try
        {
            // Gọi endpoint lấy toàn bộ POI
            var locDtos = await _httpClient.GetFromJsonAsync<List<PLTour.Shared.Models.DTO.LocationDto>>("api/Locations");

            if (locDtos == null || !locDtos.Any())
                return new List<PoiModel>();

            return locDtos.Select(loc => MapToPoiModel(loc)).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[API_ERROR] GetAllLocations: {ex.Message}");
            return new List<PoiModel>();
        }
    }

    private PoiModel MapToPoiModel(PLTour.Shared.Models.DTO.LocationDto loc)
    {
        string selectedLangCode = Preferences.Default.Get("UserLanguage", "vi");
        var narration = loc.Narrations?.FirstOrDefault(n => n.LanguageCode == selectedLangCode)
                        ?? loc.Narrations?.FirstOrDefault(n => n.IsDefault)
                        ?? loc.Narrations?.FirstOrDefault();

        return new PoiModel
        {
            Id = loc.LocationId,
            ImageUrl = string.IsNullOrEmpty(loc.ImageUrl) ? "tour_thumb.jpg" : loc.ImageUrl,
            AudioUrl = narration?.AudioUrl,
            FullContent = narration?.Content,
            LanguageName = narration?.LanguageName ?? "Chưa xác định",
            Name = loc.Name,
            Lat = loc.Latitude,
            Lng = loc.Longitude,
            Radius = loc.Radius > 0 ? loc.Radius : 150,
            Description = loc.Description ?? "",
            Address = loc.Address ?? "",
            Category = MapCategoryName(loc.CategoryId),
            PinColor = GetPinColor(loc.CategoryId)
        };
    }

    private string MapCategoryName(int categoryId) => categoryId switch
    {
        1 => PoiCategories.ThamQuan,
        2 => PoiCategories.AnUong,
        3 => PoiCategories.SuKien,
        _ => PoiCategories.ThamQuan
    };

    private MapsuiColor GetPinColor(int categoryId) => categoryId switch
    {
        1 => MapsuiColor.Red,
        2 => MapsuiColor.Orange,
        3 => MapsuiColor.Purple,
        _ => MapsuiColor.Blue
    };
}