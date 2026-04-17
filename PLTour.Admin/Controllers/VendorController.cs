using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using System.Text.Json;

namespace PLTour.Admin.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class VendorController : Controller
    {
        private readonly PLTourDbContext _context;
        private readonly HttpClient _httpClient;

        public VendorController(PLTourDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        // GET: Vendor
        public async Task<IActionResult> Index(string searchString, string status = "", int page = 1)
        {
            int pageSize = 10;

            var query = _context.Vendors
                .Include(v => v.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(v => v.ShopName.Contains(searchString)
                                      || v.OwnerName.Contains(searchString)
                                      || v.Email.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var totalItems = await query.CountAsync();
            var vendors = await query
                .OrderByDescending(v => v.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.SearchString = searchString;
            ViewBag.SelectedStatus = status;

            var statusCounts = await _context.Vendors
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key ?? "Pending", Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

            ViewBag.StatusCounts = statusCounts;

            return View(vendors);
        }

        // GET: Vendor/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors
                .Include(v => v.Category)
                .FirstOrDefaultAsync(v => v.VendorId == id);

            if (vendor == null) return NotFound();

            ViewBag.CategoryName = vendor.Category?.Name;
            return View(vendor);
        }

        // GET: Vendor/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            return View(vendor);
        }

        // POST: Vendor/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string status, string notes)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor == null) return NotFound();

            vendor.Status = status;
            vendor.Notes = notes ?? "";
            vendor.IsActive = (status == "Approved");
            vendor.UpdatedDate = DateTime.Now;

            if (status == "Approved")
            {
                vendor.ApprovedDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã {status} vendor thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Vendor/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var vendor = await _context.Vendors
                .Include(v => v.Category)
                .FirstOrDefaultAsync(v => v.VendorId == id);

            if (vendor == null) return NotFound();

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.CurrentLogo = vendor.LogoUrl; // Thêm dòng này để hiển thị logo cũ
            return View(vendor);
        }

        // POST: Vendor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VendorId,ShopName,OwnerName,Email,Phone,Address,CategoryId,Description,Status,IsActive,Latitude,Longitude")] Vendor vendor, IFormFile? logoFile)
        {
            if (id != vendor.VendorId) return NotFound();

            // Xóa validation không cần thiết
            ModelState.Remove("Category");
            ModelState.Remove("Products");
            ModelState.Remove("PasswordHash");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingVendor = await _context.Vendors.FindAsync(id);
                    if (existingVendor == null) return NotFound();

                    // Xử lý upload logo mới qua API
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        using (var client = new HttpClient())
                        using (var content = new MultipartFormDataContent())
                        {
                            content.Add(new StreamContent(logoFile.OpenReadStream()), "file", logoFile.FileName);

                            var response = await client.PostAsync("https://localhost:7291/api/upload/image?folder=vendors", content);
                            var responseJson = await response.Content.ReadAsStringAsync();

                            using (var doc = JsonDocument.Parse(responseJson))
                            {
                                var url = doc.RootElement.GetProperty("url").GetString();
                                existingVendor.LogoUrl = url;
                            }
                        }
                    }

                    // Cập nhật các trường
                    existingVendor.ShopName = vendor.ShopName;
                    existingVendor.OwnerName = vendor.OwnerName;
                    existingVendor.Email = vendor.Email;
                    existingVendor.Phone = vendor.Phone;
                    existingVendor.Address = vendor.Address;
                    existingVendor.CategoryId = vendor.CategoryId;
                    existingVendor.Description = vendor.Description;
                    existingVendor.Status = vendor.Status;
                    existingVendor.IsActive = vendor.IsActive;
                    existingVendor.Latitude = vendor.Latitude;
                    existingVendor.Longitude = vendor.Longitude;
                    existingVendor.UpdatedDate = DateTime.Now;

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật vendor thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Lỗi: " + ex.Message);
                }
            }

            ViewBag.Categories = await _context.Categories.ToListAsync();
            return View(vendor);
        }

        // POST: Vendor/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var vendor = await _context.Vendors.FindAsync(id);
            if (vendor != null)
            {
                _context.Vendors.Remove(vendor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa vendor thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool VendorExists(int id)
        {
            return _context.Vendors.Any(e => e.VendorId == id);
        }
    }
}