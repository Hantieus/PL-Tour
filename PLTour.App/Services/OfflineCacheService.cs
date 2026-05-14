using System.Text.Json;
using Microsoft.Maui.Storage;
using PLTour.App.Models;

namespace PLTour.App.Services;

public class OfflineCacheService
{
    private const string ToursCacheFileName = "tours-cache.json";
    private const string TourByIdPrefix = "tour-";
    private const string TourPoisPrefix = "tour-pois-";

    private static OfflineCacheService? _instance;
    public static OfflineCacheService Instance => _instance ??= new OfflineCacheService();

    private readonly string _cacheFolder;

    private OfflineCacheService()
    {
        _cacheFolder = Path.Combine(FileSystem.AppDataDirectory, "offline-cache");
        Directory.CreateDirectory(_cacheFolder);
    }

    public async Task SaveToursAsync(List<TourModel> tours)
    {
        await WriteAsync(ToursCacheFileName, tours);
        foreach (var tour in tours)
        {
            await SaveTourAsync(tour);
        }
    }

    public async Task<List<TourModel>> LoadToursAsync()
    {
        return await ReadAsync<List<TourModel>>(ToursCacheFileName) ?? new List<TourModel>();
    }

    public async Task SaveTourAsync(TourModel tour)
    {
        if (string.IsNullOrWhiteSpace(tour.Id)) return;

        await WriteAsync($"{TourByIdPrefix}{tour.Id}.json", tour);
        await WriteAsync($"{TourPoisPrefix}{tour.Id}.json", tour.Pois ?? new List<PoiModel>());
    }

    public async Task<TourModel?> LoadTourAsync(string tourId)
    {
        if (string.IsNullOrWhiteSpace(tourId)) return null;

        var tour = await ReadAsync<TourModel>($"{TourByIdPrefix}{tourId}.json");
        if (tour == null) return null;

        var pois = await ReadAsync<List<PoiModel>>($"{TourPoisPrefix}{tourId}.json");
        if (pois != null && pois.Count > 0)
            tour.Pois = pois;

        return tour;
    }

    public async Task SaveLocationsAsync(List<PoiModel> pois)
    {
        await WriteAsync("locations-cache.json", pois);
    }

    public async Task<List<PoiModel>> LoadLocationsAsync()
    {
        return await ReadAsync<List<PoiModel>>("locations-cache.json") ?? new List<PoiModel>();
    }

    public Task ClearAsync()
    {
        try
        {
            if (Directory.Exists(_cacheFolder))
            {
                foreach (var file in Directory.GetFiles(_cacheFolder))
                    File.Delete(file);
            }
        }
        catch
        {
        }

        return Task.CompletedTask;
    }

    private async Task WriteAsync<T>(string fileName, T data)
    {
        var path = Path.Combine(_cacheFolder, fileName);
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json);
    }

    private async Task<T?> ReadAsync<T>(string fileName)
    {
        var path = Path.Combine(_cacheFolder, fileName);
        if (!File.Exists(path)) return default;

        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<T>(json);
    }
}
