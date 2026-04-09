using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.DTO;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ToursController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public ToursController(PLTourDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TourDto>>> GetTours()
        {
            // 1. Lấy dữ liệu từ DB, Include thêm Narrations và Language
            var tours = await _context.Tours
                .Include(t => t.TourLocations)
                    .ThenInclude(tl => tl.Location)
                        .ThenInclude(l => l.Narrations)
                            .ThenInclude(n => n.Language) // Phải lấy Language để có mã "vi", "en"
                .Where(t => t.IsActive)
                .ToListAsync();

            // 2. Chuyển sang DTO
            var tourDtos = tours.Select(t => new TourDto
            {
                TourId = t.TourId,
                Name = t.Name,
                Duration = t.Duration,
                IntroText = t.IntroText,
                ImageUrl = t.ImageUrl,
                Locations = t.TourLocations
                    .OrderBy(tl => tl.OrderIndex)
                    .Select(tl => new LocationDto
                    {
                        LocationId = tl.Location.LocationId,
                        Name = tl.Location.Name,
                        Latitude = tl.Location.Latitude,
                        Longitude = tl.Location.Longitude,
                        Address = tl.Location.Address,
                        Description = tl.Location.Description,
                        ImageUrl = tl.Location.ImageUrl,
                        CategoryId = tl.Location.CategoryId,

                        // BỔ SUNG: Chuyển Narrations sang DTO
                        Narrations = tl.Location.Narrations?.Select(n => new NarrationDto
                        {
                            LanguageCode = n.Language != null ? n.Language.Code : "vi",
                            Title = n.Title,
                            Content = n.Content,
                            AudioUrl = n.AudioUrl,
                            IsDefault = n.IsDefault
                        }).ToList()
                    }).ToList()
            }).ToList();

            return Ok(tourDtos);
        }
    }
}