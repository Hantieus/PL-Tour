using PLTour.App.Models;

namespace PLTour.App.Services;

public sealed class AutoPlayPriorityPolicy
{
    public static AutoPlayPriorityPolicy Default { get; } = new();

    private static readonly Dictionary<int, int> CategoryPriorityScore = new()
    {
        [3] = 200,
        [1] = 120,
        [2] = 80
    };

    private static readonly (double MaxRadius, int Score)[] RadiusPriorityScore =
    {
        (25, 1000),
        (50, 800),
        (double.MaxValue, 400)
    };

    private static readonly (double MaxDistance, int Score)[] DistancePriorityScore =
    {
        (10, 500),
        (30, 300),
        (60, 150),
        (double.MaxValue, 0)
    };

    public int GetScore(PoiModel poi, double currentLatitude, double currentLongitude, int indexInTour)
    {
        var score = 0;

        if (CategoryPriorityScore.TryGetValue(poi.CategoryId, out var categoryScore))
            score += categoryScore;

        var radiusTier = RadiusPriorityScore.FirstOrDefault(x => poi.Radius <= x.MaxRadius);
        score += radiusTier.Score;

        var distance = CalculateDistance(currentLatitude, currentLongitude, poi.Lat, poi.Lng);
        var distanceTier = DistancePriorityScore.FirstOrDefault(x => distance <= x.MaxDistance);
        score += distanceTier.Score;

        score += Math.Max(0, 100 - indexInTour);

        return score;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var r = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return r * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
