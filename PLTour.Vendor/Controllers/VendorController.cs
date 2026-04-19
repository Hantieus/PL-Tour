using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;


namespace PLTour.Vendor.Controllers
{
    [Authorize(Roles = "Admin,Manager")]
    public class VendorController : Controller
    {
        private readonly PLTourDbContext _context;

        public VendorController(PLTourDbContext context)
        {
            _context = context;
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
            ViewBag.StatusCounts = await _context.Vendors
                .GroupBy(v => v.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Status, g => g.Count);

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
            vendor.Notes = notes;
            vendor.IsActive = (status == "Approved");

            if (status == "Approved")
            {
                vendor.ApprovedDate = DateTime.UtcNow;
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
            return View(vendor);
        }

        // POST: Vendor/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PLTour.Shared.Models.Entities.Vendor vendor)
        {
            if (id != vendor.VendorId) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    vendor.UpdatedDate = DateTime.UtcNow;
                    _context.Update(vendor);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật vendor thành công!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VendorExists(vendor.VendorId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
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