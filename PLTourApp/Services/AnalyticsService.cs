using PLTourApp.Database;
using PLTourApp.Models;

namespace PLTourApp.Services;

public class AnalyticsService
{
    SQLiteHelper db;

    public AnalyticsService(SQLiteHelper database)
    {
        db = database;
    }

    public async Task<List<(int poiId, int count)>> GetTopPois()
    {
        var logs = await db.GetPlayLogs();

        return logs
            .GroupBy(x => x.PoiId)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();
    }

    public async Task<int> GetTotalListeningTime()
    {
        var logs = await db.GetPlayLogs();

        return logs.Sum(x => x.Duration);
    }

    public async Task<List<UserRoute>> GetRouteData()
    {
        return await db.GetRoutes();
    }
}