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
            // 1. Lấy dữ liệu từ DB, Include các bảng liên quan
            var tours = await _context.Tours
                .Include(t => t.TourLocations)
                    .ThenInclude(tl => tl.Location)
                        .ThenInclude(l => l.Narrations)
                            .ThenInclude(n => n.Language)
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
                        Radius = tl.Location.Radius,
                        OrderIndex = tl.OrderIndex, // Giữ đúng thứ tự Tour

                        // Ánh xạ chi tiết Narrations (Đã bổ sung NarrationId và các thông tin ngôn ngữ)
                        Narrations = tl.Location.Narrations?.Select(n => new NarrationDto
                        {
                            NarrationId = n.NarrationId,
                            LanguageId = n.LanguageId,
                            LanguageCode = n.Language?.Code ?? "vi",
                            LanguageName = n.Language?.Name ?? "Tiếng Việt",
                            Title = n.Title,
                            Content = n.Content,
                            AudioUrl = n.AudioUrl,
                            Duration = n.Duration,
                            IsDefault = n.IsDefault
                        }).ToList()
                    }).ToList()
            }).ToList();

            return Ok(tourDtos);
        }
    }
}