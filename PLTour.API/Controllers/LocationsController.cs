using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models;
using PLTour.API.Models.DTO;

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
        public async Task<ActionResult<IEnumerable<LocationDto>>> GetLocations(
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radiusInMeters = 100)  // Đổi tên và đơn vị mét
        {
            var query = _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .Where(l => l.IsActive);

            // Nếu có tọa độ, lọc theo bán kính thực tế (mét)
            if (lat.HasValue && lng.HasValue && radiusInMeters.HasValue)
            {
                var locations = await query.ToListAsync();

                // Lọc bằng công thức Haversine
                var filteredLocations = locations.Where(location =>
                {
                    double distance = CalculateDistance(
                        lat.Value, lng.Value,
                        location.Latitude, location.Longitude
                    );
                    return distance <= radiusInMeters.Value;
                }).ToList();

                // Chuyển đổi sang DTO
                var result = filteredLocations.OrderBy(l => l.OrderIndex)
                    .Select(l => MapToLocationDto(l)).ToList();

                return Ok(result);
            }

            // Không có tọa độ, trả về tất cả
            var allLocations = await query.OrderBy(l => l.OrderIndex).ToListAsync();
            var allResult = allLocations.Select(l => MapToLocationDto(l)).ToList();
            return Ok(allResult);
        }

        // GET: api/locations/5
        [HttpGet("{id}")]
        public async Task<ActionResult<LocationDto>> GetLocation(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .FirstOrDefaultAsync(l => l.LocationId == id);

            if (location == null)
                return NotFound();

            return Ok(MapToLocationDto(location));
        }

        // GET: api/locations/5/narrations
        [HttpGet("{id}/narrations")]
        public async Task<ActionResult<IEnumerable<NarrationDto>>> GetNarrationsByLocation(
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

        // ==================== HÀM TÍNH KHOẢNG CÁCH ====================
        /// <summary>
        /// Tính khoảng cách giữa 2 tọa độ (công thức Haversine)
        /// </summary>
        /// <param name="lat1">Vĩ độ điểm 1</param>
        /// <param name="lon1">Kinh độ điểm 1</param>
        /// <param name="lat2">Vĩ độ điểm 2</param>
        /// <param name="lon2">Kinh độ điểm 2</param>
        /// <returns>Khoảng cách (mét)</returns>
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000; // Bán kính trái đất (mét)

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

        // ==================== HÀM MAP ENTITY -> DTO ====================
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
                IsActive = l.IsActive,
                Radius = l.Radius,  // ✅ THÊM DÒNG NÀY
                Narrations = l.Narrations?.Select(n => new NarrationDto
                {
                    NarrationId = n.NarrationId,
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