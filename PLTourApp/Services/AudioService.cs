//using Android.Media;
using Microsoft.Maui.Storage;
using Plugin.Maui.Audio;

namespace PLTourApp.Services;

public class AudioService
{
    IAudioPlayer? player;
    IAudioManager manager;

    public bool IsPlaying => player != null && player.IsPlaying;

    public AudioService()
    {
        manager = AudioManager.Current;
    }

    public async Task Play(string file)
    {
        try
        {
            var stream = await FileSystem.OpenAppPackageFileAsync(file);

            player = manager.CreatePlayer(stream);

            player.Play();
        }
        catch
        {
            // tránh crash nếu file audio lỗi
        }
    }

    public void Stop()
    {
        if (player != null && player.IsPlaying)
        {
            player.Stop();
        }
    }
}