using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PLTour.API.Models.DbContext;
using PLTour.API.Services;
using PLTour.Admin.Services;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class NarrationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ILogger<NarrationController> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IConfiguration _configuration;
        private readonly ITranslationService _translationService;
        private readonly ITtsService _ttsService;

        public NarrationController(
            PLTourDbContext context,
            IWebHostEnvironment hostEnvironment,
            ILogger<NarrationController> logger,
            ICloudinaryService cloudinaryService,
            IConfiguration configuration,
            ITranslationService translationService,
            ITtsService ttsService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _logger = logger;
            _cloudinaryService = cloudinaryService;
            _configuration = configuration;
            _translationService = translationService;
            _ttsService = ttsService;
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
            ViewBag.HasNarrations = narrations.Any();
            ViewBag.Languages = await _context.Languages.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToListAsync();
            return View(narrations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertLocationNarration(int locationId, List<int> languageIds, string title, string content, int duration, bool isDefault)
        {
            var location = await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == locationId);
            if (location == null) return NotFound();

            if (languageIds == null || !languageIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ngôn ngữ.";
                return RedirectToAction(nameof(ByLocation), new { locationId });
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Title và Content không được rỗng.";
                return RedirectToAction(nameof(ByLocation), new { locationId });
            }

            try
            {
                var translatedTitles = await _translationService.TranslateToAllLanguages(title, "vi");
                var translatedContents = await _translationService.TranslateToAllLanguages(content, "vi");
                var savedCount = 0;

                foreach (var languageId in languageIds.Distinct())
                {
                    var language = await _context.Languages.FirstOrDefaultAsync(l => l.LanguageId == languageId && l.IsActive);
                    if (language == null) continue;

                    var useTitle = language.Code == "vi" ? title : translatedTitles.GetValueOrDefault(language.Code, title);
                    var useContent = language.Code == "vi" ? content : translatedContents.GetValueOrDefault(language.Code, content);

                    var audioBytes = await _ttsService.GenerateAudioAsync(useContent, language.Code);
                    await using var audioStream = new MemoryStream(audioBytes);
                    var shortHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{locationId}|{language.Code}|{useTitle}|{useContent}")))[..12];
                    var audioUrl = await _cloudinaryService.UploadAudioAsync(audioStream, $"location_{locationId}_{language.Code}_{shortHash}.mp3", $"pltour/audio/locations/{locationId}");

                    var existing = await _context.Narrations
                        .FirstOrDefaultAsync(x => x.LocationId == locationId && x.LanguageId == languageId);

                    if (existing == null)
                    {
                        existing = new Narration
                        {
                            LocationId = locationId,
                            LanguageId = languageId,
                            Title = useTitle,
                            Content = useContent,
                            AudioUrl = audioUrl,
                            Duration = duration,
                            IsDefault = isDefault && language.Code == "vi",
                            IsActive = true,
                            Version = 1,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Narrations.Add(existing);
                    }
                    else
                    {
                        existing.Title = useTitle;
                        existing.Content = useContent;
                        existing.AudioUrl = audioUrl;
                        existing.Duration = duration;
                        existing.IsDefault = isDefault && language.Code == "vi";
                        existing.IsActive = true;
                        existing.Version += 1;
                        existing.UpdatedDate = DateTime.UtcNow;
                    }

                    if (existing.IsDefault)
                    {
                        var defaults = await _context.Narrations
                            .Where(x => x.LocationId == locationId && x.IsDefault && x.LanguageId != languageId)
                            .ToListAsync();
                        foreach (var item in defaults)
                            item.IsDefault = false;
                    }

                    savedCount++;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo/cập nhật narration cho {savedCount} ngôn ngữ.";
                return RedirectToAction(nameof(ByLocation), new { locationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu narration location. LocationId={LocationId}", locationId);
                TempData["ErrorMessage"] = $"Lỗi lưu narration: {ex.Message}";
                return RedirectToAction(nameof(ByLocation), new { locationId });
            }
        }

        // GET: Narration/Create/5
        public Task<IActionResult> Create(int locationId)
        {
            return Task.FromResult<IActionResult>(RedirectToAction(nameof(ByLocation), new { locationId }));
        }

        // POST: Narration/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int locationId, List<int> languageIds, string title, string content, int duration, bool isDefault)
        {
            return await UpsertLocationNarration(locationId, languageIds, title, content, duration, isDefault);
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

                    if (removeAudio && !string.IsNullOrEmpty(existingNarration.AudioUrl))
                    {
                        var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingNarration.AudioUrl);
                        if (!string.IsNullOrEmpty(publicId))
                            await _cloudinaryService.DeleteFileAsync(publicId);
                        existingNarration.AudioUrl = string.Empty;
                    }

                    if (audioFile != null && audioFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingNarration.AudioUrl))
                        {
                            var oldPublicId = _cloudinaryService.ExtractPublicIdFromUrl(existingNarration.AudioUrl);
                            if (!string.IsNullOrEmpty(oldPublicId))
                                await _cloudinaryService.DeleteFileAsync(oldPublicId);
                        }

                        var audioUrl = await _cloudinaryService.UploadAudioAsync(audioFile, $"narrations/{narration.LocationId}/{narration.LanguageId}");
                        existingNarration.AudioUrl = audioUrl;
                    }

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

        private async Task<string> GenerateNarrationAudioAsync(string title, string content, int languageId, int locationId)
        {
            var language = await _context.Languages.FirstOrDefaultAsync(l => l.LanguageId == languageId);
            if (language == null)
                throw new InvalidOperationException("Không tìm thấy ngôn ngữ");

            var audioBytes = await GetTtsAudioBytesAsync(content, language.Code);
            var publicName = $"narration_{locationId}_{language.Code}_{GetShortHash($"{title}|{content}|{language.Code}")}";
            await using var stream = new MemoryStream(audioBytes);
            return await _cloudinaryService.UploadAudioAsync(stream, publicName + ".mp3", "audio");
        }

        private async Task<byte[]> GetTtsAudioBytesAsync(string content, string langCode)
        {
            var baseUrl = _configuration["Tts:BaseUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new InvalidOperationException("Tts:BaseUrl chưa được cấu hình");

            using var client = new HttpClient();
            var response = await client.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/tts", new { text = content, langCode });
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException("Không tạo được audio từ TTS service");

            return await response.Content.ReadAsByteArrayAsync();
        }

        private static string GetShortHash(string input)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash)[..12];
        }

        private bool NarrationExists(int id)
        {
            return _context.Narrations.Any(e => e.NarrationId == id);
        }
    }
}