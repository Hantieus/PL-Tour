using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLTour.Admin.Models.DbContext;
using PLTour.Admin.Models.Entities;
using PLTour.Admin.Models.ViewModels;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class NarrationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public NarrationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // GET: Narration
        public async Task<IActionResult> Index(string searchString, int? languageId, int page = 1)
        {
            int pageSize = 10;

            var query = _context.Narrations
                .Include(n => n.Location)
                .Include(n => n.Language)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(n => n.Title.Contains(searchString)
                                       || n.Content.Contains(searchString)
                                       || n.Location.Name.Contains(searchString));
            }

            if (languageId.HasValue)
            {
                query = query.Where(n => n.LanguageId == languageId);
            }

            var totalItems = await query.CountAsync();
            var narrations = await query
                .OrderByDescending(n => n.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.Languages = await _context.Languages.ToListAsync();
            ViewBag.SelectedLanguage = languageId;

            return View(narrations);
        }

        // GET: Narration/ByLocation/5
        public async Task<IActionResult> ByLocation(int locationId)
        {
            var location = await _context.Locations
                .Include(l => l.Category)
                .FirstOrDefaultAsync(l => l.LocationId == locationId);

            if (location == null)
            {
                return NotFound();
            }

            var narrations = await _context.Narrations
                .Include(n => n.Language)
                .Where(n => n.LocationId == locationId && n.IsActive)
                .OrderBy(n => n.Language.DisplayOrder)
                .ToListAsync();

            ViewBag.Location = location; // THÊM DÒNG NÀY
            return View(narrations);
        }

        // GET: Narration/Create/5
        public async Task<IActionResult> Create(int locationId)
        {
            Console.WriteLine($"Create GET called with locationId: {locationId}");

            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy địa điểm";
                return RedirectToAction("Index", "Location");
            }

            ViewBag.Location = location;

            var languages = await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();
            ViewBag.Languages = new SelectList(languages, "LanguageId", "Name");

            var narration = new Narration
            {
                LocationId = locationId,
                IsActive = true,
                Version = 1
            };

            return View(narration);
        }

        // POST: Narration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Narration narration, IFormFile? audioFile)
        {
            Console.WriteLine("===== CREATE POST =====");
            Console.WriteLine($"LocationId: {narration.LocationId}");
            Console.WriteLine($"LanguageId: {narration.LanguageId}");
            Console.WriteLine($"Title: {narration.Title}");

            // Xóa validation không cần thiết
            ModelState.Remove("Location");
            ModelState.Remove("Language");

            // Custom validation
            if (narration.LocationId <= 0)
            {
                ModelState.AddModelError("LocationId", "Vui lòng chọn địa điểm");
            }

            if (narration.LanguageId <= 0)
            {
                ModelState.AddModelError("LanguageId", "Vui lòng chọn ngôn ngữ");
            }

            if (string.IsNullOrWhiteSpace(narration.Title))
            {
                ModelState.AddModelError("Title", "Tiêu đề không được để trống");
            }

            // Nếu có lỗi, load lại form
            if (!ModelState.IsValid)
            {
                var location = await _context.Locations.FindAsync(narration.LocationId);
                ViewBag.Location = location;

                var languages = await _context.Languages
                    .Where(l => l.IsActive)
                    .OrderBy(l => l.DisplayOrder)
                    .ToListAsync();
                ViewBag.Languages = new SelectList(languages, "LanguageId", "Name");

                return View(narration);
            }

            try
            {
                // Kiểm tra trùng lặp
                var exists = await _context.Narrations
                    .AnyAsync(n => n.LocationId == narration.LocationId
                                && n.LanguageId == narration.LanguageId);

                if (exists)
                {
                    ModelState.AddModelError("LanguageId", "Bài thuyết minh cho ngôn ngữ này đã tồn tại");

                    var location = await _context.Locations.FindAsync(narration.LocationId);
                    ViewBag.Location = location;

                    var languages = await _context.Languages.ToListAsync();
                    ViewBag.Languages = new SelectList(languages, "LanguageId", "Name");

                    return View(narration);
                }

                // Xử lý IsDefault
                if (narration.IsDefault)
                {
                    var defaultNarrations = await _context.Narrations
                        .Where(n => n.LocationId == narration.LocationId && n.IsDefault)
                        .ToListAsync();
                    foreach (var n in defaultNarrations)
                    {
                        n.IsDefault = false;
                    }
                }

                // Xử lý upload file
                if (audioFile != null && audioFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/audio");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(audioFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await audioFile.CopyToAsync(stream);
                    }

                    narration.AudioUrl = "/uploads/audio/" + uniqueFileName;
                }

                // Set các giá trị mặc định
                narration.CreatedDate = DateTime.Now;
                narration.Version = 1;

                _context.Add(narration);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thêm bài thuyết minh thành công!";
                return RedirectToAction("ByLocation", new { locationId = narration.LocationId });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);

                var location = await _context.Locations.FindAsync(narration.LocationId);
                ViewBag.Location = location;

                var languages = await _context.Languages.ToListAsync();
                ViewBag.Languages = new SelectList(languages, "LanguageId", "Name");

                return View(narration);
            }
        }

        // GET: Narration/TestCreate
        public async Task<IActionResult> TestCreate(int locationId = 1, int languageId = 1)
        {
            var narration = new Narration
            {
                LocationId = locationId,
                LanguageId = languageId,
                Title = "Test " + DateTime.Now.ToString(),
                Content = "Nội dung test",
                IsActive = true
            };

            try
            {
                _context.Narrations.Add(narration);
                await _context.SaveChangesAsync();
                return Content($"OK - Đã tạo với ID: {narration.NarrationId}");
            }
            catch (Exception ex)
            {
                return Content($"Lỗi: {ex.Message}");
            }
        }

        // GET: Narration/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var narration = await _context.Narrations
                .Include(n => n.Location)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.NarrationId == id);

            if (narration == null)
            {
                return NotFound();
            }

            ViewBag.Location = narration.Location;
            ViewBag.LanguageName = narration.Language?.Name ?? "Không xác định";

            return View(narration);
        }

        // POST: Narration/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Narration narration, IFormFile? audioFile, bool removeAudio)
        {
            if (id != narration.NarrationId)
            {
                return NotFound();
            }

            ModelState.Remove("Location");
            ModelState.Remove("Language");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingNarration = await _context.Narrations.FindAsync(id);
                    if (existingNarration == null)
                    {
                        return NotFound();
                    }

                    // Xử lý xóa audio
                    if (removeAudio && !string.IsNullOrEmpty(existingNarration.AudioUrl))
                    {
                        var oldAudioPath = Path.Combine(_hostEnvironment.WebRootPath, existingNarration.AudioUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldAudioPath))
                        {
                            System.IO.File.Delete(oldAudioPath);
                        }
                        existingNarration.AudioUrl = null;
                    }

                    // Xử lý upload audio mới
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        // Xóa audio cũ nếu có
                        if (!string.IsNullOrEmpty(existingNarration.AudioUrl))
                        {
                            var oldAudioPath = Path.Combine(_hostEnvironment.WebRootPath, existingNarration.AudioUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldAudioPath))
                            {
                                System.IO.File.Delete(oldAudioPath);
                            }
                        }

                        // Upload mới
                        var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/audio");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(audioFile.FileName);
                        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await audioFile.CopyToAsync(stream);
                        }

                        existingNarration.AudioUrl = "/uploads/audio/" + uniqueFileName;
                    }

                    // Cập nhật các trường
                    existingNarration.Title = narration.Title;
                    existingNarration.Content = narration.Content;
                    existingNarration.Duration = narration.Duration;
                    existingNarration.IsDefault = narration.IsDefault;
                    existingNarration.IsActive = narration.IsActive;
                    existingNarration.UpdatedDate = DateTime.Now;

                    // Xử lý IsDefault
                    if (narration.IsDefault)
                    {
                        var defaultNarrations = await _context.Narrations
                            .Where(n => n.LocationId == narration.LocationId
                                     && n.IsDefault
                                     && n.NarrationId != narration.NarrationId)
                            .ToListAsync();
                        foreach (var n in defaultNarrations)
                        {
                            n.IsDefault = false;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thành công!";
                    return RedirectToAction("ByLocation", new { locationId = narration.LocationId });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NarrationExists(narration.NarrationId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            // Nếu có lỗi, load lại form
            var location = await _context.Locations.FindAsync(narration.LocationId);
            ViewBag.Location = location;
            var language = await _context.Languages.FindAsync(narration.LanguageId);
            ViewBag.LanguageName = language?.Name ?? "Không xác định";

            return View(narration);
        }

        // POST: Narration/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var narration = await _context.Narrations.FindAsync(id);
            if (narration != null)
            {
                // Xóa file audio nếu có
                if (!string.IsNullOrEmpty(narration.AudioUrl))
                {
                    DeleteFile(narration.AudioUrl);
                }

                _context.Narrations.Remove(narration);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa thành công!";
            }

            return RedirectToAction("ByLocation", new { locationId = narration?.LocationId });
        }

        // Các phương thức helper
        private async Task<string> UploadAudio(IFormFile audioFile)
        {
            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/audio");
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(audioFile.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await audioFile.CopyToAsync(stream);
            }

            return "/uploads/audio/" + uniqueFileName;
        }

        private void DeleteFile(string fileUrl)
        {
            if (!string.IsNullOrEmpty(fileUrl))
            {
                var filePath = Path.Combine(_hostEnvironment.WebRootPath, fileUrl.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }

        private bool NarrationExists(int id)
        {
            return _context.Narrations.Any(e => e.NarrationId == id);
        }
    }
}