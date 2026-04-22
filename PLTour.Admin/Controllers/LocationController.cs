using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services; 

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class LocationController : Controller
    {
        private readonly PLTourDbContext _context;
		private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _hostEnvironment;
        public LocationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Location
        public async Task<IActionResult> Index(string searchString, int? categoryId, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(l => l.Name.Contains(searchString) || l.Description.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(l => l.CategoryId == categoryId);
            }

            var totalItems = await query.CountAsync();
            var locations = await query
                .OrderBy(l => l.OrderIndex)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.SelectedCategory = categoryId;

            return View(locations);
        }

        // GET: Location/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            return View();
        }

        //POST: Location/Create
          [HttpPost]
          [ValidateAntiForgeryToken]
           public async Task<IActionResult> Create(Location location, IFormFile? imageFile)
        {
            // Loại bỏ lỗi validation cho navigation property
            if (location.Category != null)
            {
                ModelState.Remove("Category");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // ✅ SỬA: Upload ảnh qua API
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "locations");
                        location.ImageUrl = imageUrl;
                    }

                    location.CreatedDate = DateTime.UtcNow;
                    location.Radius = location.Radius > 0 ? location.Radius : 50;
                    _context.Add(location);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm địa điểm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Console.WriteLine(error.ErrorMessage);
            }

            return View(location);
        }

        // GET: Location/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations.FindAsync(id);
            if (location == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            return View(location);
        }

        // POST: Location/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Location location, IFormFile? imageFile)
        {
            if (id != location.LocationId)
            {
                return NotFound();
            }

            if (location.Category != null)
            {
                ModelState.Remove("Category");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingLocation = await _context.Locations.FindAsync(id);
                    if (existingLocation == null)
                    {
                        return NotFound();
                    }

                    // Xử lý yêu cầu xóa ảnh
                    if (Request.Form.ContainsKey("removeImage") && Request.Form["removeImage"] == "true")
                    {
                        if (!string.IsNullOrEmpty(existingLocation.ImageUrl))
                        {
                            // Xóa file cũ qua API? Hoặc bỏ qua vì file sẽ được xóa khi upload mới
                            existingLocation.ImageUrl = null;
                        }
                    }

                    // Cập nhật các trường cơ bản
                    existingLocation.Name = location.Name;
                    existingLocation.Description = location.Description;
                    existingLocation.Latitude = location.Latitude;
                    existingLocation.Longitude = location.Longitude;
                    existingLocation.Address = location.Address;
                    existingLocation.CategoryId = location.CategoryId;
                    existingLocation.OrderIndex = location.OrderIndex;
                    existingLocation.IsActive = location.IsActive;
                    existingLocation.UpdatedDate = DateTime.UtcNow;
                    existingLocation.Radius = location.Radius;

					// ✅ SỬA: Upload ảnh mới qua API
					if (imageFile != null && imageFile.Length > 0)
					{
						// Xóa ảnh cũ trên Cloudinary (nếu có)
						if (!string.IsNullOrEmpty(existingLocation.ImageUrl))
						{
							var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingLocation.ImageUrl);
							if (!string.IsNullOrEmpty(publicId))
								await _cloudinaryService.DeleteFileAsync(publicId);
						}

						// Upload ảnh mới
						var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "locations");
						existingLocation.ImageUrl = imageUrl;
					}

					_context.Update(existingLocation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật địa điểm thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LocationExists(location.LocationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            foreach (var error in errors)
            {
                Console.WriteLine(error.ErrorMessage);
            }

            return View(location);
        }

        // GET: Location/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var location = await _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations)
                    .ThenInclude(n => n.Language)
                .FirstOrDefaultAsync(m => m.LocationId == id);

            if (location == null)
            {
                return NotFound();
            }

            return View(location);
        }

        // POST: Location/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var location = await _context.Locations.FindAsync(id);
            if (location != null)
            {
                // Chỉ xóa record trong DB, không xóa file vật lý (hoặc có thể xóa sau)
                _context.Locations.Remove(location);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa địa điểm thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool LocationExists(int id)
        {
            return _context.Locations.Any(e => e.LocationId == id);
        }
    }
}