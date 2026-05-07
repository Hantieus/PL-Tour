using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.API.Services;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;
using PLTour.API.Models.DbContext;

namespace PLTour.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Manager")]
public class TourNarrationController : ControllerBase
{
    private readonly PLTourDbContext _context;
    private readonly ITtsService _ttsService;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly ILogger<TourNarrationController> _logger;

    public TourNarrationController(
        PLTourDbContext context,
        ITtsService ttsService,
        ICloudinaryService cloudinaryService,
        ILogger<TourNarrationController> logger)
    {
        _context = context;
        _ttsService = ttsService;
        _cloudinaryService = cloudinaryService;
        _logger = logger;
    }

    [HttpPost("upsert")]
    public async Task<IActionResult> Upsert([FromBody] UpsertTourNarrationRequest request)
    {
        if (request.TourId <= 0 || request.LanguageId <= 0)
            return BadRequest("TourId và LanguageId không hợp lệ");

        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
            return BadRequest("Title và Content không được rỗng");

        var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourId == request.TourId);
        if (tour == null) return NotFound("Không tìm thấy tour");

        var language = await _context.Languages.FirstOrDefaultAsync(l => l.LanguageId == request.LanguageId && l.IsActive);
        if (language == null) return NotFound("Không tìm thấy ngôn ngữ");

        var existing = await _context.Set<TourNarration>()
            .FirstOrDefaultAsync(x => x.TourId == request.TourId && x.LanguageId == request.LanguageId && x.IsActive);

        var textHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.Title}|{request.Content}|{language.Code}")))[..12];
        var audioBytes = await _ttsService.GenerateAudioAsync(request.Content, language.Code);
        await using var stream = new MemoryStream(audioBytes);
        var audioUrl = await _cloudinaryService.UploadAudioAsync(stream, $"tour_{request.TourId}_{language.Code}_{textHash}.mp3", $"pltour/audio/tours/{request.TourId}");

        if (existing == null)
        {
            existing = new TourNarration
            {
                TourId = request.TourId,
                LanguageId = request.LanguageId,
                Title = request.Title,
                Content = request.Content,
                AudioUrl = audioUrl,
                Duration = request.Duration,
                IsDefault = request.IsDefault,
                Version = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
            _context.Add(existing);
        }
        else
        {
            existing.Title = request.Title;
            existing.Content = request.Content;
            existing.AudioUrl = audioUrl;
            existing.Duration = request.Duration;
            existing.IsDefault = request.IsDefault;
            existing.IsActive = request.IsActive;
            existing.Version += 1;
            existing.UpdatedDate = DateTime.UtcNow;
        }

        if (request.IsDefault)
        {
            var defaults = await _context.Set<TourNarration>()
                .Where(x => x.TourId == request.TourId && x.IsDefault && x.LanguageId != request.LanguageId)
                .ToListAsync();
            foreach (var item in defaults)
                item.IsDefault = false;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            success = true,
            tourNarrationId = existing.TourNarrationId,
            audioUrl = existing.AudioUrl
        });
    }
}

public class UpsertTourNarrationRequest
{
    public int TourId { get; set; }
    public int LanguageId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Duration { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
