using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PLTour.Admin.Services;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using System.Text.Json;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class NarrationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<NarrationController> _logger;
        private readonly HttpClient _httpClient;
        public NarrationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ILogger<NarrationController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _httpClient = new HttpClient();
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

            ViewBag.Location = location;
            return View(narrations);
        }

        // GET: Narration/Create/5
        public async Task<IActionResult> Create(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
            {
                return NotFound();
            }

            ViewBag.Location = location;
            ViewBag.Languages = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Languages.Where(l => l.IsActive).ToListAsync(),
                "LanguageId", "Name");

            return View(new Narration { LocationId = locationId, IsActive = true, Version = 1 });
        }

        // POST: Narration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Narration narration, IFormFile? audioFile)
        {
            // Remove validation errors for navigation properties
            if (narration.Location != null) ModelState.Remove("Location");
            if (narration.Language != null) ModelState.Remove("Language");

            if (ModelState.IsValid)
            {
                try
                {
                    // Check for duplicate language
                    var exists = await _context.Narrations
                        .AnyAsync(n => n.LocationId == narration.LocationId
                                    && n.LanguageId == narration.LanguageId);

                    if (exists)
                    {
                        ModelState.AddModelError("LanguageId", "Bài thuyết minh cho ngôn ngữ này đã tồn tại");
                        ViewBag.Location = await _context.Locations.FindAsync(narration.LocationId);
                        ViewBag.Languages = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                            await _context.Languages.Where(l => l.IsActive).ToListAsync(),
                            "LanguageId", "Name");
                        return View(narration);
                    }

                    // Handle audio upload qua API
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(audioFile.OpenReadStream()), "file", audioFile.FileName);

                            var response = await _httpClient.PostAsync("https://localhost:7291/api/upload/audio?folder=audio", content);
                            var responseJson = await response.Content.ReadAsStringAsync();

                            using (var doc = JsonDocument.Parse(responseJson))
                            {
                                var url = doc.RootElement.GetProperty("url").GetString();
                                narration.AudioUrl = url;
                            }
                        }
                    }

                    // Handle default language
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

                    narration.CreatedDate = DateTime.UtcNow;
                    narration.Version = 1;

                    _context.Add(narration);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm bài thuyết minh thành công!";
                    return RedirectToAction("ByLocation", new { locationId = narration.LocationId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            ViewBag.Location = await _context.Locations.FindAsync(narration.LocationId);
            ViewBag.Languages = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Languages.Where(l => l.IsActive).ToListAsync(),
                "LanguageId", "Name");
            return View(narration);
        }

        // GET: Narration/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var narration = await _context.Narrations
                .Include(n => n.Location)
                .Include(n => n.Language)
                .FirstOrDefaultAsync(n => n.NarrationId == id);

            if (narration == null) return NotFound();

            ViewBag.Location = narration.Location;
            ViewBag.LanguageName = narration.Language?.Name ?? "Không xác định";  // ✅ QUAN TRỌNG

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

                    // Handle audio removal
                    if (removeAudio && !string.IsNullOrEmpty(existingNarration.AudioUrl))
                    {
                        var oldAudioPath = Path.Combine(_hostEnvironment.WebRootPath, existingNarration.AudioUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldAudioPath))
                        {
                            System.IO.File.Delete(oldAudioPath);
                        }
                        existingNarration.AudioUrl = null;
                    }

                    // Handle audio upload qua API
                    if (audioFile != null && audioFile.Length > 0)
                    {
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(audioFile.OpenReadStream()), "file", audioFile.FileName);

                            var response = await _httpClient.PostAsync("https://localhost:7291/api/upload/audio?folder=audio", content);
                            var responseJson = await response.Content.ReadAsStringAsync();

                            using (var doc = JsonDocument.Parse(responseJson))
                            {
                                var url = doc.RootElement.GetProperty("url").GetString();
                                existingNarration.AudioUrl = url;
                            }
                        }
                    }

                    // Handle default language
                    if (narration.IsDefault && !existingNarration.IsDefault)
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

                    // Update fields
                    existingNarration.Title = narration.Title;
                    existingNarration.Content = narration.Content;
                    existingNarration.Duration = narration.Duration;
                    existingNarration.IsDefault = narration.IsDefault;
                    existingNarration.IsActive = narration.IsActive;
                    existingNarration.UpdatedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật bài thuyết minh thành công!";
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

            ViewBag.Location = await _context.Locations.FindAsync(narration.LocationId);
            ViewBag.Languages = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _context.Languages.Where(l => l.IsActive).ToListAsync(),
                "LanguageId", "Name", narration.LanguageId);
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
                if (!string.IsNullOrEmpty(narration.AudioUrl))
                {
                    var audioPath = Path.Combine(_hostEnvironment.WebRootPath, narration.AudioUrl.TrimStart('/'));
                    if (System.IO.File.Exists(audioPath))
                    {
                        System.IO.File.Delete(audioPath);
                    }
                }

                _context.Narrations.Remove(narration);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa bài thuyết minh thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        //ACTION: Dịch tự động bằng AI 
        // POST: Narration/AutoTranslate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AutoTranslate(int locationId, int sourceLanguageId)
        {
            try
            {
                _logger.LogInformation($"AutoTranslate called: locationId={locationId}, sourceLanguageId={sourceLanguageId}");

                // Lấy nội dung gốc
                var sourceNarration = await _context.Narrations
                    .Include(n => n.Language)
                    .FirstOrDefaultAsync(n => n.LocationId == locationId && n.LanguageId == sourceLanguageId);

                if (sourceNarration == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bài thuyết minh gốc. Vui lòng lưu trước." });
                }

                if (string.IsNullOrWhiteSpace(sourceNarration.Content))
                {
                    return Json(new { success = false, message = "Nội dung gốc đang trống. Vui lòng nhập nội dung trước khi dịch." });
                }

                // Lấy danh sách ngôn ngữ cần dịch
                var targetLanguages = await _context.Languages
                    .Where(l => l.IsActive && l.LanguageId != sourceLanguageId)
                    .ToListAsync();

                if (!targetLanguages.Any())
                {
                    return Json(new { success = false, message = "Không có ngôn ngữ đích nào để dịch" });
                }

                var translationService = HttpContext.RequestServices.GetRequiredService<ITranslationService>();

                // Dịch tiêu đề
                var titleTranslations = await translationService.TranslateToAllLanguages(sourceNarration.Title);

                // Dịch nội dung
                var contentTranslations = await translationService.TranslateToAllLanguages(sourceNarration.Content);

                var createdCount = 0;
                var updatedCount = 0;

                foreach (var target in targetLanguages)
                {
                    var translatedTitle = titleTranslations.ContainsKey(target.Code) ? titleTranslations[target.Code] : sourceNarration.Title;
                    var translatedContent = contentTranslations.ContainsKey(target.Code) ? contentTranslations[target.Code] : sourceNarration.Content;

                    var existing = await _context.Narrations
                        .FirstOrDefaultAsync(n => n.LocationId == locationId && n.LanguageId == target.LanguageId);

                    if (existing == null)
                    {
                        // Tạo mới bản dịch
                        var newNarration = new Narration
                        {
                            LocationId = locationId,
                            LanguageId = target.LanguageId,
                            Title = translatedTitle,
                            Content = translatedContent,
                            Duration = sourceNarration.Duration,
                            IsDefault = false,
                            IsActive = true,
                            Version = 1,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Narrations.Add(newNarration);
                        createdCount++;
                    }
                    else if (string.IsNullOrEmpty(existing.Content) || existing.Content == sourceNarration.Content)
                    {
                        // Cập nhật cả tiêu đề và nội dung
                        existing.Title = translatedTitle;
                        existing.Content = translatedContent;
                        existing.UpdatedDate = DateTime.UtcNow;
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                var message = $"Đã tạo {createdCount} bản dịch mới, cập nhật {updatedCount} bản (bao gồm tiêu đề và nội dung)";
                return Json(new { success = true, message = message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi dịch tự động");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        private bool NarrationExists(int id)
        {
            return _context.Narrations.Any(e => e.NarrationId == id);
        }
    }
}