using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorProductController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VendorProductController(PLTourDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            var vendorId = GetVendorId();
            product.VendorId = vendorId;

            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/products");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    product.ImageUrl = "/uploads/products/" + uniqueFileName;
                }

                product.CreatedDate = DateTime.Now;
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm món ăn thành công!";
                return RedirectToAction(nameof(Index));
            }
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
                if (imageFile != null && imageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/products");
                    Directory.CreateDirectory(uploadsFolder);

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(imageFile.FileName);
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }
                    existingProduct.ImageUrl = "/uploads/products/" + uniqueFileName;
                }

                existingProduct.Name = product.Name;
                existingProduct.Description = product.Description;
                existingProduct.Price = product.Price;
                existingProduct.IsAvailable = product.IsAvailable;
                existingProduct.StockQuantity = product.StockQuantity;
                existingProduct.UpdatedDate = DateTime.Now;

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