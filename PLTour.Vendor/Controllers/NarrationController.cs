using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Vendor.Controllers;

[Authorize(AuthenticationSchemes = "VendorAuth")]
public class NarrationController : Controller
{
    private readonly PLTourDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public NarrationController(PLTourDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    private int GetVendorId() => int.TryParse(User.FindFirst("VendorId")?.Value, out var id) ? id : 0;

    [HttpGet]
    public async Task<IActionResult> Create(int locationId)
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return Redirect("/vendor-login/login");

        var store = await _context.Set<VendorStore>()
            .Include(s => s.Location)
            .FirstOrDefaultAsync(s => s.LocationId == locationId && s.VendorId == vendorId);

        if (store == null) return NotFound();

        ViewBag.Location = store.Location;
        ViewBag.Languages = new SelectList(await _context.Languages.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToListAsync(), "LanguageId", "Name");

        return View(new Narration
        {
            LocationId = locationId,
            IsActive = true,
            IsDefault = false,
            Version = 1,
            Title = string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Narration narration, IFormFile? audioFile)
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return Redirect("/vendor-login/login");

        var store = await _context.Set<VendorStore>()
            .FirstOrDefaultAsync(s => s.LocationId == narration.LocationId && s.VendorId == vendorId);
        if (store == null) return NotFound();

        ModelState.Remove("Location");
        ModelState.Remove("Language");

        if (narration.LocationId <= 0)
            ModelState.AddModelError(nameof(narration.LocationId), "Vui lòng chọn địa điểm.");

        if (narration.LanguageId <= 0)
            ModelState.AddModelError(nameof(narration.LanguageId), "Vui lòng chọn ngôn ngữ.");

        if (!ModelState.IsValid)
        {
            ViewBag.Location = store.Location;
            ViewBag.Languages = new SelectList(await _context.Languages.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToListAsync(), "LanguageId", "Name", narration.LanguageId);
            return View(narration);
        }

        var exists = await _context.Narrations.AnyAsync(n => n.LocationId == narration.LocationId && n.LanguageId == narration.LanguageId);
        if (exists)
        {
            ModelState.AddModelError(nameof(narration.LanguageId), "Bài thuyết minh cho ngôn ngữ này đã tồn tại.");
            ViewBag.Location = store.Location;
            ViewBag.Languages = new SelectList(await _context.Languages.Where(l => l.IsActive).OrderBy(l => l.DisplayOrder).ToListAsync(), "LanguageId", "Name", narration.LanguageId);
            return View(narration);
        }

        if (audioFile != null && audioFile.Length > 0)
        {
            narration.AudioUrl = await _cloudinaryService.UploadAudioAsync(audioFile, "audio");
        }

        narration.CreatedDate = DateTime.UtcNow;
        narration.Version = 1;
        narration.IsActive = true;

        _context.Narrations.Add(narration);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Thêm thuyết minh thành công!";
        return RedirectToAction("Details", "VendorStore", new { id = store.StoreId });
    }
}
