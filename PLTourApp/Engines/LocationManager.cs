using Microsoft.Maui.Devices.Sensors;
using PLTourApp.Models;
using PLTourApp.Database;
using PLTourApp.Services;

namespace PLTourApp.Engines;

public class LocationManager
{
    LocationService locationService;
    GeofenceEngine geofence;
    SQLiteHelper db;

    List<Poi> pois = new();

    DateTime lastTrigger = DateTime.MinValue;

    public event Action<Poi, Location>? PoiTriggered;

    public LocationManager(
        LocationService service,
        GeofenceEngine geofenceEngine,
        SQLiteHelper database
    )
    {
        locationService = service;
        geofence = geofenceEngine;
        db = database;

        locationService.LocationChanged += OnLocationChanged;
    }

    public async Task Init()
    {
        pois = await db.GetPois();
    }

    async void OnLocationChanged(Location location)
    {
        await db.InsertRoute(new UserRoute
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            TimeStamp = DateTime.Now
        });

        var poi = geofence.FindCandidate(location, pois);

        if (poi == null)
            return;

        if ((DateTime.Now - lastTrigger).TotalSeconds < 20)
            return;

        lastTrigger = DateTime.Now;

        PoiTriggered?.Invoke(poi, location);
    }
}