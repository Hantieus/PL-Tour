using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.Vendor.ViewModels;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using System.Security.Claims;

namespace PLTour.Vendor.Controllers
{
    public class VendorLoginController : Controller
    {
        private readonly PLTourDbContext _context;

        public VendorLoginController(PLTourDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "/vendor/dashboard")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(VendorLoginViewModel model, string returnUrl = "/vendor/dashboard")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Tìm vendor theo email
            var vendor = await _context.Vendors
                .FirstOrDefaultAsync(v => v.Email == model.Email && v.IsActive && v.Status == "Approved");

            if (vendor == null)
            {
                ModelState.AddModelError("", "Email không tồn tại hoặc chưa được duyệt");
                return View(model);
            }

            // Kiểm tra mật khẩu (sử dụng BCrypt)
            if (!BCrypt.Net.BCrypt.Verify(model.Password, vendor.PasswordHash))
            {
                ModelState.AddModelError("", "Mật khẩu không đúng");
                return View(model);
            }

            // Tạo session cho vendor
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, vendor.ShopName),
                new Claim(ClaimTypes.Email, vendor.Email),
                new Claim("VendorId", vendor.VendorId.ToString()),
                new Claim("Role", "Vendor")
            };

            var claimsIdentity = new ClaimsIdentity(claims, "VendorAuth");
            await HttpContext.SignInAsync("VendorAuth", new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = model.RememberMe });

            return Redirect("/vendor/dashboard"); ;
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("VendorAuth");
            return RedirectToAction("Login");
        }
    }
}