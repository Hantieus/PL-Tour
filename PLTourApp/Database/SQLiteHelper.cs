using SQLite;
using PLTourApp.Models;
using Microsoft.Maui.Storage;

namespace PLTourApp.Database;

public class SQLiteHelper
{
    SQLiteAsyncConnection? db;

    public async Task Init()
    {
        if (db != null)
            return;

        var path = Path.Combine(FileSystem.AppDataDirectory, "pltour.db");

        db = new SQLiteAsyncConnection(path);

        await db.CreateTableAsync<Tour>();
        await db.CreateTableAsync<Poi>();
        await db.CreateTableAsync<Audio>();
        await db.CreateTableAsync<PlayLog>();
        await db.CreateTableAsync<UserRoute>();
    }

    // =========================
    // POI
    // =========================

    public async Task<List<Poi>> GetPois()
    {
        await Init();
        return await db!.Table<Poi>().ToListAsync();
    }

    public async Task<Poi?> GetPoi(int id)
    {
        await Init();
        return await db!.Table<Poi>()
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<int> InsertPoi(Poi poi)
    {
        await Init();
        return await db!.InsertAsync(poi);
    }

    public async Task<int> UpdatePoi(Poi poi)
    {
        await Init();
        return await db!.UpdateAsync(poi);
    }

    public async Task<int> DeletePoi(Poi poi)
    {
        await Init();
        return await db!.DeleteAsync(poi);
    }

    // =========================
    // PLAY LOG
    // =========================

    public async Task<int> InsertLog(PlayLog log)
    {
        await Init();
        return await db!.InsertAsync(log);
    }

    public async Task<bool> WasPlayedRecently(int poiId)
    {
        await Init();

        var log = await db!.Table<PlayLog>()
            .Where(x => x.PoiId == poiId)
            .OrderByDescending(x => x.PlayedAt)
            .FirstOrDefaultAsync();

        if (log == null)
            return false;

        return (DateTime.Now - log.PlayedAt).TotalMinutes < 10;
    }

    // =========================
    // ROUTE
    // =========================

    public async Task<int> InsertRoute(UserRoute route)
    {
        await Init();
        return await db!.InsertAsync(route);
    }

    public async Task<List<UserRoute>> GetRoutes()
    {
        await Init();
        return await db!.Table<UserRoute>().ToListAsync();
    }

    // =========================
    // ANALYTICS
    // =========================

    public async Task<List<(int poiId, int count)>> GetTopPois()
    {
        await Init();

        var logs = await db!.Table<PlayLog>().ToListAsync();

        return logs
            .GroupBy(x => x.PoiId)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    public async Task<List<PlayLog>> GetPlayLogs()
    {
        await Init();
        return await db!.Table<PlayLog>().ToListAsync();
    }
}