using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace PLTour.API.Services;

public class EdgeTtsService : ITtsService
{
    private const int MaxChunkLength = 180;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EdgeTtsService> _logger;

    public EdgeTtsService(HttpClient httpClient, IConfiguration configuration, ILogger<EdgeTtsService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<byte[]> GenerateAudioAsync(string text, string langCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text không được rỗng", nameof(text));

        var normalizedText = NormalizeText(text);
        var chunks = SplitIntoChunks(normalizedText).ToList();
        var endpoint = "https://translate.google.com/translate_tts";

        _logger.LogInformation("Gọi TTS endpoint: {Endpoint}, Lang={Lang}, Chunks={ChunkCount}", endpoint, langCode, chunks.Count);

        using var output = new MemoryStream();
        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var requestUrl = $"{endpoint}?ie=UTF-8&q={Uri.EscapeDataString(chunk)}&tl={Uri.EscapeDataString(GetVoice(langCode))}&client=tw-ob";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            using var response = await _httpClient.SendAsync(request);
            _logger.LogInformation("TTS chunk {Index}/{Total} status: {StatusCode}", i + 1, chunks.Count, response.StatusCode);
            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync();
            if (bytes.Length == 0)
                throw new InvalidOperationException("TTS trả về audio rỗng");

            await output.WriteAsync(bytes);
        }

        _logger.LogInformation("TTS audio bytes length: {Length}", output.Length);
        return output.ToArray();
    }

    private static string NormalizeText(string text)
    {
        var withoutHtml = Regex.Replace(text, "<.*?>", string.Empty);
        var collapsed = Regex.Replace(withoutHtml, "\\s+", " ");
        return collapsed.Trim();
    }

    private static IEnumerable<string> SplitIntoChunks(string text)
    {
        if (text.Length <= MaxChunkLength)
        {
            yield return text;
            yield break;
        }

        var sentences = Regex.Split(text, "(?<=[\\.!?。！？])\\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        var buffer = string.Empty;
        foreach (var sentence in sentences)
        {
            var next = string.IsNullOrWhiteSpace(buffer) ? sentence : $"{buffer} {sentence}";
            if (next.Length <= MaxChunkLength)
            {
                buffer = next;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(buffer))
            {
                yield return buffer;
                buffer = string.Empty;
            }

            if (sentence.Length <= MaxChunkLength)
            {
                buffer = sentence;
                continue;
            }

            for (var i = 0; i < sentence.Length; i += MaxChunkLength)
                yield return sentence.Substring(i, Math.Min(MaxChunkLength, sentence.Length - i));
        }

        if (!string.IsNullOrWhiteSpace(buffer))
            yield return buffer;
    }

    private static string GetVoice(string langCode)
    {
        return langCode?.ToLowerInvariant() switch
        {
            "vi" => "vi",
            "en" => "en",
            "ja" => "ja",
            "ko" => "ko",
            "zh" => "zh",
            _ => "en"
        };
    }
}
