using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.Admin.Models.DbContext;
using PLTour.Admin.Models.Entities;
using PLTour.Admin.Models.DbContext;
using PLTour.Admin.Models.Entities;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationsController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public LocationsController(PLTourDbContext context)
        {
            _context = context;
        }

        // GET: api/locations
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Location>>> GetLocations(
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radius = 1)
        {
            var query = _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                .Where(l => l.IsActive);

            if (lat.HasValue && lng.HasValue)
            {
                query = query.Where(l =>
                    Math.Abs(l.Latitude - lat.Value) <= radius.Value &&
                    Math.Abs(l.Longitude - lng.Value) <= radius.Value);
            }

            return await query.OrderBy(l => l.OrderIndex).ToListAsync();
        }

        // GET: api/locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Location>> GetLocation(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .FirstOrDefaultAsync(l => l.LocationId == id);

            if (location == null)
                return NotFound();

            return location;
        }

        // GET: api/locations/5/narrations
        [HttpGet("{id}/narrations")]
        public async Task<ActionResult<IEnumerable<Narration>>> GetNarrationsByLocation(
            int id,
            [FromQuery] string? languageCode = null)
        {
            var query = _context.Narrations
                .Include(n => n.Language)
                .Where(n => n.LocationId == id && n.IsActive);

            if (!string.IsNullOrEmpty(languageCode))
            {
                var language = await _context.Languages
                    .FirstOrDefaultAsync(l => l.Code == languageCode);

                if (language != null)
                {
                    var narration = await query
                        .FirstOrDefaultAsync(n => n.LanguageId == language.LanguageId);

                    if (narration != null)
                        return Ok(new List<Narration> { narration });
                }
            }

            var defaultNarration = await query
                .FirstOrDefaultAsync(n => n.IsDefault);

            if (defaultNarration != null)
                return Ok(new List<Narration> { defaultNarration });

            return await query.ToListAsync();
        }
    }
}