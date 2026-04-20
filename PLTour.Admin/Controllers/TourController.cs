using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class TourController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ICloudinaryService _cloudinaryService;
        public TourController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Tour
        public async Task<IActionResult> Index()
        {
            var tours = await _context.Tours
                .Include(t => t.TourLocations)
                .OrderByDescending(t => t.CreatedDate)
                .ToListAsync();
            return View(tours);
        }

        // GET: Tour/Create
        public async Task<IActionResult> Create()
        {
            var locations = await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            ViewData["Locations"] = locations;  // Dùng ViewData
            return View();
        }

        // POST: Tour/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Tour tour, IFormFile? imageFile, int[] selectedLocationIds)
        {
            if (ModelState.IsValid)
            {
                // Upload ảnh lên Cloudinary
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "tours");
                    tour.ImageUrl = imageUrl;
                }

                tour.CreatedDate = DateTime.UtcNow;
                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

                // Thêm các địa điểm vào tour
                if (selectedLocationIds != null && selectedLocationIds.Any())
                {
                    for (int i = 0; i < selectedLocationIds.Length; i++)
                    {
                        _context.TourLocations.Add(new TourLocation
                        {
                            TourId = tour.TourId,
                            LocationId = selectedLocationIds[i],
                            OrderIndex = i
                        });
                    }
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Thêm tour thành công!";
                return RedirectToAction(nameof(Index));
            }

            // Nếu có lỗi, load lại danh sách địa điểm
            var locations = await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            ViewBag.Locations = new MultiSelectList(
                await _context.Locations.Where(l => l.IsActive).ToListAsync(),
                "LocationId", "Name", selectedLocationIds);
            return View(tour);
        }

        // GET: Tour/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours
                .Include(t => t.TourLocations)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            // Lấy danh sách địa điểm đã chọn
            var selectedIds = tour.TourLocations
                .OrderBy(tl => tl.OrderIndex)
                .Select(tl => tl.LocationId)
                .ToArray();

            // Lấy tất cả địa điểm
            var locations = await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            // Gán vào ViewBag dưới dạng SelectList có selected values
            ViewBag.Locations = new MultiSelectList(locations, "LocationId", "Name", selectedIds);

            return View(tour);
        }

        // POST: Tour/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Tour tour, IFormFile? imageFile, int[] selectedLocationIds)
        {
            if (id != tour.TourId) return NotFound();

            var existingTour = await _context.Tours
                .Include(t => t.TourLocations)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (existingTour == null) return NotFound();

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Xóa ảnh cũ
                    if (!string.IsNullOrEmpty(existingTour.ImageUrl))
                    {
                        var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingTour.ImageUrl);
                        if (!string.IsNullOrEmpty(publicId))
                            await _cloudinaryService.DeleteFileAsync(publicId);
                    }

                    // Upload ảnh mới
                    var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "tours");
                    existingTour.ImageUrl = imageUrl;
                }

                existingTour.Name = tour.Name;
                existingTour.Duration = tour.Duration;
                existingTour.IntroText = tour.IntroText;
                existingTour.IsActive = tour.IsActive;
                existingTour.UpdatedDate = DateTime.UtcNow;

                // Cập nhật danh sách địa điểm
                _context.TourLocations.RemoveRange(existingTour.TourLocations);
                if (selectedLocationIds != null && selectedLocationIds.Any())
                {
                    for (int i = 0; i < selectedLocationIds.Length; i++)
                    {
                        _context.TourLocations.Add(new TourLocation
                        {
                            TourId = tour.TourId,
                            LocationId = selectedLocationIds[i],
                            OrderIndex = i
                        });
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật tour thành công!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Locations = new MultiSelectList(
                await _context.Locations.Where(l => l.IsActive).ToListAsync(),
                "LocationId", "Name", selectedLocationIds);
            return View(tour);
        }

        // POST: Tour/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var tour = await _context.Tours.FindAsync(id);
            if (tour != null)
            {
                if (!string.IsNullOrEmpty(tour.ImageUrl))
                {
                    var path = Path.Combine(_hostEnvironment.WebRootPath, tour.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
                _context.Tours.Remove(tour);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa tour thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Tour/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var tour = await _context.Tours
                .Include(t => t.TourLocations)
                    .ThenInclude(tl => tl.Location)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            return View(tour);
        }
    }
}