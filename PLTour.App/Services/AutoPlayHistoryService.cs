using Microsoft.Maui.Storage;
using System.Text.Json;

namespace PLTour.App.Services;

public sealed class AutoPlayHistoryService
{
    public static AutoPlayHistoryService Instance { get; } = new();

    private const string StorageKey = "PLTour.AutoPlayHistory";
    private readonly List<AutoPlayHistoryItem> _items = new();

    public IReadOnlyList<AutoPlayHistoryItem> Items => _items;

    public async Task LoadAsync()
    {
        try
        {
            var json = Preferences.Default.Get(StorageKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
                return;

            var loaded = JsonSerializer.Deserialize<List<AutoPlayHistoryItem>>(json);
            if (loaded != null)
            {
                _items.Clear();
                _items.AddRange(loaded);
            }
        }
        catch { }

        await Task.CompletedTask;
    }

    public async Task AddAsync(AutoPlayHistoryItem item)
    {
        _items.Insert(0, item);
        if (_items.Count > 100)
            _items.RemoveAt(_items.Count - 1);

        Preferences.Default.Set(StorageKey, JsonSerializer.Serialize(_items));
        await Task.CompletedTask;
    }
}

public sealed record AutoPlayHistoryItem(int PoiId, string PoiName, DateTime PlayedAtUtc, double DistanceMeters, double RadiusMeters);
