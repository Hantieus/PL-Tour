using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;
using VendorAccount = PLTour.Shared.Models.Entities.Vendor;
using VendorStoreEntity = PLTour.Shared.Models.Entities.VendorStore;

namespace PLTour.Vendor.Controllers;

[Authorize(AuthenticationSchemes = "VendorAuth")]
public class VendorStoreController : Controller
{
    private readonly PLTourDbContext _context;
    private readonly ICloudinaryService _cloudinaryService;

    public VendorStoreController(PLTourDbContext context, ICloudinaryService cloudinaryService)
    {
        _context = context;
        _cloudinaryService = cloudinaryService;
    }

    private int GetVendorId() => int.TryParse(User.FindFirst("VendorId")?.Value, out var id) ? id : 0;

    private static bool IsPremium(VendorAccount? vendor)
        => vendor?.Plan == "Premium" && (!vendor.PlanExpiresAt.HasValue || vendor.PlanExpiresAt.Value > DateTime.UtcNow);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return Redirect("/vendor-login/login");

        var vendor = await _context.Set<VendorAccount>().FindAsync(vendorId);
        var isPremium = IsPremium(vendor);
        var storeLimit = isPremium ? 5 : 1;

        var stores = await _context.Set<VendorStoreEntity>()
            .Include(s => s.Location)
            .Where(s => s.VendorId == vendorId)
            .OrderByDescending(s => s.IsDefault)
            .ThenByDescending(s => s.CreatedDate)
            .ToListAsync();

        ViewBag.StoreLimit = storeLimit;
        ViewBag.IsPremium = isPremium;
        return View(stores);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return Redirect("/vendor-login/login");

        var vendor = await _context.Set<VendorAccount>().FindAsync(vendorId);
        var currentStoreCount = await _context.Set<VendorStoreEntity>().CountAsync(s => s.VendorId == vendorId);
        var storeLimit = IsPremium(vendor) ? 5 : 1;

        if (currentStoreCount >= storeLimit)
        {
            TempData["ErrorMessage"] = $"Gói hiện tại chỉ cho phép {storeLimit} cửa hàng. Vui lòng nâng cấp để tạo thêm.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.CurrentStoreCount = currentStoreCount;
        return View(new VendorStoreEntity());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VendorStoreEntity store, IFormFile? logoFile)
    {
        var vendorId = GetVendorId();
        if (vendorId == 0) return Redirect("/vendor-login/login");

        var vendor = await _context.Set<VendorAccount>().FindAsync(vendorId);
        var currentStoreCount = await _context.Set<VendorStoreEntity>().CountAsync(s => s.VendorId == vendorId);
        var storeLimit = IsPremium(vendor) ? 5 : 1;

        if (currentStoreCount >= storeLimit)
        {
            TempData["ErrorMessage"] = $"Gói hiện tại chỉ cho phép {storeLimit} cửa hàng. Vui lòng nâng cấp để tạo thêm.";
            return RedirectToAction(nameof(Index));
        }

        store.VendorId = vendorId;
        store.CreatedDate = DateTime.UtcNow;
        store.UpdatedDate = DateTime.UtcNow;
        store.Plan = string.IsNullOrWhiteSpace(store.Plan) ? "Free" : store.Plan;
        store.IsDefault = currentStoreCount == 0;

        if (logoFile != null && logoFile.Length > 0)
        {
            store.LogoUrl = await _cloudinaryService.UploadImageAsync(logoFile, "vendor-stores");
        }

        var location = new Location
        {
            Name = string.IsNullOrWhiteSpace(store.StoreName) ? "Cửa hàng mới" : store.StoreName,
            Description = store.Description ?? string.Empty,
            Latitude = store.Latitude ?? 0,
            Longitude = store.Longitude ?? 0,
            Address = store.Address ?? string.Empty,
            CategoryId = 2,
            ImageUrl = store.LogoUrl,
            OrderIndex = 0,
            Radius = 50,
            IsActive = store.IsActive,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Locations.Add(location);
        await _context.SaveChangesAsync();

        store.LocationId = location.LocationId;
        _context.Set<VendorStoreEntity>().Add(store);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vendorId = GetVendorId();
        var store = await _context.Set<VendorStoreEntity>()
            .Include(s => s.Location)
            .Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.StoreId == id && s.VendorId == vendorId);
        if (store?.LocationId.HasValue == true)
        {
            ViewBag.Narrations = await _context.Narrations
                .Include(n => n.Language)
                .Where(n => n.LocationId == store.LocationId.Value && n.IsActive)
                .OrderByDescending(n => n.IsDefault)
                .ThenBy(n => n.Language.DisplayOrder)
                .ToListAsync();

            ViewBag.Location = store.Location;
        }
        else
        {
            ViewBag.Narrations = new List<Narration>();
        }
        if (store == null) return NotFound();
        return View(store);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var vendorId = GetVendorId();
        var store = await _context.Set<VendorStoreEntity>().FirstOrDefaultAsync(s => s.StoreId == id && s.VendorId == vendorId);
        if (store == null) return NotFound();

        ViewBag.Locations = await _context.Locations
            .Where(l => l.CategoryId == 2 && l.IsActive)
            .OrderBy(l => l.Name)
            .ToListAsync();

        return View(store);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VendorStoreEntity store, IFormFile? logoFile)
    {
        var vendorId = GetVendorId();
        if (id != store.StoreId) return NotFound();

        var existing = await _context.Set<VendorStoreEntity>().FirstOrDefaultAsync(s => s.StoreId == id && s.VendorId == vendorId);
        if (existing == null) return NotFound();

        existing.StoreName = store.StoreName;
        existing.Description = store.Description;
        existing.Address = store.Address;
        existing.Latitude = store.Latitude;
        existing.Longitude = store.Longitude;
        existing.IsActive = store.IsActive;
        existing.UpdatedDate = DateTime.UtcNow;

        if (logoFile != null && logoFile.Length > 0)
        {
            existing.LogoUrl = await _cloudinaryService.UploadImageAsync(logoFile, "vendor-stores");
        }

        var linkedLocation = existing.LocationId.HasValue
            ? await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == existing.LocationId.Value)
            : null;
        if (linkedLocation != null)
        {
            linkedLocation.Name = existing.StoreName;
            linkedLocation.Description = existing.Description ?? string.Empty;
            linkedLocation.Address = existing.Address ?? string.Empty;
            linkedLocation.Latitude = existing.Latitude ?? linkedLocation.Latitude;
            linkedLocation.Longitude = existing.Longitude ?? linkedLocation.Longitude;
            linkedLocation.ImageUrl = existing.LogoUrl;
            linkedLocation.IsActive = existing.IsActive;
            linkedLocation.UpdatedDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var vendorId = GetVendorId();
        var store = await _context.Set<VendorStoreEntity>().FirstOrDefaultAsync(s => s.StoreId == id && s.VendorId == vendorId);
        if (store == null) return NotFound();

        _context.Set<VendorStoreEntity>().Remove(store);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id)
    {
        var vendorId = GetVendorId();
        var store = await _context.Set<VendorStoreEntity>().FirstOrDefaultAsync(s => s.StoreId == id && s.VendorId == vendorId);
        if (store == null) return NotFound();

        var stores = await _context.Set<VendorStoreEntity>().Where(s => s.VendorId == vendorId).ToListAsync();
        foreach (var s in stores)
            s.IsDefault = s.StoreId == id;

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
