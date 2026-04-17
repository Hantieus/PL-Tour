using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Vendor.ViewModels;
using System.Net.Http;
using System.Text.Json;


namespace PLTour.Vendor.Controllers
{
    public class VendorRegistrationController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly HttpClient _httpClient;

        public VendorRegistrationController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _httpClient = new HttpClient();
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
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(model.LogoFile.OpenReadStream()), "file", model.LogoFile.FileName);

                            var response = await _httpClient.PostAsync("...", content);
                            var responseJson = await response.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(responseJson))
                            {
                                logoUrl = doc.RootElement.GetProperty("url").GetString();
                            }
                        }
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
                        CreatedDate = DateTime.Now
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