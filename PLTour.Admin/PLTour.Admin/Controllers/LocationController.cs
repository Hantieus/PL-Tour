using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.Admin.Models.DbContext;
using PLTour.Admin.Models.Entities;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class LocationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public LocationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Location
        public async Task<IActionResult> Index(string searchString, int? categoryId, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Locations
                .Include(l => l.Category)
                .Include(l => l.Narrations) // THÊM DÒNG NÀY ĐỂ LOAD NARRATIONS
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

        // POST: Location/Create
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
                    // Xử lý upload ảnh
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Kiểm tra định dạng ảnh
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh .jpg, .jpeg, .png, .gif, .webp");
                            ViewBag.Categories = await _context.Categories.ToListAsync();
                            return View(location);
                        }

                        // Tạo thư mục uploads nếu chưa có
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/locations");
                        Directory.CreateDirectory(uploadsFolder);

                        // Tạo tên file duy nhất
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        location.ImageUrl = "/uploads/locations/" + uniqueFileName;
                    }

                    

                    // Set thời gian tạo
                    location.CreatedDate = DateTime.Now;
                    location.Radius = location.Radius > 0 ? location.Radius : 50;
                    // Thêm vào database
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

            // Nếu có lỗi, hiển thị lại form với dữ liệu đã nhập
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Log lỗi để debug
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

            // Loại bỏ validation cho navigation property
            if (location.Category != null)
            {
                ModelState.Remove("Category");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy đối tượng hiện tại từ database
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
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingLocation.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
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
                    existingLocation.UpdatedDate = DateTime.Now;
                    existingLocation.Radius = location.Radius;
                    // XỬ LÝ UPLOAD ẢNH MỚI
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        // Kiểm tra định dạng ảnh
                        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                        var fileExtension = Path.GetExtension(imageFile.FileName).ToLower();

                        if (!allowedExtensions.Contains(fileExtension))
                        {
                            ModelState.AddModelError("", "Chỉ chấp nhận file ảnh .jpg, .jpeg, .png, .gif, .webp");
                            ViewBag.Categories = await _context.Categories.ToListAsync();
                            return View(location);
                        }

                        // Xóa ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingLocation.ImageUrl))
                        {
                            var oldImagePath = Path.Combine(_hostEnvironment.WebRootPath, existingLocation.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Upload ảnh mới
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/locations");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        existingLocation.ImageUrl = "/uploads/locations/" + uniqueFileName;
                    }

                

                    // Cập nhật database
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

            // Nếu có lỗi, hiển thị lại form
            ViewBag.Categories = await _context.Categories.ToListAsync();

            // Log lỗi để debug
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
                .Include(l => l.Narrations) // THÊM DÒNG NÀY
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
                // Xóa file ảnh và audio
                if (!string.IsNullOrEmpty(location.ImageUrl))
                {
                    var imagePath = Path.Combine(_hostEnvironment.WebRootPath, location.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

              

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