using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Services;

namespace PLTour.Vendor.Controllers
{
    [Authorize(AuthenticationSchemes = "VendorAuth")]
    public class VendorImageController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IWebHostEnvironment _hostEnvironment;

        public VendorImageController(PLTourDbContext context, IWebHostEnvironment hostEnvironment, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _hostEnvironment = hostEnvironment;
            _cloudinaryService = cloudinaryService;
        }

        private int GetVendorId()
        {
            return int.Parse(User.FindFirst("VendorId")?.Value ?? "0");
        }

        // Thư viện ảnh
        public async Task<IActionResult> Index()
        {
            var vendorId = GetVendorId();
            var images = await _context.VendorImages
                .Where(i => i.VendorId == vendorId)
                .OrderBy(i => i.DisplayOrder)
                .ToListAsync();
            return View(images);
        }

        // Thêm ảnh - ĐÃ SỬA UPLOAD QUA API
        [HttpPost]
        public async Task<IActionResult> AddImage(IFormFile imageFile, string title, string imageType)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh";
                return RedirectToAction(nameof(Index));
            }

            var vendorId = GetVendorId();


            // Upload ảnh qua cloudinary 
            var imageUrl = await _cloudinaryService.UploadImageAsync(imageFile, "vendor-galleries");

            var vendorImage = new VendorImage
            {
                VendorId = vendorId,
                ImageUrl = imageUrl,
                Title = title,
                ImageType = imageType,
                DisplayOrder = await _context.VendorImages.CountAsync(i => i.VendorId == vendorId),
                CreatedDate = DateTime.UtcNow
            };

            _context.VendorImages.Add(vendorImage);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Thêm ảnh thành công!";
            return RedirectToAction(nameof(Index));
        }


        // Xóa ảnh
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id)
        {
            var vendorId = GetVendorId();
            var image = await _context.VendorImages
                .FirstOrDefaultAsync(i => i.ImageId == id && i.VendorId == vendorId);

            if (image != null)
            {
                _context.VendorImages.Remove(image);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa ảnh thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // Set ảnh làm logo
        [HttpPost]
        public async Task<IActionResult> SetAsLogo(int id)
        {
            var vendorId = GetVendorId();
            var image = await _context.VendorImages
                .FirstOrDefaultAsync(i => i.ImageId == id && i.VendorId == vendorId);

            if (image != null)
            {
                var vendor = await _context.Vendors.FindAsync(vendorId);
                vendor.LogoUrl = image.ImageUrl;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã đặt làm logo!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}