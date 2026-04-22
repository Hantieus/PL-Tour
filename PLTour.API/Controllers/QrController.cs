using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PLTour.API.Models.DbContext;
using PLTour.Shared.Models.Entities;
using QRCoder;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QrController : ControllerBase
    {
        private readonly PLTourDbContext _context;

        public QrController(PLTourDbContext context)
        {
            _context = context;
        }

        // GET: api/qr/generate/{locationId}
        [HttpGet("generate/{locationId}")]
        public async Task<IActionResult> GenerateQrCode(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
                return NotFound(new { message = "Không tìm thấy địa điểm" });

            // Tạo mã QR duy nhất (có thể dùng locationId hoặc GUID)
            var qrCodeValue = $"https://pltour.com/qr/{locationId}";  // Hoặc dùng GUID
            // var qrCodeValue = $"https://pltour.com/qr/{Guid.NewGuid()}";

            // Tạo QR Code
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(qrCodeValue, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);

                // Lưu mã QR vào database
                location.QrCode = qrCodeValue;
                location.QrCodeGeneratedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Trả về ảnh QR
                return File(qrCodeImage, "image/png");
            }
        }

        // GET: api/qr/location/{locationId}
        [HttpGet("location/{locationId}")]
        public async Task<IActionResult> GetQrCodeImage(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null || string.IsNullOrEmpty(location.QrCode))
                return NotFound(new { message = "Chưa có QR code cho địa điểm này" });

            // Tạo lại QR từ giá trị đã lưu
            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode(location.QrCode, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                return File(qrCodeImage, "image/png");
            }
        }

        // GET: api/qr/scan/{code}
        [HttpGet("scan/{code}")]
        public async Task<IActionResult> ScanQrCode(string code)
        {
            // Tìm location theo QR code
            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.QrCode == code || l.QrCode == $"https://pltour.com/qr/{code}");

            if (location == null)
                return NotFound(new { message = "Mã QR không hợp lệ" });

            // Redirect hoặc trả về thông tin location
            return Ok(new
            {
                locationId = location.LocationId,
                name = location.Name,
                description = location.Description,
                latitude = location.Latitude,
                longitude = location.Longitude
            });
        }
    }
}