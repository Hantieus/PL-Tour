using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Models.DTO;

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

        // ========== 1. Timeline ==========
        [HttpGet("timeline")]
        public async Task<IActionResult> GetTimeline(DateTime? from, DateTime? to, int? days, string? eventType, string? languageCode)
        {
            var hasCustomRange = from.HasValue || to.HasValue;
            var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1);
            var startDate = hasCustomRange
                ? (from ?? endDate.AddDays(-30)).Date
                : DateTime.UtcNow.AddDays(-(days ?? 30)).Date;

            var query = _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate)
                .Where(e => e.event_type != "location_ping");

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.event_type == eventType);
            if (!string.IsNullOrWhiteSpace(languageCode))
                query = query.Where(e => e.language_code == languageCode);

            var timeline = await query
                .GroupBy(e => e.timestamp.Date)
                .Select(g => new TimelinePointDto
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(t => t.Date)
                .ToListAsync();

            return Ok(timeline);
        }

        // ========== 2. Breakdown ==========
        [HttpGet("breakdown")]
        public async Task<IActionResult> GetBreakdown(DateTime? from, DateTime? to, int? days, string? languageCode)
        {
            var hasCustomRange = from.HasValue || to.HasValue;
            var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1);
            var startDate = hasCustomRange
                ? (from ?? endDate.AddDays(-30)).Date
                : DateTime.UtcNow.AddDays(-(days ?? 30)).Date;

            var query = _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate)
                .Where(e => e.event_type != "location_ping");

            if (!string.IsNullOrWhiteSpace(languageCode))
                query = query.Where(e => e.language_code == languageCode);

            var breakdown = await query
                .GroupBy(e => e.event_type)
                .Select(g => new BreakdownDto
                {
                    EventType = g.Key ?? "unknown",
                    Count = g.Count()
                })
                .OrderByDescending(b => b.Count)
                .ToListAsync();

            return Ok(breakdown);
        }

        // ========== 3. Top Locations ==========
        [HttpGet("top-locations-detailed")]
        public async Task<IActionResult> GetTopLocationsDetailed(DateTime? from, DateTime? to, int? days, int take = 10, string? languageCode = null)
        {
            var hasCustomRange = from.HasValue || to.HasValue;
            var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1);
            var startDate = hasCustomRange
                ? (from ?? endDate.AddDays(-30)).Date
                : DateTime.UtcNow.AddDays(-(days ?? 30)).Date;

            var listenStartQuery = _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate)
                .Where(e => e.location_id.HasValue)
                .Where(e => e.event_type == "listen_onsite" || e.event_type == "listen_remote");

            if (!string.IsNullOrWhiteSpace(languageCode))
                listenStartQuery = listenStartQuery.Where(e => e.language_code == languageCode);

            var durationQuery = _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate)
                .Where(e => e.location_id.HasValue)
                .Where(e => e.event_type == "listen_onsite" || e.event_type == "listen_remote" || e.event_type == "listen_duration")
                .Where(e => e.duration.HasValue);

            if (!string.IsNullOrWhiteSpace(languageCode))
                durationQuery = durationQuery.Where(e => e.language_code == languageCode);

            var listenCounts = await listenStartQuery
                .GroupBy(e => e.location_id)
                .Select(g => new { LocationId = g.Key ?? 0, Count = g.Count() })
                .ToListAsync();

            var durationStats = await durationQuery
                .GroupBy(e => e.location_id)
                .Select(g => new
                {
                    LocationId = g.Key ?? 0,
                    TotalDuration = g.Sum(e => e.duration ?? 0),
                    AvgDuration = g.Average(e => e.duration ?? 0),
                    TopLanguage = g.GroupBy(e => e.language_code)
                        .OrderByDescending(lg => lg.Count())
                        .Select(lg => lg.Key)
                        .FirstOrDefault() ?? "N/A"
                })
                .ToListAsync();

            var listenCountMap = listenCounts.ToDictionary(x => x.LocationId, x => x.Count);
            var durationMap = durationStats.ToDictionary(x => x.LocationId, x => x);

            var locationIds = listenCountMap.Keys.Union(durationMap.Keys).ToList();
            var topLocations = locationIds
                .Select(locationId =>
                {
                    durationMap.TryGetValue(locationId, out var duration);
                    listenCountMap.TryGetValue(locationId, out var listenCount);

                    return new TopLocationDetailDto
                    {
                        LocationId = locationId,
                        ListenCount = listenCount,
                        TotalDuration = duration?.TotalDuration ?? 0,
                        AvgDuration = duration?.AvgDuration ?? 0,
                        TopLanguage = duration?.TopLanguage ?? "N/A"
                    };
                })
                .OrderByDescending(l => l.ListenCount)
                .ThenByDescending(l => l.TotalDuration)
                .Take(take)
                .ToList();

            // Lấy tên địa điểm
            var topLocationIds = topLocations.Select(l => l.LocationId).ToList();
            var locations = await _context.Locations
                .Where(l => topLocationIds.Contains(l.LocationId))
                .ToDictionaryAsync(l => l.LocationId, l => l.Name);

            foreach (var item in topLocations)
            {
                item.LocationName = locations.GetValueOrDefault(item.LocationId, $"Địa điểm #{item.LocationId}");
            }

            return Ok(topLocations);
        }

        // 4. Overall stats
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(DateTime? from, DateTime? to, int? days, string? eventType, string? languageCode)
        {
            var hasCustomRange = from.HasValue || to.HasValue;
            var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1);
            var startDate = hasCustomRange
                ? (from ?? endDate.AddDays(-30)).Date
                : DateTime.UtcNow.AddDays(-(days ?? 30)).Date;

            var query = _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate);

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(e => e.event_type == eventType);
            if (!string.IsNullOrWhiteSpace(languageCode))
                query = query.Where(e => e.language_code == languageCode);

            var totalEvents = await query.CountAsync();
            var uniqueDevices = await query
                .Where(e => e.device_id != null)
                .Select(e => e.device_id)
                .Distinct()
                .CountAsync();
            var uniqueSessions = await query
                .Where(e => e.session_id != null)
                .Select(e => e.session_id)
                .Distinct()
                .CountAsync();

            var listenQuery = query.Where(e => e.event_type == "listen_onsite" || e.event_type == "listen_remote");
            var totalListens = await listenQuery.CountAsync();
            var onsiteCount = await listenQuery.CountAsync(e => e.event_type == "listen_onsite");
            var remoteCount = await listenQuery.CountAsync(e => e.event_type == "listen_remote");

            var durationValues = await listenQuery
                .Where(e => e.duration.HasValue)
                .Select(e => e.duration!.Value)
                .ToListAsync();

            var avgDuration = durationValues.Count > 0 ? durationValues.Average() : 0;

            return Ok(new OverviewDto
            {
                TotalEvents = totalEvents,
                UniqueDevices = uniqueDevices,
                UniqueSessions = uniqueSessions,
                TotalListens = totalListens,
                AvgDurationSeconds = Math.Round(avgDuration, 1),
                OnsiteCount = onsiteCount,
                RemoteCount = remoteCount
            });
        }

        // 5. Heatmap data
        [HttpGet("heatmap")]
        public async Task<IActionResult> GetHeatmap(DateTime? from, DateTime? to)
        {
            var endDate = (to ?? DateTime.UtcNow).Date.AddDays(1);
            var startDate = (from ?? DateTime.UtcNow.AddDays(-30)).Date;

            var points = await _context.AnalyticsEvents
                .Where(e => e.timestamp >= startDate && e.timestamp < endDate)
                .Where(e => e.latitude.HasValue && e.longitude.HasValue)
                .Select(e => new HeatmapPointDto
                {
                    Latitude = e.latitude.Value,
                    Longitude = e.longitude.Value,
                    Weight = e.event_type == "listen_onsite" ? 2 : 1
                })
                .ToListAsync();

            return Ok(points);
        }
    }
}
