using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AudioController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AudioController> _logger;
    private readonly HttpClient _httpClient;

    public AudioController(IWebHostEnvironment env, ILogger<AudioController> logger, IHttpClientFactory httpClientFactory)
    {
        _env = env;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateAudio([FromQuery] string text, [FromQuery] string langCode, [FromQuery] int narrationId)
    {
        if (string.IsNullOrWhiteSpace(text)) return BadRequest("Nội dung rỗng");
        if (string.IsNullOrWhiteSpace(_env.WebRootPath)) return StatusCode(500, "WebRootPath chưa được cấu hình");

        string audioFolder = Path.Combine(_env.WebRootPath, "audio");
        if (!Directory.Exists(audioFolder))
        {
            try
            {
                Directory.CreateDirectory(audioFolder);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError(ex, "Không có quyền ghi thư mục audio: {AudioFolder}", audioFolder);
                return StatusCode(500, "Không có quyền ghi thư mục audio");
            }
        }

        string fileName = $"audio_narration_{narrationId}_{langCode}.mp3";
        string filePath = Path.Combine(audioFolder, fileName);
        string fileUrl = $"{Request.Scheme}://{Request.Host}/audio/{fileName}";

        if (System.IO.File.Exists(filePath))
        {
            return Ok(new { url = fileUrl });
        }

        string voiceLang = langCode switch
        {
            "vi" => "vi-vn",
            "ko" => "ko-kr",
            "ja" => "ja-jp",
            "zh" => "zh-cn",
            _ => "en-us"
        };

        string apiKey = "7d62828e8c0d49e58a472b35ffa64f17";
        string encodedText = Uri.EscapeDataString(text);
        string requestUrl = $"https://api.voicerss.org/?key={apiKey}&hl={voiceLang}&src={encodedText}&c=MP3&f=16khz_16bit_mono";

        byte[] audioBytes = await _httpClient.GetByteArrayAsync(requestUrl);
        await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);

        return Ok(new { url = fileUrl });
    }
}
