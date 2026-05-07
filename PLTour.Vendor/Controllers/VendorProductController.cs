using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorProductController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VendorProductController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
        }

        private int GetVendorId()
        {
            return int.TryParse(User.FindFirst("VendorId")?.Value, out var vendorId) ? vendorId : 0;
        }

        // Danh sách món ăn
        public async Task<IActionResult> Index()
        {
            var vendorId = GetVendorId();
            var storeIds = await _context.Set<VendorStore>()
                .Where(s => s.VendorId == vendorId)
                .Select(s => s.StoreId)
                .ToListAsync();

            var products = await _context.Products
                .Include(p => p.Store)
                .Where(p => p.StoreId.HasValue && storeIds.Contains(p.StoreId.Value))
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
            return View(products);
        }

        // Thêm món ăn
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vendorId = GetVendorId();
            var stores = await _context.Set<VendorStore>()
                .Where(s => s.VendorId == vendorId)
                .OrderByDescending(s => s.CreatedDate)
                .ToListAsync();

            ViewBag.Stores = stores;
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
            ModelState.Remove("Store");

            var store = product.StoreId.HasValue
                ? await _context.Set<VendorStore>().Include(s => s.Location).FirstOrDefaultAsync(s => s.StoreId == product.StoreId.Value && s.VendorId == vendorId)
                : null;
            if (store == null)
            {
                ModelState.AddModelError(nameof(product.StoreId), "Cửa hàng không hợp lệ.");
            }
            else if (store.LocationId.HasValue)
            {
                var location = await _context.Locations.FirstOrDefaultAsync(l => l.LocationId == store.LocationId.Value);
                ViewBag.LocationName = location?.Name;
            }

            product.VendorId = vendorId;
            if (product.StoreId == null)
            {
                ModelState.AddModelError(nameof(product.StoreId), "Vui lòng chọn cửa hàng.");
            }

            var productCountForStore = product.StoreId.HasValue
                ? await _context.Products.CountAsync(p => p.StoreId == product.StoreId.Value)
                : 0;
            var storeLimit = 10;
            if (store != null && store.Plan == "Premium")
            {
                storeLimit = 100;
            }

            if (productCountForStore >= storeLimit)
            {
                ModelState.AddModelError(string.Empty, $"Cửa hàng này đã đạt giới hạn {storeLimit} sản phẩm cho gói hiện tại.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Upload ảnh qua cloudinary
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                        product.ImageUrl = imageUrl;
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
                // Upload ảnh qua cloudinary
                if (imageFile != null && imageFile.Length > 0)
                {
                    if (!string.IsNullOrEmpty(existingProduct.ImageUrl))
                    {
                        var publicId = _cloudinaryService.ExtractPublicIdFromUrl(existingProduct.ImageUrl);
                        if (!string.IsNullOrEmpty(publicId))
                            await _cloudinaryService.DeleteFileAsync(publicId);
                    }

                    var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "products");
                    existingProduct.ImageUrl = imageUrl;
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.IsAvailable = product.IsAvailable;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.StoreId = product.StoreId;
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