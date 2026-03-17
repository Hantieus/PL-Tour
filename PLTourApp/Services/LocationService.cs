using Microsoft.Maui.Devices.Sensors;

namespace PLTourApp.Services;

public class LocationService
{
    public event Action<Location>? LocationChanged;

    bool isRunning = false;

    public async Task Start()
    {
        if (isRunning)
            return;

        isRunning = true;

        while (isRunning)
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.Best,
                    TimeSpan.FromSeconds(5)
                );

                var location = await Geolocation.Default.GetLocationAsync(request);

                if (location != null)
                {
                    LocationChanged?.Invoke(location);
                }
            }
            catch
            {
                // ignore lỗi GPS
            }

            await Task.Delay(5000);
        }
    }

    public void Stop()
    {
        isRunning = false;
    }
}