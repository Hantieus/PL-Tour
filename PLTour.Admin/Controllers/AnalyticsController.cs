using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class AnalyticsController : Controller
    {
        private readonly PLTourDbContext _context;

        public AnalyticsController(PLTourDbContext context)
        {
            _context = context;
        }

        // Dashboard tổng quan
        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            ViewBag.TotalEvents = await _context.AnalyticsEvents.CountAsync();
            ViewBag.UniqueDevices = await _context.AnalyticsEvents.Select(e => e.device_id).Distinct().CountAsync();
            ViewBag.TotalSessions = await _context.AnalyticsEvents.Select(e => e.session_id).Distinct().CountAsync();
            ViewBag.TodayEvents = await _context.AnalyticsEvents.CountAsync(e => e.timestamp >= today);
            ViewBag.WeekEvents = await _context.AnalyticsEvents.CountAsync(e => e.timestamp >= weekAgo);

            // Top địa điểm được quan tâm
            ViewBag.TopLocations = await _context.AnalyticsEvents
                .Where(e => e.location_id.HasValue)
                .GroupBy(e => e.location_id)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(10)
                .ToListAsync();

            return View();
        }

        // Danh sách sự kiện
        public async Task<IActionResult> Events(DateTime? from, DateTime? to, string? eventType, int page = 1)
        {
            int pageSize = 50;
            var query = _context.AnalyticsEvents.AsQueryable();

            if (from.HasValue) query = query.Where(e => e.timestamp >= from.Value);
            if (to.HasValue) query = query.Where(e => e.timestamp <= to.Value);
            if (!string.IsNullOrEmpty(eventType)) query = query.Where(e => e.event_type == eventType);

            var totalItems = await query.CountAsync();
            var events = await query
                .OrderByDescending(e => e.timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.From = from?.ToString("yyyy-MM-dd");
            ViewBag.To = to?.ToString("yyyy-MM-dd");
            ViewBag.SelectedType = eventType;
            ViewBag.EventTypes = await _context.AnalyticsEvents
                .Select(e => e.event_type)
                .Distinct()
                .ToListAsync();

            return View(events);
        }

        // Chi tiết thiết bị
        public async Task<IActionResult> Device(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var events = await _context.AnalyticsEvents
                .Where(e => e.device_id == id)
                .OrderByDescending(e => e.timestamp)
                .Take(100)
                .ToListAsync();

            if (!events.Any()) return NotFound();

            ViewBag.DeviceId = id;
            ViewBag.FirstSeen = events.Last().timestamp;
            ViewBag.LastSeen = events.First().timestamp;
            ViewBag.TotalEvents = events.Count;
            ViewBag.UniqueSessions = events.Select(e => e.session_id).Distinct().Count();

            return View(events);
        }
    }
}