using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using PLTour.API.Models.DbContext;
using PLTour.API.Services;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AudioController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AudioController> _logger;
    private readonly PLTourDbContext _context;
    private readonly ITtsService _ttsService;
    private readonly ICloudinaryService _cloudinaryService;

    public AudioController(
        IWebHostEnvironment env,
        ILogger<AudioController> logger,
        PLTourDbContext context,
        ITtsService ttsService,
        ICloudinaryService cloudinaryService)
    {
        _env = env;
        _logger = logger;
        _context = context;
        _ttsService = ttsService;
        _cloudinaryService = cloudinaryService;
    }

    [HttpGet("generate-narration")]
    public async Task<IActionResult> GenerateNarrationAudio([FromQuery] int narrationId, [FromQuery] string langCode)
    {
        var narration = await _context.Narrations.FindAsync(narrationId);
        if (narration == null) return NotFound("Không tìm thấy narration");

        var text = narration.Content;
        if (string.IsNullOrWhiteSpace(text)) return BadRequest("Narration chưa có nội dung");

        var textHash = GetShortHash(text);
        var fileName = $"narration_{narrationId}_{langCode}_{textHash}.mp3";
        var folder = $"pltour/audio/narrations/{narrationId}";

        if (!string.IsNullOrWhiteSpace(narration.AudioUrl) && narration.AudioUrl.Contains(textHash, StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new { url = narration.AudioUrl, cached = true });
        }

        byte[] audioBytes;
        try
        {
            audioBytes = await _ttsService.GenerateAudioAsync(text, langCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo audio narration {NarrationId}", narrationId);
            return StatusCode(500, "Không tạo được audio");
        }

        await using var stream = new MemoryStream(audioBytes);
        var url = await _cloudinaryService.UploadAudioAsync(stream, fileName, folder);

        narration.AudioUrl = url;
        narration.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { url, cached = false });
    }

    [HttpGet("generate-tour")]
    public async Task<IActionResult> GenerateTourAudio([FromQuery] int tourId, [FromQuery] int languageId)
    {
        var tour = await _context.Tours.FindAsync(tourId);
        if (tour == null) return NotFound("Không tìm thấy tour");

        var language = await _context.Languages.FindAsync(languageId);
        if (language == null) return NotFound("Không tìm thấy ngôn ngữ");

        var text = tour.IntroText;
        if (string.IsNullOrWhiteSpace(text)) return BadRequest("Tour chưa có IntroText");

        var textHash = GetShortHash(text);
        var existing = _context.TourAudios.FirstOrDefault(x => x.TourId == tourId && x.LanguageId == languageId && x.TextHash == textHash && x.IsActive);
        if (existing != null)
        {
            return Ok(new { url = existing.AudioUrl, cached = true, tourAudioId = existing.TourAudioId });
        }

        byte[] audioBytes;
        try
        {
            audioBytes = await _ttsService.GenerateAudioAsync(text, language.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi tạo audio tour {TourId}", tourId);
            return StatusCode(500, "Không tạo được audio tour");
        }

        var fileName = $"tour_{tourId}_{language.Code}_{textHash}.mp3";
        var folder = $"pltour/audio/tours/{tourId}";

        await using var stream = new MemoryStream(audioBytes);
        var url = await _cloudinaryService.UploadAudioAsync(stream, fileName, folder);

        var tourAudio = new TourAudio
        {
            TourId = tourId,
            LanguageId = languageId,
            TextHash = textHash,
            AudioUrl = url,
            Duration = 0,
            IsActive = true,
            CreatedDate = DateTime.UtcNow
        };

        _context.TourAudios.Add(tourAudio);
        await _context.SaveChangesAsync();

        return Ok(new { url, cached = false, tourAudioId = tourAudio.TourAudioId });
    }

    private static string GetShortHash(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..12];
    }
}
