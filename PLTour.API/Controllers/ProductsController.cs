using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using PLTour.Shared.Models.DTO;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public ProductsController(PLTourDbContext context)
        {
            _context = context;
        }

        // GET: api/products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
            [FromQuery] int? vendorId = null,
            [FromQuery] bool? isAvailable = null)
        {
            var query = _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Category)
                .AsQueryable();

            if (vendorId.HasValue)
            {
                query = query.Where(p => p.VendorId == vendorId);
            }

            if (isAvailable.HasValue)
            {
                query = query.Where(p => p.IsAvailable == isAvailable.Value);
            }

            var products = await query
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var productDtos = products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                VendorId = p.VendorId,
                VendorName = p.Vendor?.ShopName ?? "",
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "",
                IsAvailable = p.IsAvailable,
                StockQuantity = p.StockQuantity,
                CreatedDate = p.CreatedDate
            }).ToList();

            return Ok(productDtos);
        }

        // GET: api/products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
            {
                return NotFound();
            }

            return Ok(new ProductDto
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                ImageUrl = product.ImageUrl,
                VendorId = product.VendorId,
                VendorName = product.Vendor?.ShopName ?? "",
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name ?? "",
                IsAvailable = product.IsAvailable,
                StockQuantity = product.StockQuantity,
                CreatedDate = product.CreatedDate
            });
        }

        // GET: api/products/by-vendor/5
        [HttpGet("by-vendor/{vendorId}")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByVendor(int vendorId)
        {
            var products = await _context.Products
                .Include(p => p.Vendor)
                .Include(p => p.Category)
                .Where(p => p.VendorId == vendorId && p.IsAvailable)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var productDtos = products.Select(p => new ProductDto
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                VendorId = p.VendorId,
                VendorName = p.Vendor?.ShopName ?? "",
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? "",
                IsAvailable = p.IsAvailable,
                StockQuantity = p.StockQuantity,
                CreatedDate = p.CreatedDate
            }).ToList();

            return Ok(productDtos);
        }
    }
}