using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models;
using PLTour.Shared.Models.DTO;
using PLTour.Shared.Models.Entities;

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

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations(
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radiusInMeters = 100)
        {
            var query = _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .Where(l => l.IsActive);

            if (lat.HasValue && lng.HasValue && radiusInMeters.HasValue)
            {
                var locations = await query.ToListAsync();
                var filteredLocations = locations.Where(location =>
                {
                    var distance = CalculateDistance(lat.Value, lng.Value, location.Latitude, location.Longitude);
                    return distance <= radiusInMeters.Value;
                }).ToList();

                return Ok(filteredLocations.Select(MapToLocationDto).ToList());
            }

            var allLocations = await query.ToListAsync();
            return Ok(allLocations.Select(MapToLocationDto).ToList());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .FirstOrDefaultAsync(l => l.LocationId == id);

            if (location == null) return NotFound();
            return Ok(MapToLocationDto(location));
        }

        private LocationDto MapToLocationDto(Location l)
        {
            return new LocationDto
            {
                LocationId = l.LocationId,
                Name = l.Name,
                Description = l.Description,
                Latitude = l.Latitude,
                Longitude = l.Longitude,
                Address = l.Address,
                CategoryId = l.CategoryId,
                CategoryName = l.Category?.Name ?? "",
                ImageUrl = l.ImageUrl,
                OrderIndex = l.OrderIndex,
                Radius = l.Radius,
                IsActive = l.IsActive,
                Narrations = l.Narrations?.Select(n => new NarrationDto
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
                }).ToList()
            };
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000;
            var lat1Rad = lat1 * Math.PI / 180;
            var lat2Rad = lat2 * Math.PI / 180;
            var deltaLat = (lat2 - lat1) * Math.PI / 180;
            var deltaLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                    Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                    Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
    }
}