namespace PLTour.App.Services;

public sealed class QueuedActionService
{
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly Queue<Func<Task>> _queue = new();
    private bool _isProcessing;

    public event EventHandler? QueueChanged;

    public int PendingCount
    {
        get
        {
            lock (_queue)
            {
                return _queue.Count;
            }
        }
    }

    public bool IsProcessing
    {
        get
        {
            lock (_queue)
            {
                return _isProcessing;
            }
        }
    }

    public Task EnqueueAsync(Func<Task> action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        lock (_queue)
        {
            _queue.Enqueue(action);
            if (_isProcessing)
            {
                QueueChanged?.Invoke(this, EventArgs.Empty);
                return Task.CompletedTask;
            }

            _isProcessing = true;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
        return ProcessAsync();
    }

    private async Task ProcessAsync()
    {
        await _mutex.WaitAsync();
        try
        {
            while (true)
            {
                Func<Task>? next = null;
                lock (_queue)
                {
                    if (_queue.Count > 0)
                        next = _queue.Dequeue();
                    else
                    {
                        _isProcessing = false;
                        QueueChanged?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }

                try
                {
                    if (next != null)
                        await next();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[QUEUE] Action failed: {ex.Message}");
                }

                QueueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    public void Clear()
    {
        lock (_queue)
        {
            _queue.Clear();
            _isProcessing = false;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
    }
}
