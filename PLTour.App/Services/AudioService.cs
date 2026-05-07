using Microsoft.Maui.ApplicationModel;
using Plugin.Maui.Audio;

namespace PLTour.App.Services;

public sealed class AudioService : IAudioService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private static readonly HttpClient _httpClient = new();
    private CancellationTokenSource? _playCts;
    private IAudioPlayer? _player;
    private MemoryStream? _playerStream;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    public AudioService() { }

    public async Task PlayAudioAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        await _gate.WaitAsync();
        try
        {
            await StopInternalAsync().ConfigureAwait(false);
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            var audioBytes = await _httpClient.GetByteArrayAsync(url, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            _playerStream = new MemoryStream(audioBytes);
            _player = AudioManager.Current.CreatePlayer(_playerStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();

            _isPlaying = true;
            PlaybackStarted?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            await StopInternalAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Không thể phát audio: {ex.Message}", ex);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task PlayTextToSpeechAsync(string text, string languageCode)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        await _gate.WaitAsync();
        try
        {
            await StopInternalAsync().ConfigureAwait(false);
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            _isPlaying = true;
            PlaybackStarted?.Invoke(this, EventArgs.Empty);

            // Chạy TTS trong một Task độc lập để không giữ _gate trong suốt thời gian đọc dài
            _ = Task.Run(async () =>
            {
                try
                {
                    var locales = await TextToSpeech.Default.GetLocalesAsync();
                    var targetLocale = locales.FirstOrDefault(l =>
                        l.Language.StartsWith(languageCode, StringComparison.OrdinalIgnoreCase));

                    // Nếu không tìm thấy locale, pass null để dùng mặc định thay vì tạo object rỗng gây lỗi
                    var speechOptions = targetLocale != null ? new SpeechOptions { Locale = targetLocale } : null;

                    await TextToSpeech.Default.SpeakAsync(text, speechOptions, token);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] TTS error: {ex.Message}");
                }
                finally
                {
                    if (!token.IsCancellationRequested)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _isPlaying = false;
                            PlaybackStopped?.Invoke(this, EventArgs.Empty);
                        });
                    }
                }
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _gate.WaitAsync();
        try
        {
            await StopInternalAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private Task StopInternalAsync()
    {
        var wasPlaying = _isPlaying;

        if (_playCts != null)
        {
            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }

        if (_player != null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            try { _player.Stop(); } catch { }
            _player.Dispose();
            _player = null;
        }

        _playerStream?.Dispose();
        _playerStream = null;

        _isPlaying = false;
        if (wasPlaying)
        {
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        }

        return Task.CompletedTask;
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isPlaying = false;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Dispose()
    {
        _playCts?.Cancel();
        _playCts?.Dispose();
        _player?.Dispose();
        _player = null;
        _playerStream?.Dispose();
        _playerStream = null;
        _gate.Dispose();
    }
}
