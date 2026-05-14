using PLTour.Shared.Models.DTO;

namespace PLTour.App.Services;

public class AnalyticsService
{
    private static AnalyticsService? _instance;
    public static AnalyticsService Instance => _instance ??= new AnalyticsService();

    private readonly DeviceMonitorService _monitorService = DeviceMonitorService.Instance ?? new DeviceMonitorService();
    private readonly DeduplicationService _deduplicationService = new();

    public string SessionId => _monitorService.SessionId;
    public string DeviceId => _monitorService.DeviceId;

    // Lưu trạng thái nghe audio
    private DateTime? _playbackStartTime;
    private int _currentTrackedLocationId;

    private AnalyticsService()
    {
    }

    /// <summary>
    /// Gửi sự kiện lên server (bất đồng bộ, không chờ, không ảnh hưởng UI)
    /// </summary>
    private Task TrackEventAsync(string eventType, AnalyticsEventDto? data = null)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ANALYTICS] Queueing event '{eventType}'");
            return _monitorService.TrackEventAsync(eventType, data).ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[ANALYTICS] Failed to send '{eventType}': {task.Exception.GetBaseException().Message}");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ANALYTICS] TrackEvent threw for '{eventType}': {ex.Message}");
            return Task.CompletedTask;
        }
    }

    // ==================== CÁC PHƯƠNG THỨC GỌI TỪ APP ====================

    /// <summary> Gửi vị trí hiện tại (dùng cho heatmap) </summary>
    public Task TrackLocationPingAsync(double lat, double lng)
    {
        var key = _deduplicationService.BuildKey("location_ping", Math.Round(lat, 4), Math.Round(lng, 4));
        if (!_deduplicationService.ShouldProcess(key, TimeSpan.FromSeconds(20)))
            return Task.CompletedTask;

        _ = TrackEventAsync("location_ping", new AnalyticsEventDto
        {
            Latitude = lat,
            Longitude = lng,
            Timestamp = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task TrackPoiViewAsync(int locationId)
    {
        var key = _deduplicationService.BuildKey("view_location", locationId);
        if (!_deduplicationService.ShouldProcess(key, TimeSpan.FromSeconds(20)))
            return Task.CompletedTask;

        _ = TrackEventAsync("view_location", new AnalyticsEventDto
        {
            LocationId = locationId,
            Timestamp = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task TrackAudioStartAsync(int locationId, string languageCode, bool isOnSite)
    {
        var key = _deduplicationService.BuildKey("audio_start", locationId, languageCode, isOnSite);
        if (!_deduplicationService.ShouldProcess(key, TimeSpan.FromSeconds(5)))
            return Task.CompletedTask;

        _playbackStartTime = DateTime.UtcNow;
        _currentTrackedLocationId = locationId;
        string eventType = isOnSite ? "listen_onsite" : "listen_remote";
        _ = TrackEventAsync(eventType, new AnalyticsEventDto
        {
            LocationId = locationId,
            LanguageCode = languageCode,
            HasAudio = true,
            Timestamp = DateTime.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task TrackAudioStopAsync()
    {
        if (_playbackStartTime.HasValue && _currentTrackedLocationId > 0)
        {
            var elapsedSeconds = (DateTime.UtcNow - _playbackStartTime.Value).TotalSeconds;
            var seconds = Math.Max(1, (int)Math.Ceiling(elapsedSeconds));

            _ = TrackEventAsync("listen_duration", new AnalyticsEventDto
            {
                LocationId = _currentTrackedLocationId,
                Duration = seconds,
                Timestamp = DateTime.UtcNow
            });

            _playbackStartTime = null;
            _currentTrackedLocationId = 0;
        }

        return Task.CompletedTask;
    }

}