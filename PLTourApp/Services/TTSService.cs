namespace PLTourApp.Services;

public class TTSService
{
    public async Task Speak(string text, string language)
    {
        var locales = await TextToSpeech.GetLocalesAsync();

        var locale = locales.FirstOrDefault(x =>
            x.Language.StartsWith(language));

        var options = new SpeechOptions()
        {
            Locale = locale,
            Pitch = 1.0f,
            Volume = 1.0f
        };

        await TextToSpeech.SpeakAsync(text, options);
    }
}