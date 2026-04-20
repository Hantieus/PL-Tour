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
            return int.Parse(User.FindFirst("VendorId")?.Value ?? "0");
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

            ViewBag.ProductCount = await _context.Products.CountAsync(p => p.VendorId == vendorId);
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
        public async Task<IActionResult> EditProfile(PLTour.Shared.Models.Entities.Vendor vendor, IFormFile? logoFile)
        {
            var vendorId = GetVendorId();
            var existingVendor = await _context.Vendors.FindAsync(vendorId);

            if (existingVendor == null) return NotFound();

            // Upload logo mới qua cloudinary 
            if (logoFile != null && logoFile.Length > 0)
            {
                // Xóa logo cũ
                if (!string.IsNullOrEmpty(existingVendor.LogoUrl))
                {
                    var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingVendor.LogoUrl);
                    if (!string.IsNullOrEmpty(publicId))
                        await _cloudinaryService.DeleteFileAsync(publicId);
                }

                // Upload logo mới
                var logoUrl = await _cloudinaryService.UploadImageAsync(logoFile, "vendors");
                existingVendor.LogoUrl = logoUrl;
            }

            existingVendor.ShopName = vendor.ShopName;
            existingVendor.Description = vendor.Description;
            existingVendor.Address = vendor.Address;
            existingVendor.Phone = vendor.Phone;
            existingVendor.Latitude = vendor.Latitude;
            existingVendor.Longitude = vendor.Longitude;
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