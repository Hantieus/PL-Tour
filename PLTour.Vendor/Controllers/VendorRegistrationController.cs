using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Vendor.ViewModels;
using PLTour.Shared.Services;


namespace PLTour.Vendor.Controllers
{
    public class VendorRegistrationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _hostEnvironment;
        public VendorRegistrationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
        }

        // GET: /vendor-registration
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive)
                .ToListAsync();
            return View();
        }

        // POST: /vendor-registration
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(VendorRegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra email đã đăng ký chưa
                    var existingVendor = await _context.Vendors
                        .FirstOrDefaultAsync(v => v.Email == model.Email);

                    if (existingVendor != null)
                    {
                        ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View(model);
                    }

                    // Xử lý upload logo qua API
                    string? logoUrl = null;
                    if (model.LogoFile != null && model.LogoFile.Length > 0)
                    {
                        logoUrl = await _cloudinaryService.UploadImageAsync(model.LogoFile, "vendors");
                    }

                    // Tạo vendor mới
                    var vendor = new PLTour.Shared.Models.Entities.Vendor
                    {
                        ShopName = model.ShopName,
                        OwnerName = model.OwnerName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Address = model.Address,
                        CategoryId = model.CategoryId,
                        Description = model.Description,
                        LogoUrl = logoUrl,
                        Latitude = model.Latitude,
                        Longitude = model.Longitude,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),  // ✅ THÊM DÒNG NÀY
                        Notes = "",
                        Status = "Pending",
                        IsActive = false,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Vendors.Add(vendor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đăng ký thành công! Admin sẽ xét duyệt trong thời gian sớm nhất.";
                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(model);
        }

        // GET: /vendor-registration/success
        public IActionResult Success()
        {
            return View();
        }
    }
}