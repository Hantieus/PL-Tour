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
    private readonly CancellationTokenSource _cts = new();
    private readonly object _gate = new();

    private bool _isRunning;
    private bool _isRestored;

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
            StartWorkerIfNeeded();
            _ = RestoreAsync();
        }
    }

    public void Stop()
    {
        lock (_gate)
        {
            _isRunning = false;
            _cts.Cancel();
        }
    }

    public Task EnqueueAsync(string url, object payload, string label)
    {
        var item = new MonitorQueueItem(url, JsonSerializer.Serialize(payload), label);
        _queue.Enqueue(item);
        QueueChanged?.Invoke(this, EventArgs.Empty);
        _signal.Release();
        return PersistAsync();
    }

    private void StartWorkerIfNeeded()
    {
        if (_isRestored)
            return;

        _isRestored = true;
        _ = Task.Run(ProcessAsync);
    }

    private async Task RestoreAsync()
    {
        var items = await _store.LoadAsync();
        foreach (var item in items)
            _queue.Enqueue(item);

        if (items.Count > 0)
        {
            QueueChanged?.Invoke(this, EventArgs.Empty);
            _signal.Release(items.Count);
        }
    }

    private async Task ProcessAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                await _signal.WaitAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            while (_queue.TryDequeue(out var item))
            {
                var sent = await SendWithRetryAsync(item);
                if (!sent)
                {
                    _queue.Enqueue(item with { Attempt = item.Attempt + 1 });
                }

                QueueChanged?.Invoke(this, EventArgs.Empty);
                await PersistAsync();
            }
        }
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
