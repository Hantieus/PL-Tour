using System.Text.Json;

namespace PLTour.Admin.Services
{
    public interface ITranslationService
    {
        Task<Dictionary<string, string>> TranslateToAllLanguages(string sourceText, string sourceLanguage = "vi");
    }

    public class FreeTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<FreeTranslationService> _logger;

        // DeepL Free API (đăng ký tại https://www.deepl.com/pro#developer)
        private const string DEEPL_API_KEY = "sk-5aac6fc95a82917c1a31ac2d0efaaece2753b71a5adea84f4c18f1e4cb40";  // 👈 ĐĂNG KÝ KEY MIỄN PHÍ
        private const string DEEPL_API_URL = "https://api-free.deepl.com/v2/translate";

        private readonly List<(string Code, string Target)> _targetLanguages = new()
        {
            ("en", "EN-US"),
            ("zh", "ZH"),
            ("ko", "KO"),
            ("ja", "JA")
        };

        public FreeTranslationService(ILogger<FreeTranslationService> logger)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(60);
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> TranslateToAllLanguages(string sourceText, string sourceLanguage = "vi")
        {
            var translations = new Dictionary<string, string>();

            foreach (var target in _targetLanguages)
            {
                try
                {
                    _logger.LogInformation($"Đang dịch sang {target.Code}...");
                    var translated = await TranslateViaDeepL(sourceText, target.Target);
                    translations[target.Code] = translated;
                    _logger.LogInformation($"Dịch sang {target.Code} thành công");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi dịch sang {target.Code}");
                    translations[target.Code] = sourceText;
                }

                await Task.Delay(500);
            }

            return translations;
        }

        private async Task<string> TranslateViaDeepL(string text, string targetLang)
        {
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("auth_key", DEEPL_API_KEY),
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("target_lang", targetLang)
            });

            var response = await _httpClient.PostAsync(DEEPL_API_URL, content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeepLResponse>(json);

            return result?.Translations?.FirstOrDefault()?.Text ?? text;
        }

        private class DeepLResponse
        {
            public List<DeepLTranslation>? Translations { get; set; }
        }

        private class DeepLTranslation
        {
            public string? Text { get; set; }
        }
    }
}