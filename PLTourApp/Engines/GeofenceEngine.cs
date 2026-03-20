using PLTourApp.Models;

namespace PLTourApp.Engines;

public class GeofenceEngine
{
    public Poi FindCandidate(Location location, List<Poi> pois)
    {
        Poi candidate = null;

        double bestScore = double.MaxValue;

        foreach (var poi in pois)
        {
            double distance = CalculateDistance(
                location.Latitude,
                location.Longitude,
                poi.Latitude,
                poi.Longitude
            );

            if (distance <= poi.Radius)
            {
                double score = distance - (poi.Priority * 10);

                if (score < bestScore)
                {
                    bestScore = score;
                    candidate = poi;
                }
            }
        }

        return candidate;
    }

    double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371000;

        double dLat = (lat2 - lat1) * Math.PI / 180;
        double dLon = (lon2 - lon1) * Math.PI / 180;

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) *
            Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLon / 2) *
            Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}