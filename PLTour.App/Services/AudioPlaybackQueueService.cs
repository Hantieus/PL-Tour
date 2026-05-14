using System.Collections.Concurrent;

namespace PLTour.App.Services;

public sealed class AudioPlaybackQueueService
{
    private readonly IAudioService _audioService;
    private readonly ConcurrentQueue<PlaybackRequest> _queue = new();
    private readonly SemaphoreSlim _signal = new(0);
    private readonly SemaphoreSlim _workerGate = new(1, 1);
    private readonly object _gate = new();
    private bool _isRunning;
    private bool _isWorkerStarted;
    private bool _isProcessing;

    public event EventHandler? QueueChanged;

    public int PendingCount => _queue.Count;
    public bool IsRunning => _isRunning;
    public bool IsProcessing
    {
        get
        {
            lock (_gate)
            {
                return _isProcessing;
            }
        }
    }

    public AudioPlaybackQueueService(IAudioService audioService)
    {
        _audioService = audioService;
    }

    public void Start()
    {
        lock (_gate)
        {
            if (_isRunning)
                return;

            _isRunning = true;
            StartWorkerIfNeeded();
        }
    }

    public async Task StopAsync()
    {
        lock (_gate)
        {
            if (!_isRunning)
                return;

            _isRunning = false;
            while (_queue.TryDequeue(out _)) { }
            _isProcessing = false;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
        _signal.Release();
        await _audioService.StopAsync();
    }

    public Task EnqueueAudioAsync(string? audioUrl, string label, bool clearPending = false)
        => EnqueueAsync(new PlaybackRequest(PlaybackKind.Audio, audioUrl, null, null, label, clearPending));

    public Task EnqueueSpeechAsync(string text, string languageCode, string label, bool clearPending = false)
        => EnqueueAsync(new PlaybackRequest(PlaybackKind.Speech, null, text, languageCode, label, clearPending));

    public async Task ClearAsync(bool stopCurrent = true)
    {
        lock (_gate)
        {
            while (_queue.TryDequeue(out _)) { }
            _isProcessing = false;
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);

        if (stopCurrent)
            await _audioService.StopAsync();
    }

    private Task EnqueueAsync(PlaybackRequest request)
    {
        Start();

        lock (_gate)
        {
            if (request.ClearPending)
            {
                while (_queue.TryDequeue(out _)) { }
            }

            _queue.Enqueue(request);
        }

        QueueChanged?.Invoke(this, EventArgs.Empty);
        _signal.Release();
        return Task.CompletedTask;
    }

    private void StartWorkerIfNeeded()
    {
        if (_isWorkerStarted)
            return;

        _isWorkerStarted = true;
        _ = Task.Run(ProcessAsync);
    }

    private async Task ProcessAsync()
    {
        while (true)
        {
            await _signal.WaitAsync();

            lock (_gate)
            {
                if (!_isRunning)
                    return;
            }

            if (!_queue.TryDequeue(out var request))
                continue;

            await _workerGate.WaitAsync();
            try
            {
                lock (_gate)
                {
                    _isProcessing = true;
                }
                QueueChanged?.Invoke(this, EventArgs.Empty);

                await _audioService.StopAsync();

                switch (request.Kind)
                {
                    case PlaybackKind.Audio:
                        await _audioService.PlayAudioAsync(request.AudioUrl ?? string.Empty);
                        break;
                    case PlaybackKind.Speech:
                        await _audioService.PlayTextToSpeechAsync(request.Text ?? string.Empty, request.LanguageCode ?? "vi");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AUDIO_QUEUE] {request.Label} failed: {ex.Message}");
            }
            finally
            {
                lock (_gate)
                {
                    _isProcessing = false;
                }
                QueueChanged?.Invoke(this, EventArgs.Empty);
                _workerGate.Release();
            }
        }
    }

    private enum PlaybackKind
    {
        Audio,
        Speech
    }

    private sealed record PlaybackRequest(
        PlaybackKind Kind,
        string? AudioUrl,
        string? Text,
        string? LanguageCode,
        string Label,
        bool ClearPending);
}