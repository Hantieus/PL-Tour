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

        private readonly List<(string Code, string Name)> _targetLanguages = new()
        {
            ("en", "English"),
            ("zh", "中文"),
            ("ko", "한국어"),
            ("ja", "日本語")
        };

        public FreeTranslationService(ILogger<FreeTranslationService> logger)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> TranslateToAllLanguages(string sourceText, string sourceLanguage = "vi")
        {
            var translations = new Dictionary<string, string>();

            foreach (var target in _targetLanguages)
            {
                try
                {
                    _logger.LogInformation($"Đang dịch sang {target.Name}...");
                    var translated = await TranslateViaGoogle(sourceText, sourceLanguage, target.Code);
                    translations[target.Code] = translated;
                    _logger.LogInformation($"Dịch sang {target.Name} thành công");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Lỗi dịch sang {target.Name}");
                    translations[target.Code] = sourceText;
                }

                await Task.Delay(500);
            }

            return translations;
        }

        /// <summary>
        /// Dùng Google Translate FREE API (không cần key)
        /// </summary>
        private async Task<string> TranslateViaGoogle(string text, string sourceLang, string targetLang)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            var url = $"https://translate.googleapis.com/translate_a/single?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var translated = doc.RootElement[0][0][0].GetString();

            return translated ?? text;
        }
    }
}