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
            ViewBag.Plans = new[]
            {
                new { Value = "Free", Text = "Free - Miễn phí" },
                new { Value = "Premium", Text = "Premium - Nâng cấp" }
            };
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

                    if (await _context.Vendors.AnyAsync(v => v.Phone == model.Phone))
                    {
                        ModelState.AddModelError("Phone", "Số điện thoại này đã được đăng ký.");
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View(model);
                    }

                    if (existingVendor != null)
                    {
                        ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng sử dụng email khác.");
                        ViewBag.Categories = await _context.Categories.ToListAsync();
                        return View(model);
                    }

                    // Xử lý upload logo qua API
                    string? avatarUrl = null;
                    if (model.AvatarFile != null && model.AvatarFile.Length > 0)
                    {
                        avatarUrl = await _cloudinaryService.UploadImageAsync(model.AvatarFile, "vendors");
                    }

                    // Tạo vendor mới
                    var vendor = new PLTour.Shared.Models.Entities.Vendor
                    {
                        BusinessName = model.BusinessName,
                        ContactName = model.ContactName,
                        Email = model.Email,
                        Phone = model.Phone,
                        CategoryId = model.CategoryId,
                        Description = model.Description,
                        AvatarUrl = avatarUrl,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        Notes = "",
                        Status = "Pending",
                        Plan = string.IsNullOrWhiteSpace(model.Plan) ? "Free" : model.Plan,
                        IsActive = false,
                        CreatedDate = DateTime.UtcNow,
                        UpdatedDate = DateTime.UtcNow
                    };

                    _context.Vendors.Add(vendor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đăng ký thành công! Admin sẽ xét duyệt trong thời gian sớm nhất.";
                    return RedirectToAction("Success");
                }
                catch (Exception ex)
                {
                    var errorMsg = ex.Message;
                    if (ex.InnerException != null)
                        errorMsg += " | Inner: " + ex.InnerException.Message;
                    ModelState.AddModelError("", "Có lỗi xảy ra: " + errorMsg);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.Plans = new[]
            {
                new { Value = "Free", Text = "Free - Miễn phí" },
                new { Value = "Premium", Text = "Premium - Nâng cấp" }
            };
            return View(model);
        }

        // GET: /vendor-registration/success
        public IActionResult Success()
        {
            return View();
        }
    }
}