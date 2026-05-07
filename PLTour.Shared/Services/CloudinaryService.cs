using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PLTour.Shared.Services
{
    public interface ICloudinaryService
    {
        Task<string> UploadImageAsync(IFormFile file, string folder);
        Task<string> UploadAudioAsync(IFormFile file, string folder);
        Task<string> UploadAudioAsync(Stream audioStream, string fileName, string folder, string resourceType = "raw");
        Task<bool> DeleteFileAsync(string publicId);
        string? ExtractPublicIdFromUrl(string url);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;
        private readonly ILogger<CloudinaryService> _logger;

        public CloudinaryService(IConfiguration configuration, ILogger<CloudinaryService> logger)
        {
            var account = new Account(
                configuration["Cloudinary:CloudName"],
                configuration["Cloudinary:ApiKey"],
                configuration["Cloudinary:ApiSecret"]);
            _cloudinary = new Cloudinary(account);
            _logger = logger;
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
            return await UploadAudioAsync(stream, file.FileName, folder);
        }

        public async Task<string> UploadAudioAsync(Stream audioStream, string fileName, string folder, string resourceType = "raw")
        {
            if (audioStream == null) return null;

            _logger.LogInformation("Uploading audio to Cloudinary. FileName={FileName}, Folder={Folder}", fileName, folder);

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, audioStream),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            _logger.LogInformation("Cloudinary upload completed. PublicId={PublicId}, Url={Url}", uploadResult.PublicId, uploadResult.SecureUrl?.ToString());
            return uploadResult.SecureUrl?.ToString();
        }

        public async Task<bool> DeleteFileAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result?.StatusCode == System.Net.HttpStatusCode.OK;
        }

        public string? ExtractPublicIdFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url)) return null;

            try
            {
                var uri = new Uri(url);
                var segments = uri.Segments;

                for (int i = 0; i < segments.Length; i++)
                {
                    if (segments[i] == "upload/")
                    {
                        if (i + 1 < segments.Length)
                        {
                            var path = string.Join("", segments.Skip(i + 1));
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
