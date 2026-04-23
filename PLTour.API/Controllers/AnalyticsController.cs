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

        [HttpPost("track")]
        public async Task<IActionResult> TrackEvent([FromBody] AnalyticsEventDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Dữ liệu không hợp lệ.");
            }

            try
            {
                // PHẦN QUAN TRỌNG: Xử lý thời gian chuẩn UTC cho PostgreSQL
                DateTime timestampUtc;
                if (dto.Timestamp > DateTime.MinValue)
                {
                    timestampUtc = DateTime.SpecifyKind(dto.Timestamp, DateTimeKind.Utc);
                }
                else
                {
                    timestampUtc = DateTime.UtcNow;
                }

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
                    Timestamp = timestampUtc
                };

                _context.AnalyticsEvents.Add(newEvent);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Lưu sự kiện thành công!" });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : "N/A";
                return StatusCode(500, $"Lỗi lưu DB: {ex.Message} | Chi tiết: {innerMessage}");
            }
        }

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

        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmapData()
        {
            try
            {
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

// Khai báo các DTO trực tiếp trong namespace này để Controller tìm thấy ngay
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
        public string? SessionId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}