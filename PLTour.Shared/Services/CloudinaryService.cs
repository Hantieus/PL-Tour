using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Security.Principal;

namespace PLTour.Shared.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<string> UploadAudioAsync(IFormFile file, string folder);
        Task<bool> DeleteFileAsync(string publicId);
        string ExtractPublicIdFromUrl(string url);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]);
            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Transformation = new Transformation().Quality("auto").FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<string> UploadAudioAsync(IFormFile file, string folder)
        {
            if (file == null || file.Length == 0) return null;

            await using var stream = file.OpenReadStream();
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result.Result == "ok";
        }

        // Lấy publicId từ URL Cloudinary
        public string ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;

                // Tìm vị trí của "upload/" trong URL Cloudinary
                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i] == "upload/")
                    {
                        if (i + 1 < segments.Length)
                        {
                            var path = string.Join("", segments.Skip(i + 1));
                            // Loại bỏ phần mở rộng file
                            var lastDot = path.LastIndexOf('.');
                            if (lastDot > 0)
                                path = path.Substring(0, lastDot);
                            return path;
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}