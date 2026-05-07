using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Vendor.ViewModels;
using PLTour.Shared.Services;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorDashboardController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VendorDashboardController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
       
        }

        // Lấy VendorId từ session
        private int GetVendorId()
        {
            return int.TryParse(User.FindFirst("VendorId")?.Value, out var vendorId) ? vendorId : 0;
        }

        // Dashboard chính
        public async Task<IActionResult> Index()
        {
            var vendorId = GetVendorId();
            if (vendorId == 0)
            {
                return Redirect("/vendor-login/login");
            }

            var vendor = await _context.Vendors.FindAsync(vendorId);
            if (vendor == null) return Redirect("/vendor-login/login");

            var stores = await _context.Set<VendorStore>()
                .Where(s => s.VendorId == vendorId)
                .OrderByDescending(s => s.IsDefault)
                .ThenByDescending(s => s.CreatedDate)
                .ToListAsync();

            var storeIds = stores.Select(s => s.StoreId).ToList();

            ViewBag.Stores = stores;
            ViewBag.StoreCount = stores.Count;
            ViewBag.ProductCount = await _context.Products.CountAsync(p => p.StoreId.HasValue && storeIds.Contains(p.StoreId.Value));
            ViewBag.ImageCount = await _context.VendorImages.CountAsync(i => i.VendorId == vendorId);

            return View(vendor);
        }

        // Sửa thông tin quán
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var vendorId = GetVendorId();
            var vendor = await _context.Vendors.FindAsync(vendorId);
            return View(vendor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(PLTour.Shared.Models.Entities.Vendor vendor, IFormFile? avatarFile)
        {
            var vendorId = GetVendorId();
            var existingVendor = await _context.Vendors.FindAsync(vendorId);

            if (existingVendor == null) return NotFound();

            // Upload logo mới qua cloudinary 
            if (avatarFile != null && avatarFile.Length > 0)
            {
                // Xóa ảnh cũ
                if (!string.IsNullOrEmpty(existingVendor.AvatarUrl))
                {
                    var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingVendor.AvatarUrl);
                    if (!string.IsNullOrEmpty(publicId))
                        await _cloudinaryService.DeleteFileAsync(publicId);
                }

                // Upload ảnh mới
                var avatarUrl = await _cloudinaryService.UploadImageAsync(avatarFile, "vendors");
                existingVendor.AvatarUrl = avatarUrl;
            }

            existingVendor.BusinessName = vendor.BusinessName;
            existingVendor.ContactName = vendor.ContactName;
            existingVendor.Description = vendor.Description;
            existingVendor.Phone = vendor.Phone;
            existingVendor.AvatarUrl = existingVendor.AvatarUrl;
            existingVendor.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Index");
        }

        // Đổi mật khẩu
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var vendorId = GetVendorId();
            var vendor = await _context.Vendors.FindAsync(vendorId);

            if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, vendor.PasswordHash))
            {
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng");
                return View(model);
            }

            vendor.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            vendor.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("ChangePassword");
        }
        // log out
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("VendorAuth");
            return RedirectToAction("Login", "VendorLogin");
        }
    }
}