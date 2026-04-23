using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Models.DTO;
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
                    session_id = dto.SessionId,
                    device_id = dto.DeviceId,
                    event_type = dto.EventType,
                    location_id = dto.LocationId,
                    tour_id = dto.TourId,
                    language_code = dto.LanguageCode,
                    duration = dto.Duration,
                    keyword = dto.Keyword,
                    platform = dto.Platform,
                    has_audio = dto.HasAudio,
                    latitude = dto.Latitude,
                    longitude = dto.Longitude,
                    timestamp = dto.Timestamp > DateTime.MinValue ? dto.Timestamp : DateTime.UtcNow
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
                    .Where(e => e.event_type == "listen_onsite" || e.event_type == "listen_remote")
                    .Where(e => e.location_id != null)
                    .GroupBy(e => e.location_id)
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
                    .Where(e => e.event_type == "listen_duration" && e.duration.HasValue && e.location_id != null)
                    .GroupBy(e => e.location_id)
                    .Select(g => new AvgDurationDto
                    {
                        LocationId = g.Key,
                        AverageSeconds = Math.Round(g.Average(e => e.duration.Value), 2)
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
                    .Where(e => e.event_type == "location_ping"
                             && e.latitude.HasValue
                             && e.longitude.HasValue
                             && e.timestamp >= thirtyDaysAgo)
                    .Select(e => new HeatmapPointDto
                    {
                        SessionId = e.session_id,
                        Latitude = e.latitude.Value,
                        Longitude = e.longitude.Value,
                        Timestamp = e.timestamp
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