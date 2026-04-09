using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Vendor.ViewModels;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorDashboardController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VendorDashboardController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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

            // Upload logo mới
            if (logoFile != null && logoFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/vendors");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(logoFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await logoFile.CopyToAsync(stream);
                }

                existingVendor.LogoUrl = "/uploads/vendors/" + uniqueFileName;
            }

            existingVendor.ShopName = vendor.ShopName;
            existingVendor.Description = vendor.Description;
            existingVendor.Address = vendor.Address;
            existingVendor.Phone = vendor.Phone;
            existingVendor.Latitude = vendor.Latitude;
            existingVendor.Longitude = vendor.Longitude;
            existingVendor.UpdatedDate = DateTime.Now;

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
            vendor.UpdatedDate = DateTime.Now;
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