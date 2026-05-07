namespace PLTour.App.Services;

public interface IAudioService
{
    Task PlayAudioAsync(string url);
    Task StopAsync();
    bool IsPlaying { get; }
    event EventHandler? PlaybackStarted;
    event EventHandler? PlaybackStopped;
}
