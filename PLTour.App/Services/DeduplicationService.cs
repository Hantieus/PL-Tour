namespace PLTour.App.Services;

public sealed class DeduplicationService
{
    private readonly TimeSpan _defaultWindow = TimeSpan.FromSeconds(10);
    private readonly Dictionary<string, DateTime> _lastSeen = new();
    private readonly object _gate = new();

    public bool ShouldProcess(string key, TimeSpan? window = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return true;

        var now = DateTime.UtcNow;
        var effectiveWindow = window ?? _defaultWindow;

        lock (_gate)
        {
            if (_lastSeen.TryGetValue(key, out var lastSeen) && now - lastSeen < effectiveWindow)
                return false;

            _lastSeen[key] = now;
            return true;
        }
    }

    public void Clear(string? key = null)
    {
        lock (_gate)
        {
            if (key != null)
            {
                _lastSeen.Remove(key);
                return;
            }

            _lastSeen.Clear();
        }
    }

    public string BuildKey(params object?[] parts)
        => string.Join('|', parts.Select(p => p?.ToString()?.Trim() ?? string.Empty));
}
