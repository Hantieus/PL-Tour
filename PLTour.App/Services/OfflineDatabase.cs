using SQLite;
using System.Text.Json;
using PLTour.Shared.Models.DTO;

namespace PLTour.App.Services;

// 1. Tạo các bảng lưu trữ chuỗi JSON của DTO
public class OfflineLocationCache
{
    [PrimaryKey]
    public int LocationId { get; set; }
    public string JsonData { get; set; } = string.Empty;
}

public class OfflineTourCache
{
    [PrimaryKey]
    public int TourId { get; set; }
    public string JsonData { get; set; } = string.Empty;
}

// 2. Lớp thao tác cơ sở dữ liệu
public class OfflineDatabase
{
    private SQLiteAsyncConnection _db;

    private async Task InitAsync()
    {
        if (_db != null) return;

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "pltour_offline.db3");
        _db = new SQLiteAsyncConnection(dbPath);

        await _db.CreateTableAsync<OfflineLocationCache>();
        await _db.CreateTableAsync<OfflineTourCache>();
    }

    // --- XỬ LÝ LƯU/ĐỌC ĐỊA ĐIỂM (POI) ---
    public async Task SaveLocationsAsync(List<LocationDto> locations)
    {
        if (locations == null || !locations.Any()) return;

        await InitAsync();
        await _db.DeleteAllAsync<OfflineLocationCache>();

        var caches = locations.Select(loc => new OfflineLocationCache
        {
            LocationId = loc.LocationId,
            JsonData = JsonSerializer.Serialize(loc)
        }).ToList();

        await _db.InsertAllAsync(caches);
    }

    public async Task<List<LocationDto>> GetLocationsOfflineAsync()
    {
        await InitAsync();
        var caches = await _db.Table<OfflineLocationCache>().ToListAsync();

        return caches.Select(c => JsonSerializer.Deserialize<LocationDto>(c.JsonData))
                     .Where(d => d != null)
                     .ToList()!;
    }

    // --- XỬ LÝ LƯU/ĐỌC TOUR ---
    public async Task SaveToursAsync(List<TourDto> tours)
    {
        if (tours == null || !tours.Any()) return;

        await InitAsync();
        await _db.DeleteAllAsync<OfflineTourCache>();

        var caches = tours.Select(tour => new OfflineTourCache
        {
            TourId = tour.TourId,
            JsonData = JsonSerializer.Serialize(tour)
        }).ToList();

        await _db.InsertAllAsync(caches);
    }

    public async Task<List<TourDto>> GetToursOfflineAsync()
    {
        await InitAsync();
        var caches = await _db.Table<OfflineTourCache>().ToListAsync();

        return caches.Select(c => JsonSerializer.Deserialize<TourDto>(c.JsonData))
                     .Where(d => d != null)
                     .ToList()!;
    }
}