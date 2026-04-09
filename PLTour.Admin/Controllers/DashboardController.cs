using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;

namespace PLTour.Admin.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly PLTourDbContext _context;

        public DashboardController(PLTourDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalLocations = await _context.Locations.CountAsync();
            ViewBag.TotalVendors = await _context.Vendors.CountAsync();
            ViewBag.PendingVendors = await _context.Vendors.CountAsync(v => v.Status == "Pending");
            ViewBag.TotalUsers = await _context.Users.CountAsync();

            // Lấy danh sách vendors pending
            var pendingVendors = await _context.Vendors
                .Include(v => v.Category)
                .Where(v => v.Status == "Pending")
                .OrderByDescending(v => v.CreatedDate)
                .Take(5)
                .ToListAsync();

            return View(pendingVendors);
        }
        // GET: Dashboard/GetLocationStats
        public async Task<IActionResult> GetLocationStats()
        {
            var stats = await _context.Locations
                .Include(l => l.Category)
                .GroupBy(l => l.Category.Name)
                .Select(g => new { Category = g.Key, Count = g.Count() })
                .ToListAsync();

            var labels = stats.Select(s => s.Category).ToArray();
            var values = stats.Select(s => s.Count).ToArray();

            return Json(new { labels, values });
        }
    }

}