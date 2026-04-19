using SQLite;
using PLTour.App.Models;

namespace PLTour.App.Services;

public class DatabaseService
{
    private SQLiteAsyncConnection _db;

    private async Task InitAsync()
    {
        if (_db != null) return;
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "PoiHistory.db3");
        _db = new SQLiteAsyncConnection(dbPath);
        await _db.CreateTableAsync<PoiHistory>();
    }

    public async Task SavePoiHistoryAsync(string name, double lat, double lng)
    {
        await InitAsync();
        var history = new PoiHistory
        {
            Name = name,
            Lat = lat,
            Lng = lng,
            Time = DateTime.UtcNow
        };
        await _db.InsertAsync(history);
    }
}