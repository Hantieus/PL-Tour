using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PLTour.API.Models.DbContext;
using PLTour.API.Services;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;
using PLTour.Admin.Services;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class TourController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ITranslationService _translationService;
        private readonly ITtsService _ttsService;
        private readonly ILogger<TourController> _logger;

        public TourController(
            PLTourDbContext context,
            IWebHostEnvironment hostEnvironment,
            ICloudinaryService cloudinaryService,
            ITranslationService translationService,
            ITtsService ttsService,
            ILogger<TourController> logger)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
            _translationService = translationService;
            _ttsService = ttsService;
            _logger = logger;
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
            selectedLocationIds ??= Array.Empty<int>();
            selectedLocationIds = selectedLocationIds.Distinct().ToArray();

            if (!selectedLocationIds.Any())
            {
                ModelState.AddModelError(nameof(selectedLocationIds), "Vui lòng chọn ít nhất một địa điểm cho tour.");
            }

            if (selectedLocationIds.Any())
            {
                var validLocationCount = await _context.Locations.CountAsync(l => l.IsActive && selectedLocationIds.Contains(l.LocationId));
                if (validLocationCount != selectedLocationIds.Length)
                {
                    ModelState.AddModelError(nameof(selectedLocationIds), "Có địa điểm không hợp lệ hoặc đã bị khóa.");
                }
            }

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "tours");
                    tour.ImageUrl = imageUrl;
                }

                tour.CreatedDate = DateTime.UtcNow;
                _context.Tours.Add(tour);
                await _context.SaveChangesAsync();

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

                TempData["SuccessMessage"] = "Thêm tour thành công!";
                return RedirectToAction(nameof(Index));
            }

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

            var oldIntroText = existingTour.IntroText;

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

                if (!string.Equals(oldIntroText?.Trim(), tour.IntroText?.Trim(), StringComparison.Ordinal))
                {
                    TempData["SuccessMessage"] = "Cập nhật tour thành công! IntroText đã thay đổi, bạn nên tạo lại audio trong mục Quản lý audio.";
                }
                else
                {
                    TempData["SuccessMessage"] = "Cập nhật tour thành công!";
                }

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

        // GET: Tour/Audio/5
        public async Task<IActionResult> Audio(int id)
        {
            var tour = await _context.Tours
                .Include(t => t.TourNarrations)
                    .ThenInclude(a => a.Language)
                .FirstOrDefaultAsync(t => t.TourId == id);

            if (tour == null) return NotFound();

            var languages = await _context.Languages
                .Where(l => l.IsActive)
                .OrderBy(l => l.DisplayOrder)
                .ToListAsync();

            ViewBag.Languages = languages;
            return View(tour);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpsertTourNarration(int tourId, List<int> languageIds, string title, string content, int duration, bool isDefault)
        {
            var tour = await _context.Tours.FirstOrDefaultAsync(t => t.TourId == tourId);
            if (tour == null) return NotFound();

            if (languageIds == null || !languageIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 ngôn ngữ.";
                return RedirectToAction(nameof(Audio), new { id = tourId });
            }

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Title và Content không được rỗng.";
                return RedirectToAction(nameof(Audio), new { id = tourId });
            }

            try
            {
                _logger.LogInformation("UpsertTourNarration started. TourId={TourId}, Languages={Count}", tourId, languageIds.Count);
                var translatedTitles = await _translationService.TranslateToAllLanguages(title, "vi");
                var translatedContents = await _translationService.TranslateToAllLanguages(content, "vi");
                var savedCount = 0;

                foreach (var languageId in languageIds.Distinct())
                {
                    var language = await _context.Languages.FirstOrDefaultAsync(l => l.LanguageId == languageId && l.IsActive);
                    if (language == null)
                    {
                        _logger.LogWarning("Language not found. LanguageId={LanguageId}", languageId);
                        continue;
                    }

                    var useTitle = language.Code == "vi" ? title : translatedTitles.GetValueOrDefault(language.Code, title);
                    var useContent = language.Code == "vi" ? content : translatedContents.GetValueOrDefault(language.Code, content);

                    _logger.LogInformation("Generating audio for TourId={TourId}, Lang={Lang}", tourId, language.Code);
                    var audioBytes = await _ttsService.GenerateAudioAsync(useContent, language.Code);
                    await using var audioStream = new MemoryStream(audioBytes);
                    var shortHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{tourId}|{language.Code}|{useTitle}|{useContent}")))[..12];
                    var audioUrl = await _cloudinaryService.UploadAudioAsync(audioStream, $"tour_{tourId}_{language.Code}_{shortHash}.mp3", $"pltour/audio/tours/{tourId}");

                    var existing = await _context.Set<TourNarration>()
                        .FirstOrDefaultAsync(x => x.TourId == tourId && x.LanguageId == languageId && x.IsActive);

                    if (existing == null)
                    {
                        existing = new TourNarration
                        {
                            TourId = tourId,
                            LanguageId = languageId,
                            Title = useTitle,
                            Content = useContent,
                            AudioUrl = audioUrl,
                            Duration = duration,
                            IsDefault = isDefault && language.Code == "vi",
                            Version = 1,
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        _context.Add(existing);
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
                        var defaults = await _context.Set<TourNarration>()
                            .Where(x => x.TourId == tourId && x.IsDefault && x.LanguageId != languageId)
                            .ToListAsync();
                        foreach (var item in defaults)
                            item.IsDefault = false;
                    }

                    savedCount++;
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã tạo/cập nhật narration cho {savedCount} ngôn ngữ.";
                return RedirectToAction(nameof(Audio), new { id = tourId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi lưu narration tour. TourId={TourId}", tourId);
                TempData["ErrorMessage"] = $"Lỗi lưu narration: {ex.Message}";
                return RedirectToAction(nameof(Audio), new { id = tourId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTourNarration(int id, string title, string content, int duration, bool isDefault, bool isActive, IFormFile? audioFile, bool removeAudio)
        {
            var narration = await _context.Set<TourNarration>()
                .FirstOrDefaultAsync(x => x.TourNarrationId == id);

            if (narration == null) return NotFound();

            try
            {
                if (removeAudio && !string.IsNullOrWhiteSpace(narration.AudioUrl))
                {
                    var publicId = _cloudinaryService.ExtractPublicIdFromUrl(narration.AudioUrl);
                    if (!string.IsNullOrWhiteSpace(publicId))
                        await _cloudinaryService.DeleteFileAsync(publicId);
                    narration.AudioUrl = string.Empty;
                }

                if (audioFile != null && audioFile.Length > 0)
                {
                    if (!string.IsNullOrWhiteSpace(narration.AudioUrl))
                    {
                        var publicId = _cloudinaryService.ExtractPublicIdFromUrl(narration.AudioUrl);
                        if (!string.IsNullOrWhiteSpace(publicId))
                            await _cloudinaryService.DeleteFileAsync(publicId);
                    }

                    await using var stream = audioFile.OpenReadStream();
                    var shortHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{narration.TourId}|{narration.LanguageId}|{title}|{content}|{DateTime.UtcNow.Ticks}")))[..12];
                    narration.AudioUrl = await _cloudinaryService.UploadAudioAsync(stream, $"tour_{narration.TourId}_{narration.LanguageId}_{shortHash}.mp3", $"pltour/audio/tours/{narration.TourId}");
                }

                if (isDefault && !narration.IsDefault)
                {
                    var defaults = await _context.Set<TourNarration>()
                        .Where(x => x.TourId == narration.TourId && x.IsDefault && x.TourNarrationId != narration.TourNarrationId)
                        .ToListAsync();
                    foreach (var item in defaults)
                        item.IsDefault = false;
                }

                narration.Title = title;
                narration.Content = content;
                narration.Duration = duration;
                narration.IsDefault = isDefault;
                narration.IsActive = isActive;
                narration.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật narration tour thành công!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi cập nhật narration tour. NarrationId={NarrationId}", id);
                TempData["ErrorMessage"] = $"Lỗi cập nhật narration: {ex.Message}";
            }

            return RedirectToAction(nameof(Audio), new { id = narration.TourId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTourNarration(int id)
        {
            var narration = await _context.Set<TourNarration>().FindAsync(id);
            if (narration == null) return NotFound();

            narration.IsActive = false;
            narration.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tắt narration tour.";
            return RedirectToAction(nameof(Audio), new { id = narration.TourId });
        }
    }
}