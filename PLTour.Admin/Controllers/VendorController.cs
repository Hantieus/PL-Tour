using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class VendorController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public VendorController(PLTourDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Vendor
        public async Task<IActionResult> Index(string searchString, string status = "", string plan = "", int page = 1)
        {
            int pageSize = 10;

            var query = _context.Vendors
                .Include(v => v.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(v => v.BusinessName.Contains(searchString)
                                      || v.ContactName.Contains(searchString)
                                      || v.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(v => v.Status == status);
            }

            if (!string.IsNullOrEmpty(plan))
            {
                query = query.Where(v => v.Plan == plan);
            }

            var totalItems = await query.CountAsync();
            var vendors = await query
                .OrderByDescending(v => v.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedPlan = plan;

            var statusCounts = await _context.Vendors
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key ?? "Pending", Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            ViewBag.StatusCounts = statusCounts;

            return View(vendors);
        }

        // GET: Vendor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors
                .Include(v => v.Category)
                .FirstOrDefaultAsync(v => v.VendorId == id);

            if (vendor == null) return NotFound();

            ViewBag.CategoryName = vendor.Category?.Name;
            return View(vendor);
        }

        // GET: Vendor/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            return View(vendor);
        }

        // POST: Vendor/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string status, string notes)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            var normalizedStatus = status?.Trim();
            var allowedStatuses = new[] { "Pending", "Approved", "Rejected", "Suspended" };

            if (string.IsNullOrWhiteSpace(normalizedStatus) || !allowedStatuses.Contains(normalizedStatus))
            {
                ModelState.AddModelError("status", "Trạng thái vendor không hợp lệ.");
                return View(vendor);
            }

            vendor.Status = normalizedStatus;
            vendor.Notes = notes?.Trim() ?? string.Empty;
            vendor.IsActive = normalizedStatus == "Approved";

            vendor.UpdatedDate = DateTime.UtcNow;
            vendor.ApprovedDate = normalizedStatus == "Approved" ? DateTime.UtcNow : vendor.ApprovedDate;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái vendor thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Vendor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors
                .Include(v => v.Category)
                .FirstOrDefaultAsync(v => v.VendorId == id);

            if (vendor == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentLogo = vendor.AvatarUrl;
            return View(vendor);
        }

        // POST: Vendor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VendorId,BusinessName,ContactName,Email,Phone,Address,CategoryId,Description,Status,IsActive,Latitude,Longitude,AvatarUrl")] Vendor vendor, IFormFile? logoFile)
        {
            if (id != vendor.VendorId) return NotFound();

            // Xóa validation không cần thiết
            ModelState.Remove("Category");
            ModelState.Remove("Products");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingVendor = await _context.Vendors.FindAsync(id);
                    if (existingVendor == null) return NotFound();

                    if (await _context.Vendors.AnyAsync(v => v.Email == vendor.Email && v.VendorId != id))
                    {
                        ModelState.AddModelError("Email", "Email này đã tồn tại.");
                    }

                    if (!ModelState.IsValid)
                    {
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        ViewBag.CurrentLogo = existingVendor.AvatarUrl;
                        return View(vendor);
                    }

                    if (logoFile != null && logoFile.Length > 0)
                    {
                        var uploadUrl = await _cloudinaryService.UploadImageAsync(logoFile, "vendors");
                        existingVendor.AvatarUrl = uploadUrl;
                    }

                    existingVendor.BusinessName = vendor.BusinessName;
                    existingVendor.ContactName = vendor.ContactName;

                    if (!ModelState.IsValid)
                    {
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        ViewBag.CurrentLogo = existingVendor.AvatarUrl;
                        return View(vendor);
                    }

                    if (logoFile != null && logoFile.Length > 0)
                    {
                        var uploadUrl = await _cloudinaryService.UploadImageAsync(logoFile, "vendors");
                        existingVendor.AvatarUrl = uploadUrl;
                    }

                    existingVendor.BusinessName = vendor.BusinessName;
                    existingVendor.ContactName = vendor.ContactName;
                    existingVendor.Email = vendor.Email;
                    existingVendor.Phone = vendor.Phone;
                    existingVendor.Address = vendor.Address;
                    existingVendor.CategoryId = vendor.CategoryId;
                    existingVendor.Description = vendor.Description;
                    existingVendor.Status = vendor.Status;
                    existingVendor.IsActive = vendor.IsActive;
                    existingVendor.Plan = existingVendor.Plan ?? "Free";
                    existingVendor.AvatarUrl = vendor.AvatarUrl ?? existingVendor.AvatarUrl;
                    existingVendor.Latitude = vendor.Latitude;
                    existingVendor.Longitude = vendor.Longitude;
                    existingVendor.UpdatedDate = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật vendor thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(vendor);
        }

        // POST: Vendor/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa vendor thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.VendorId == id);
        }
    }
}