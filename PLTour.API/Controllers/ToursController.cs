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
            // =====================================================================
            // [CODE TƯƠNG LAI] ĐOẠN NÀY LÀ CODE GỌI DATABASE THẬT.
            // DO BÊN ADMIN CHƯA XONG NÊN TẠM THỜI COMMENT LẠI ĐỂ CHẠY DEMO.
            // KHI NÀO ADMIN XONG, CHỈ CẦN BỎ COMMENT ĐOẠN NÀY VÀ XÓA ĐOẠN DEMO BÊN DƯỚI.
            // =====================================================================
            /*
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
            */

            // =====================================================================
            // [CODE DEMO] DỮ LIỆU GIẢ LẬP ĐỂ TEST APP TRONG LÚC CHỜ ADMIN
            // =====================================================================
            var demoTours = new List<TourDto>
            {
                new TourDto
                {
                    TourId = 1,
                    Name = "Khám phá trung tâm Sài Gòn",
                    Duration = 150, // 2 tiếng 30 phút
                    IntroText = "Chào mừng bạn đến với tour Sài Gòn lịch sử. Hôm nay chúng ta sẽ đi qua các địa danh nổi tiếng nhất.",
                    ImageUrl = "https://images.unsplash.com/photo-1583417319070-4a69db38a482?w=800&q=80", // Ảnh tĩnh để test
                    Locations = new List<LocationDto>
                    {
                        new LocationDto
                        {
                            LocationId = 1,
                            Name = "Dinh Độc Lập",
                            Latitude = 10.7769,
                            Longitude = 106.6951,
                            Address = "135 Nam Kỳ Khởi Nghĩa, Quận 1",
                            Description = "Di tích lịch sử quốc gia đặc biệt, nơi lưu giữ nhiều dấu ấn lịch sử.",
                            CategoryId = 1,
                            ImageUrl = "https://images.unsplash.com/photo-1691230495861-d0b84f3c4db5?w=500&q=80",
                            Narrations = new List<NarrationDto>
                            {
                                new NarrationDto { LanguageCode = "vi", Title = "Dinh Độc Lập", Content = "Dinh Độc Lập được khởi công xây dựng ngày 1 tháng 7 năm 1962...", IsDefault = true }
                            }
                        },
                        new LocationDto
                        {
                            LocationId = 2,
                            Name = "Nhà thờ Đức Bà",
                            Latitude = 10.7797,
                            Longitude = 106.6990,
                            Address = "Số 01 Công xã Paris, Quận 1",
                            Description = "Công trình kiến trúc tôn giáo biểu tượng của thành phố.",
                            CategoryId = 1,
                            ImageUrl = "https://images.unsplash.com/photo-1600582230676-47b194fb8cd6?w=500&q=80",
                            Narrations = new List<NarrationDto>
                            {
                                new NarrationDto { LanguageCode = "vi", Title = "Nhà thờ Đức Bà", Content = "Nhà thờ Đức Bà Sài Gòn là một trong những công trình kiến trúc cổ kính...", IsDefault = true }
                            }
                        }
                    }
                },
                new TourDto
                {
                    TourId = 2,
                    Name = "Food Tour Chợ Lớn",
                    Duration = 90, // 1 tiếng 30 phút
                    IntroText = "Trải nghiệm ẩm thực phong phú tại khu vực Chợ Lớn sầm uất.",
                    ImageUrl = "https://images.unsplash.com/photo-1555126634-ae23522be740?w=800&q=80",
                    Locations = new List<LocationDto>
                    {
                        new LocationDto
                        {
                            LocationId = 3,
                            Name = "Chợ Bình Tây",
                            Latitude = 10.7495,
                            Longitude = 106.6509,
                            Address = "57a Tháp Mười, Quận 6",
                            Description = "Khu chợ đầu mối lớn và cổ kính nhất của cộng đồng người Hoa.",
                            CategoryId = 2,
                            Narrations = new List<NarrationDto>
                            {
                                new NarrationDto { LanguageCode = "vi", Title = "Chợ Bình Tây", Content = "Chợ Bình Tây hay còn gọi là Chợ Lớn Mới, nổi bật với kiến trúc Á Đông...", IsDefault = true }
                            }
                        }
                    }
                }
            };

            return Ok(demoTours);
        }
    }
}