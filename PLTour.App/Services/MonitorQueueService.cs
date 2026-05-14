using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace PLTour.App.Services;

public sealed class MonitorQueueService
{
    private readonly HttpClient _httpClient;
    private readonly MonitorQueueStore _store = new();
    private readonly ConcurrentQueue<MonitorQueueItem> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private CancellationTokenSource? _cts;
    private readonly object _gate = new();

    private bool _isRunning;
    private bool _isWorkerStarted;

    private readonly ConcurrentDictionary<string, DateTime> _processedKeys = new();
    private readonly TimeSpan _defaultCooldown = TimeSpan.FromMinutes(5);

    public event EventHandler? QueueChanged;

    public int PendingCount => _queue.Count;
    public bool IsRunning => _isRunning;

    public MonitorQueueService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        })
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_isRunning)
                return;

            _isRunning = true;
            _cts ??= new CancellationTokenSource();
            StartWorkerIfNeeded();
            _ = RestoreAsync();
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }

    public Task EnqueueAsync(string url, object payload, string label, int priority = 0, string? deduplicationKey = null, TimeSpan? notBeforeDelay = null)
    {
        var item = new MonitorQueueItem(
            url,
            JsonSerializer.Serialize(payload),
            label,
            0,
            deduplicationKey,
            priority,
            notBeforeDelay.HasValue ? DateTime.UtcNow.Add(notBeforeDelay.Value) : null);

        if (!string.IsNullOrWhiteSpace(deduplicationKey))
        {
            var cooldown = item.NotBeforeUtc ?? DateTime.UtcNow.Add(_defaultCooldown);
            var existing = _processedKeys.GetOrAdd(deduplicationKey, cooldown);
            if (existing > DateTime.UtcNow)
            {
                System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] Skipped duplicate {label}. Key={deduplicationKey}, CooldownUntil={existing:o}");
                return Task.CompletedTask;
            }

            _processedKeys[deduplicationKey] = cooldown;
        }

        _queue.Enqueue(item);
        System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] Enqueued {label}. Pending={_queue.Count}, Url={url}, Priority={priority}");
        QueueChanged?.Invoke(this, EventArgs.Empty);
        _signal.Release();
        return PersistAsync();
    }

    private void StartWorkerIfNeeded()
    {
        if (_isWorkerStarted)
            return;

        _isWorkerStarted = true;
        _ = Task.Run(ProcessAsync);
    }

    private async Task RestoreAsync()
    {
        var items = await _store.LoadAsync();
        foreach (var item in items)
        {
            _queue.Enqueue(item);
            if (!string.IsNullOrWhiteSpace(item.DeduplicationKey))
                _processedKeys[item.DeduplicationKey] = item.NotBeforeUtc ?? DateTime.UtcNow.Add(_defaultCooldown);
        }

        System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] Restored {items.Count} items. Pending={_queue.Count}");

        if (items.Count > 0)
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
            _signal.Release(items.Count);
        }
    }

    private async Task ProcessAsync()
    {
        while (true)
        {
            CancellationToken token;
            lock (_gate)
            {
                if (!_isRunning || _cts == null)
                    return;

                token = _cts.Token;
            }

            try
            {
                await _signal.WaitAsync(token);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                System.Diagnostics.Debug.WriteLine("[MONITOR_QUEUE] Offline - giữ hàng đợi để gửi sau.");
                await Task.Delay(TimeSpan.FromSeconds(5), token).ContinueWith(_ => { }, TaskScheduler.Default);
                continue;
            }

            var nextItem = DequeueNextReadyItem();
            if (nextItem is null)
                continue;

            var sent = await SendWithRetryAsync(nextItem);
            if (!sent)
            {
                _queue.Enqueue(nextItem with { Attempt = nextItem.Attempt + 1, NotBeforeUtc = DateTime.UtcNow.Add(TimeSpan.FromSeconds(10)) });
                System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] Re-queued {nextItem.Label}. Attempt={nextItem.Attempt + 1}, Pending={_queue.Count}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] Sent {nextItem.Label} successfully. Pending={_queue.Count}");
            }

            QueueChanged?.Invoke(this, EventArgs.Empty);
            await PersistAsync();

            if (_queue.Count > 0)
                _signal.Release();
        }
    }

    private MonitorQueueItem? DequeueNextReadyItem()
    {
        var snapshot = _queue.ToArray();
        if (snapshot.Length == 0)
            return null;

        var now = DateTime.UtcNow;
        var ordered = snapshot
            .Select((item, index) => new { item, index })
            .OrderByDescending(x => x.item.Priority)
            .ThenBy(x => x.item.NotBeforeUtc ?? DateTime.MinValue)
            .ThenBy(x => x.index)
            .ToList();

        foreach (var candidate in ordered)
        {
            if (candidate.item.NotBeforeUtc.HasValue && candidate.item.NotBeforeUtc.Value > now)
                continue;

            if (TryRemoveItem(candidate.item))
                return candidate.item;
        }

        var nextDue = ordered
            .Where(x => x.item.NotBeforeUtc.HasValue)
            .Select(x => x.item.NotBeforeUtc!.Value)
            .DefaultIfEmpty(now.AddSeconds(5))
            .Min();

        var delay = nextDue > now ? nextDue - now : TimeSpan.FromSeconds(1);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, _cts?.Token ?? CancellationToken.None);
                _signal.Release();
            }
            catch (OperationCanceledException)
            {
            }
        });

        return null;
    }

    private bool TryRemoveItem(MonitorQueueItem item)
    {
        var removed = new List<MonitorQueueItem>();
        var found = false;

        while (_queue.TryDequeue(out var current))
        {
            if (!found && EqualityComparer<MonitorQueueItem>.Default.Equals(current, item))
            {
                found = true;
                continue;
            }

            removed.Add(current);
        }

        foreach (var remaining in removed)
            _queue.Enqueue(remaining);

        return found;
    }

    private async Task<bool> SendWithRetryAsync(MonitorQueueItem item)
    {
        var delay = TimeSpan.FromSeconds(1);
        for (var attempt = item.Attempt; attempt < item.Attempt + 3; attempt++)
        {
            try
            {
                var content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(item.Url, content);
                if (response.IsSuccessStatusCode)
                    return true;

                System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] {item.Label} attempt {attempt + 1} failed with HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] {item.Label} attempt {attempt + 1} failed: {ex.Message}");
            }

            await Task.Delay(delay);
            delay += delay;
        }

        return false;
    }

    private async Task PersistAsync()
    {
        try
        {
            await _store.SaveAsync(_queue.ToArray());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MONITOR_QUEUE] persist failed: {ex.Message}");
        }
    }
}
