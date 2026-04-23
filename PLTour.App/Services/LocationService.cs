using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace PLTour.App.Services
{
    public class LocationService
    {
        private double _lastSpeed = 0;
        private DateTime _lastAdaptiveUpdate = DateTime.MinValue;

        public Location CurrentLocation
        {
            get => GetSavedLocation();
            set
            {
                if (value != null)
                {
                    Preferences.Default.Set("UserLat", value.Latitude);
                    Preferences.Default.Set("UserLng", value.Longitude);

                    if (value.Speed.HasValue)
                        _lastSpeed = value.Speed.Value;
                }
            }
        }

        public async Task<Location> GetAdaptiveLocationAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                    if (status != PermissionStatus.Granted) return null;
                }

                var accuracy = _lastSpeed > 1.0 ? GeolocationAccuracy.High : GeolocationAccuracy.Medium;
                var request = new GeolocationRequest(accuracy, TimeSpan.FromSeconds(5));
                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    CurrentLocation = location;
                    _lastAdaptiveUpdate = DateTime.UtcNow;
                }
                return location;
            }
            catch
            {
                return null;
            }
        }

        public int GetAdaptiveInterval()
        {
            if (_lastSpeed < 0.5) return 30000;
            if (_lastSpeed > 10.0) return 5000;
            return 15000;
        }

        public bool ShouldSendHeartbeat(int maxIdleMinutes = 5)
        {
            return (DateTime.UtcNow - _lastAdaptiveUpdate).TotalMinutes >= maxIdleMinutes;
        }

        public async Task<Location> GetAndSaveCurrentLocationAsync()
        {
            return await GetAdaptiveLocationAsync();
        }

        public Location GetSavedLocation()
        {
            if (Preferences.Default.ContainsKey("UserLat") && Preferences.Default.ContainsKey("UserLng"))
            {
                var lat = Preferences.Default.Get("UserLat", 0.0);
                var lng = Preferences.Default.Get("UserLng", 0.0);
                return new Location(lat, lng);
            }
            return null;
        }
    }
}