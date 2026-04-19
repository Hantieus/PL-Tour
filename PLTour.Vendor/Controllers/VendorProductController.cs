using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using System.Text.Json;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorProductController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly HttpClient _httpClient;

        public VendorProductController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _httpClient = new HttpClient();
        }

        private int GetVendorId()
        {
            return int.Parse(User.FindFirst("VendorId")?.Value ?? "0");
        }

        // Danh sách món ăn
        public async Task<IActionResult> Index()
        {
            var vendorId = GetVendorId();
            var products = await _context.Products
                .Where(p => p.VendorId == vendorId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(products);
        }

        // Thêm món ăn
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: VendorProduct/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            var vendorId = GetVendorId();

            // Xóa validation không cần thiết
            ModelState.Remove("Vendor");
            ModelState.Remove("Category");

            product.VendorId = vendorId;

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload ảnh qua API
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(imageFile.OpenReadStream()), "file", imageFile.FileName);

                            var response = await _httpClient.PostAsync("https://localhost:7291/api/upload/image?folder=products", content);
                            var responseJson = await response.Content.ReadAsStringAsync();
                            using (var doc = JsonDocument.Parse(responseJson))
                            {
                                var url = doc.RootElement.GetProperty("url").GetString();
                                product.ImageUrl = url;
                            }
                        }
                    }

                    product.CreatedDate = DateTime.UtcNow;
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Thêm món ăn thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            // Nếu có lỗi, trả về form
            return View(product);
        }

        // Sửa món ăn
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var vendorId = GetVendorId();
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.VendorId == vendorId);

            if (product == null) return NotFound();
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? imageFile)
        {
            var vendorId = GetVendorId();
            var existingProduct = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.VendorId == vendorId);

            if (existingProduct == null) return NotFound();

            if (ModelState.IsValid)
            {
                // Upload ảnh qua API
                if (imageFile != null && imageFile.Length > 0)
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        content.Add(new StreamContent(imageFile.OpenReadStream()), "file", imageFile.FileName);

                        var response = await _httpClient.PostAsync("https://localhost:7291/api/upload/image?folder=products", content);
                        var responseJson = await response.Content.ReadAsStringAsync();
                        using (var doc = JsonDocument.Parse(responseJson))
                        {
                            var url = doc.RootElement.GetProperty("url").GetString();
                            existingProduct.ImageUrl = url;
                        }
                    }
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.IsAvailable = product.IsAvailable;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.UpdatedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật món ăn thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(product);
        }

        // Xóa món ăn
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var vendorId = GetVendorId();
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.ProductId == id && p.VendorId == vendorId);

            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa món ăn thành công!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}