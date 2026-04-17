using Microsoft.AspNetCore.Mvc;

namespace PLTour.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public UploadController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile file, [FromQuery] string folder = "general")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { error = "Invalid file format" });

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/{folder}/{uniqueFileName}";
            return Ok(new { url = url });
        }

        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio(IFormFile file, [FromQuery] string folder = "audio")
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded" });

            var allowedExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac" };
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
                return BadRequest(new { error = "Invalid audio format" });

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", folder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var url = $"/uploads/{folder}/{uniqueFileName}";
            return Ok(new { url = url });
        }
    }
}