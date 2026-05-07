namespace PLTour.API.Services;

public interface ITtsService
{
    Task<byte[]> GenerateAudioAsync(string text, string langCode);
}
