using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Models.DTO;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NarrationsController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public NarrationsController(PLTourDbContext context)
        {
            _context = context;
        }

        // GET: api/narrations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NarrationDto>>> GetNarrations()
        {
            var narrations = await _context.Narrations
                .Include(n => n.Location)
                .Include(n => n.Language)
                .Where(n => n.IsActive)
                .OrderByDescending(n => n.CreatedDate)
                .ToListAsync();

            return Ok(narrations.Select(MapToNarrationDto).ToList());
        }

        // GET: api/narrations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NarrationDto>> GetNarration(int id)
        {
            var narration = await _context.Narrations
                .Include(n => n.Location)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.NarrationId == id);

            if (narration == null)
            {
                return NotFound();
            }

            return Ok(MapToNarrationDto(narration));
        }

        // GET: api/narrations/by-location/5
        [HttpGet("by-location/{locationId}")]
        public async Task<ActionResult<IEnumerable<NarrationDto>>> GetNarrationsByLocation(
            int locationId,
            [FromQuery] string? languageCode = null)
        {
            var query = _context.Narrations
                .Include(n => n.Language)
                .Where(n => n.LocationId == locationId && n.IsActive);

            if (!string.IsNullOrEmpty(languageCode))
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Code == languageCode);

                if (language != null)
                {
                    var narration = await query
                        .FirstOrDefaultAsync(n => n.LanguageId == language.LanguageId);

                    if (narration != null)
                    {
                        return Ok(new List<NarrationDto> { MapToNarrationDto(narration) });
                    }
                }
            }

            var defaultNarration = await query
                .FirstOrDefaultAsync(n => n.IsDefault);

            if (defaultNarration != null)
            {
                return Ok(new List<NarrationDto> { MapToNarrationDto(defaultNarration) });
            }

            var narrations = await query.ToListAsync();
            return Ok(narrations.Select(MapToNarrationDto).ToList());
        }

        // GET: api/narrations/by-location/5/default
        [HttpGet("by-location/{locationId}/default")]
        public async Task<ActionResult<NarrationDto>> GetDefaultNarration(int locationId)
        {
            var narration = await _context.Narrations
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.LocationId == locationId && n.IsDefault && n.IsActive);

            if (narration == null)
            {
                return NotFound();
            }

            return Ok(MapToNarrationDto(narration));
        }

        private NarrationDto MapToNarrationDto(Narration n)
        {
            return new NarrationDto
            {
                NarrationId = n.NarrationId,
                LocationId = n.LocationId,
                LanguageId = n.LanguageId,
                LanguageCode = n.Language?.Code ?? "",
                LanguageName = n.Language?.Name ?? "",
                Title = n.Title,
                Content = n.Content,
                AudioUrl = n.AudioUrl,
                Duration = n.Duration,
                IsDefault = n.IsDefault,
                IsActive = n.IsActive,
                CreatedDate = n.CreatedDate,
                Version = n.Version
            };
        }
    }
}