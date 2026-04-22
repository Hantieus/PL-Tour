using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext; // Chứa PLTourDbContext của bạn
using PLTour.Shared.Models.Entities; // Chứa class AnalyticsEvent
using PLTour.Shared.Models.DTO; // Chứa AnalyticsEventDto
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public AnalyticsController(PLTourDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // 1. API CHO MOBILE APP GỬI DỮ LIỆU LÊN
        // URL: POST /api/analytics/track
        // ==========================================
        [HttpPost("track")]
        public async Task<IActionResult> TrackEvent([FromBody] AnalyticsEventDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                var newEvent = new AnalyticsEvent
                {
                    SessionId = dto.SessionId,
                    DeviceId = dto.DeviceId,
                    EventType = dto.EventType,
                    LocationId = dto.LocationId,
                    TourId = dto.TourId,
                    LanguageCode = dto.LanguageCode,
                    Duration = dto.Duration,
                    Keyword = dto.Keyword,
                    Platform = dto.Platform,
                    HasAudio = dto.HasAudio,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    Timestamp = dto.Timestamp > DateTime.MinValue ? dto.Timestamp : DateTime.UtcNow
                };

                _context.AnalyticsEvents.Add(newEvent);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lưu sự kiện thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // ==========================================
        // 2. CÁC API CHO WEB ADMIN LẤY DỮ LIỆU VỀ
        // ==========================================

        // Lấy Top địa điểm nghe nhiều nhất
        // URL: GET /api/analytics/top-locations?take=5
        [HttpGet("top-locations")]
        public async Task<IActionResult> GetTopLocations([FromQuery] int take = 10)
        {
            try
            {
                var query = await _context.AnalyticsEvents
                    .Where(e => e.EventType == "listen_onsite" || e.EventType == "listen_remote")
                    .Where(e => e.LocationId != null)
                    .GroupBy(e => e.LocationId)
                    .Select(g => new TopLocationDto
                    {
                        LocationId = g.Key,
                        PlayCount = g.Count()
                    })
                    .OrderByDescending(x => x.PlayCount)
                    .Take(take)
                    .ToListAsync();

                return Ok(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Lấy thời gian nghe trung bình của từng POI
        // URL: GET /api/analytics/average-duration
        [HttpGet("average-duration")]
        public async Task<IActionResult> GetAverageDuration()
        {
            try
            {
                var query = await _context.AnalyticsEvents
                    .Where(e => e.EventType == "listen_duration" && e.Duration.HasValue && e.LocationId != null)
                    .GroupBy(e => e.LocationId)
                    .Select(g => new AvgDurationDto
                    {
                        LocationId = g.Key,
                        AverageSeconds = Math.Round(g.Average(e => e.Duration.Value), 2)
                    })
                    .OrderByDescending(x => x.AverageSeconds)
                    .ToListAsync();

                return Ok(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }

        // Lấy tọa độ để vẽ Heatmap (vùng nhiệt) và Tuyến đường
        // URL: GET /api/analytics/heatmap
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmapData()
        {
            try
            {
                // Lọc dữ liệu trong 30 ngày gần nhất để biểu đồ không bị quá tải
                var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

                var query = await _context.AnalyticsEvents
                    .Where(e => e.EventType == "location_ping"
                             && e.Latitude.HasValue
                             && e.Longitude.HasValue
                             && e.Timestamp >= thirtyDaysAgo)
                    .Select(e => new HeatmapPointDto
                    {
                        SessionId = e.SessionId,
                        Latitude = e.Latitude.Value,
                        Longitude = e.Longitude.Value,
                        Timestamp = e.Timestamp
                    })
                    .OrderBy(e => e.Timestamp)
                    .ToListAsync();

                return Ok(query);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server: {ex.Message}");
            }
        }
    }
}

// ==========================================
// CÁC CLASS DTO PHỤC VỤ TRẢ KẾT QUẢ CHO ADMIN
// (Bạn có thể để nguyên ở đây hoặc cắt sang project Shared nếu muốn)
// ==========================================
namespace PLTour.Shared.Models.DTO
{
    public class TopLocationDto
    {
        public int? LocationId { get; set; }
        public int PlayCount { get; set; }
    }

    public class AvgDurationDto
    {
        public int? LocationId { get; set; }
        public double AverageSeconds { get; set; }
    }

    public class HeatmapPointDto
    {
        public string SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}