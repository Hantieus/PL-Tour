using System.Collections.ObjectModel;
using PLTourApp.Models;
using PLTourApp.Database;
using Microsoft.Maui.Devices.Sensors;

namespace PLTourApp.ViewModels;

public class MapViewModel
{
    SQLiteHelper db;

    public ObservableCollection<Poi> Pois { get; set; } = new();

    public Poi NearestPoi { get; set; }

    public MapViewModel(SQLiteHelper database)
    {
        db = database;
    }

    public async Task LoadPois()
    {
        var list = await db.GetPois();

        Pois.Clear();

        foreach (var p in list)
        {
            Pois.Add(p);
        }
    }

    public Poi FindNearest(Location location)
    {
        Poi nearest = null;
        double min = double.MaxValue;

        foreach (var poi in Pois)
        {
            double distance = Distance(
                location.Latitude,
                location.Longitude,
                poi.Latitude,
                poi.Longitude
            );

            if (distance < min)
            {
                min = distance;
                nearest = poi;
            }
        }

        NearestPoi = nearest;

        return nearest;
    }

    double Distance(double lat1, double lon1, double lat2, double lon2)
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