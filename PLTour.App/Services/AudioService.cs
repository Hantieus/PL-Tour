using CommunityToolkit.Maui.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace PLTour.App.Services;

public sealed class AudioService : IAudioService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _playCts;
    private MediaElement? _mediaPlayer;
    private bool _isPlaying;

    public bool IsPlaying => _isPlaying;

    public event EventHandler? PlaybackStarted;
    public event EventHandler? PlaybackStopped;

    public AudioService()
    {
        MainThread.BeginInvokeOnMainThread(EnsurePlayer);
    }

    public async Task PlayAudioAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        await _gate.WaitAsync();
        try
        {
            await StopInternalAsync().ConfigureAwait(false);
            _playCts = new CancellationTokenSource();
            var token = _playCts.Token;

            await Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    EnsurePlayer();
                    _mediaPlayer!.Source = MediaSource.FromUri(url);
                    _mediaPlayer.Play();
                    _isPlaying = true;
                    PlaybackStarted?.Invoke(this, EventArgs.Empty);
                });
            }, token).ConfigureAwait(false);
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

    private void EnsurePlayer()
    {
        if (_mediaPlayer != null) return;

        _mediaPlayer = new MediaElement { IsVisible = false };
        _mediaPlayer.MediaEnded += OnMediaEnded;
    }

    private async Task StopInternalAsync()
    {
        if (_playCts != null)
        {
            _playCts.Cancel();
            _playCts.Dispose();
            _playCts = null;
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (_mediaPlayer != null)
            {
                try { _mediaPlayer.Stop(); } catch { }
                _mediaPlayer.Source = null;
                _isPlaying = false;
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        _isPlaying = false;
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        _playCts?.Cancel();
        _playCts?.Dispose();
        if (_mediaPlayer != null)
        {
            _mediaPlayer.MediaEnded -= OnMediaEnded;
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
        _gate.Dispose();
    }
}
