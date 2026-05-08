using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using PLTour.Shared.Models.DTO;

namespace PLTour.App.Services;

public class DeviceMonitorService
{
    private readonly string _baseUrl;
    private readonly string _heartbeatUrl;
    private readonly string _eventUrl;
    private readonly MonitorQueueService _queueService;

    private bool _isStarted;
    private CancellationTokenSource? _heartbeatCts;

    public static DeviceMonitorService Instance { get; private set; } = null!;

    public string DeviceId { get; }
    public string SessionId { get; }

    public event EventHandler? QueueChanged;

    public int PendingCount => _queueService.PendingCount;
    public bool QueueIsRunning => _queueService.IsRunning;

    public DeviceMonitorService()
    {
        Instance = this;

        const string DevTunnelUrl = "https://q0x087zj-7291.asse.devtunnels.ms/";
        const string LanUrl = "http://192.168.100.123:5229/";
        const string RenderUrl = "https://pl-tour-production.up.railway.app/";

#if DEBUG
        // --- CẤU HÌNH KHI CHẠY DEBUG TẠI LOCAL ---
        // Mặc định dùng Dev Tunnel
        _baseUrl = DevTunnelUrl;
#else
        // --- CẤU HÌNH KHI PUBLISH / CHẤM ĐỒ ÁN (SERVER THẬT) ---
        // Mặc định dùng Render
        _baseUrl = RenderUrl;
#endif

        // Cho phép đổi sang LAN hoặc Render bằng biến môi trường
        // PLTOUR_MONITOR_MODE = devtunnel | lan | render
        var monitorMode = Environment.GetEnvironmentVariable("PLTOUR_MONITOR_MODE")?.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(monitorMode))
        {
            _baseUrl = monitorMode switch
            {
                "lan" => LanUrl,
                "render" => RenderUrl,
                _ => DevTunnelUrl
            };
        }

        System.Diagnostics.Debug.WriteLine($"[MONITOR_LOG] App đang kết nối tới: {_baseUrl}");

        var handler = new HttpClientHandler
        {
            // Bỏ qua lỗi chứng chỉ SSL khi chạy HTTP ở local
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        _heartbeatUrl = $"{_baseUrl.TrimEnd('/')}/api/monitor/heartbeat";
        _eventUrl = $"{_baseUrl.TrimEnd('/')}/api/monitor/event";
        _queueService = new MonitorQueueService();
        _queueService.QueueChanged += (_, __) => QueueChanged?.Invoke(this, EventArgs.Empty);

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
        _queueService.Start();
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
        _queueService.Stop();
    }

    public Task TrackEventAsync(string eventType, AnalyticsEventDto? data = null)
        => EnqueueEventAsync(eventType, data);

    public Task SendHeartbeatAsync(string? reason = null)
        => EnqueueHeartbeatAsync(reason);

    private async Task RunHeartbeatLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                await EnqueueHeartbeatAsync("periodic");
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

    private Task EnqueueHeartbeatAsync(string? reason)
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

        return _queueService.EnqueueAsync(_heartbeatUrl, payload, "heartbeat");
    }

    private Task EnqueueEventAsync(string eventType, AnalyticsEventDto? data)
    {
        data ??= new AnalyticsEventDto();
        data.DeviceId ??= DeviceId;
        data.SessionId ??= SessionId;
        data.EventType = eventType;
        data.Platform ??= DeviceInfo.Current.Platform.ToString();
        if (data.Timestamp == default)
            data.Timestamp = DateTime.UtcNow;

        return _queueService.EnqueueAsync(_eventUrl, data, eventType);
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
}
