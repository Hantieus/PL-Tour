using PLTourApp.Models;

namespace PLTourApp.Engines;

public class HeatmapEngine
{
    public List<(double lat, double lng, int count)> GenerateHeatmap(List<UserRoute> routes)
    {
        var groups = routes
            .GroupBy(x => new
            {
                Lat = Math.Round(x.Latitude, 3),
                Lng = Math.Round(x.Longitude, 3)
            });

        var heatmap = new List<(double, double, int)>();

        foreach (var g in groups)
        {
            heatmap.Add((g.Key.Lat, g.Key.Lng, g.Count()));
        }

        return heatmap;
    }
}