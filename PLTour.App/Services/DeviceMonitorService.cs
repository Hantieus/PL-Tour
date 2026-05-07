using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;
using System.Text;
using System.Text.Json;

namespace PLTour.App.Services;

public class DeviceMonitorService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _heartbeatUrl;
    private readonly string _eventUrl;

    private bool _isStarted;
    private CancellationTokenSource? _heartbeatCts;

    public static DeviceMonitorService Instance { get; private set; } = null!;

    public string DeviceId { get; }
    public string SessionId { get; }

    public DeviceMonitorService()
    {
        Instance = this;

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

#if DEBUG
        _baseUrl = "http://192.168.2.6:5229/";
#else
        _baseUrl = "https://pl-tour-production.up.railway.app/";
#endif

        _heartbeatUrl = $"{_baseUrl.TrimEnd('/')}/api/monitor/heartbeat";
        _eventUrl = $"{_baseUrl.TrimEnd('/')}/api/monitor/event";

        DeviceId = Preferences.Default.Get("PLTour.DeviceId", string.Empty);
        if (string.IsNullOrWhiteSpace(DeviceId))
        {
            DeviceId = Guid.NewGuid().ToString("N");
            Preferences.Default.Set("PLTour.DeviceId", DeviceId);
        }

        SessionId = Guid.NewGuid().ToString("N");
    }

    public void Start()
    {
        if (_isStarted)
            return;

        _isStarted = true;
        _heartbeatCts = new CancellationTokenSource();
        _ = SendHeartbeatAsync("app_start");
        _ = RunHeartbeatLoopAsync(_heartbeatCts.Token);
    }

    public void Stop()
    {
        if (!_isStarted)
            return;

        _isStarted = false;
        _heartbeatCts?.Cancel();
        _heartbeatCts?.Dispose();
        _heartbeatCts = null;
    }

    public Task TrackEventAsync(string eventType, AnalyticsEventDto? data = null)
    {
        return SendEventAsync(eventType, data);
    }

    public Task SendHeartbeatAsync(string? reason = null)
    {
        return SendHeartbeatInternalAsync(reason);
    }

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                await SendHeartbeatInternalAsync("periodic");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MONITOR] Heartbeat loop error: {ex.Message}");
            }
        }
    }

    private async Task SendHeartbeatInternalAsync(string? reason)
    {
        var (batteryLevel, isCharging) = GetBatteryInfo();

        var payload = new
        {
            deviceId = DeviceId,
            sessionId = SessionId,
            reason,
            deviceName = DeviceInfo.Current.Name,
            deviceModel = DeviceInfo.Current.Model,
            osVersion = DeviceInfo.Current.VersionString,
            appVersion = AppInfo.VersionString,
            platform = DeviceInfo.Current.Platform.ToString(),
            batteryLevel,
            isCharging,
            timestamp = DateTime.UtcNow,
            latitude = LocationService.Shared?.CurrentLocation?.Latitude,
            longitude = LocationService.Shared?.CurrentLocation?.Longitude
        };

        await PostJsonAsync(_heartbeatUrl, payload, "heartbeat");
    }

    private static (int? batteryLevel, bool isCharging) GetBatteryInfo()
    {
        try
        {
#if ANDROID
            var intentFilter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
            var batteryStatus = Android.App.Application.Context?.RegisterReceiver(null, intentFilter);
            if (batteryStatus == null)
                return (null, false);

            var level = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraLevel, -1);
            var scale = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraScale, -1);
            var status = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraStatus, -1);

            var batteryLevel = (level >= 0 && scale > 0)
                ? (int)Math.Round((level * 100d) / scale)
                : (int?)null;

            var isCharging = status == (int)Android.OS.BatteryStatus.Charging ||
                             status == (int)Android.OS.BatteryStatus.Full;

            return (batteryLevel, isCharging);
#else
            var batteryLevel = (int)Math.Round(Battery.Default.ChargeLevel * 100);
            var isCharging = Battery.Default.State == BatteryState.Charging || Battery.Default.State == BatteryState.Full;
            return (batteryLevel, isCharging);
#endif
        }
        catch
        {
            return (null, false);
        }
    }

    private async Task SendEventAsync(string eventType, AnalyticsEventDto? data)
    {
        data ??= new AnalyticsEventDto();
        data.DeviceId ??= DeviceId;
        data.SessionId ??= SessionId;
        data.EventType = eventType;
        data.Platform ??= DeviceInfo.Current.Platform.ToString();
        if (data.Timestamp == default)
            data.Timestamp = DateTime.UtcNow;

        await PostJsonAsync(_eventUrl, data, eventType);
    }

    private async Task PostJsonAsync(string url, object payload, string label)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[MONITOR] {label} failed: {response.StatusCode} - {body}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MONITOR] {label} error: {ex.Message}");
        }
    }
}
