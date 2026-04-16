using Microsoft.AspNetCore.Mvc;

[Route("api/[controller]")]
[ApiController]
public class AudioController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public AudioController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("generate")]
    public async Task<IActionResult> GenerateAudio([FromQuery] string text, [FromQuery] string langCode, [FromQuery] int narrationId)
    {
        if (string.IsNullOrEmpty(text)) return BadRequest("Nội dung rỗng");

        // 1. Tạo thư mục lưu file tạm trên server
        string audioFolder = Path.Combine(_env.WebRootPath, "audio");
        if (!Directory.Exists(audioFolder)) Directory.CreateDirectory(audioFolder);

        // 2. Kiểm tra xem file này đã từng được tạo chưa (dựa vào ID) để tránh gọi lại VoiceRSS
        string fileName = $"audio_narration_{narrationId}_{langCode}.mp3";
        string filePath = Path.Combine(audioFolder, fileName);
        string fileUrl = $"{Request.Scheme}://{Request.Host}/audio/{fileName}";

        if (System.IO.File.Exists(filePath))
        {
            return Ok(new { url = fileUrl }); // Đã có file thì trả link luôn
        }

        // 3. Nếu chưa có, gọi VoiceRSS API để tạo mới
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
        string requestUrl = $"http://api.voicerss.org/?key={apiKey}&hl={voiceLang}&src={encodedText}&c=MP3&f=16khz_16bit_mono";

        using var httpClient = new HttpClient();
        byte[] audioBytes = await httpClient.GetByteArrayAsync(requestUrl);

        // 4. Lưu file vào server
        await System.IO.File.WriteAllBytesAsync(filePath, audioBytes);

        return Ok(new { url = fileUrl });
    }
}