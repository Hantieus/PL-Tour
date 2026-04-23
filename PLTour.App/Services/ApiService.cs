using System.Net.Http.Json;
using System.Collections.ObjectModel;
using PLTour.App.Models;
using PLTour.Shared.Models.DTO;
using MapsuiColor = Mapsui.Styles.Color;

namespace PLTour.App.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly OfflineDatabase _offlineDb;

    public ApiService(OfflineDatabase offlineDb)
    {
        _offlineDb = offlineDb;

#if DEBUG
        // Sử dụng 10.0.2.2 nếu bạn dùng máy ảo Android, hoặc IP thật nếu dùng máy thật
        _baseUrl = "http://192.168.2.6:5229/";
#else
        _baseUrl = "https://pl-tour-production.up.railway.app/";
#endif

        var handler = new HttpClientHandler
        {
            // Cho phép bypass SSL trong quá trình phát triển (DEBUG)
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(15) // Tăng lên 15s để ổn định hơn
        };
    }

    // --- LẤY DANH SÁCH TOUR ---
    public async Task<List<TourModel>> GetToursAsync()
    {
        try
        {
            List<TourDto>? tourDtos = null;

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                try
                {
                    tourDtos = await _httpClient.GetFromJsonAsync<List<TourDto>>("api/tours");
                    if (tourDtos != null && tourDtos.Any())
                    {
                        // Lưu vào cache offline để dùng lần sau
                        await _offlineDb.SaveToursAsync(tourDtos);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API Error: {ex.Message}");
                }
            }

            // Nếu không có mạng hoặc API lỗi, lấy từ DB Offline
            if (tourDtos == null || !tourDtos.Any())
            {
                tourDtos = await _offlineDb.GetToursOfflineAsync();
            }

            if (tourDtos == null) return new List<TourModel>();

            return tourDtos.Select(dto => new TourModel
            {
                Id = dto.TourId.ToString(),
                Name = dto.Name ?? "Không tên",
                Duration = dto.Duration,
                IntroText = dto.IntroText ?? "",
                ImageUrl = FormatImageUrl(dto.ImageUrl),
                Pois = dto.Locations?.Select(loc => MapToPoiModel(loc)).ToList() ?? new List<PoiModel>(),
                Latitude = dto.Locations != null && dto.Locations.Any() ? dto.Locations.First().Latitude : 0,
                Longitude = dto.Locations != null && dto.Locations.Any() ? dto.Locations.First().Longitude : 0
            }).ToList();
        }
        catch
        {
            return new List<TourModel>();
        }
    }

    // --- LẤY TẤT CẢ ĐỊA ĐIỂM ---
    public async Task<List<PoiModel>> GetAllLocationsAsync()
    {
        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return new List<PoiModel>(); // Hoặc lấy từ Offline nếu bạn có hàm lưu Locations

            var locDtos = await _httpClient.GetFromJsonAsync<List<LocationDto>>("api/Locations");
            return locDtos?.Select(loc => MapToPoiModel(loc)).ToList() ?? new List<PoiModel>();
        }
        catch
        {
            return new List<PoiModel>();
        }
    }

    // --- LẤY DANH SÁCH MÓN ĂN (MENU) THEO VENDOR ---
    public async Task<ObservableCollection<ProductDto>> GetProductsByVendorAsync(int vendorId)
    {
        try
        {
            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                var products = await _httpClient.GetFromJsonAsync<List<ProductDto>>($"api/products/by-vendor/{vendorId}");
                if (products != null)
                {
                    return new ObservableCollection<ProductDto>(products);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Menu Error: {ex.Message}");
        }
        return new ObservableCollection<ProductDto>();
    }

    // --- CÁC HÀM BỔ TRỢ ---
    private PoiModel MapToPoiModel(LocationDto loc)
    {
        return new PoiModel
        {
            Id = loc.LocationId,
            Name = loc.Name ?? "Địa điểm không tên",
            Lat = loc.Latitude,
            Lng = loc.Longitude,
            Radius = loc.Radius,
            Description = loc.Description ?? "",
            Address = loc.Address ?? "",
            ImageUrl = FormatImageUrl(loc.ImageUrl),
            Category = loc.CategoryName ?? "Chưa phân loại",
            // Đảm bảo Shared Project đã update LocationDto có VendorId
            // VendorId = loc.VendorId, 
            PinColor = (loc.CategoryName == PoiCategories.AnUong) ? MapsuiColor.Red : MapsuiColor.Blue
        };
    }

    private string FormatImageUrl(string? rawUrl)
    {
        if (string.IsNullOrEmpty(rawUrl)) return "tour_thumb.jpg";
        if (rawUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return rawUrl;

        return $"{_baseUrl.TrimEnd('/')}/{rawUrl.TrimStart('/')}";
    }
}