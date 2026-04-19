using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HomeBroker.Application;
using HomeBroker.Application.IImageService;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ImageUploadResult = HomeBroker.Application.IImageService.ImageUploadResult;

namespace HomeBroker.Infrastructure.Services.ImageService
{
    public class ImageService : IImageService
    {
        private readonly Cloudinary _cloudinary;
        private readonly CloudinarySettings _config;
        private const long MaxSizeBytes = 5 * 1024 * 1024;

        // Inject IOptions<CloudinarySettings> here
        public ImageService(IOptions<CloudinarySettings> config)
        {
            _config = config.Value;

            // Initialize the Account using the injected settings
            var account = new Account(
                _config.CloudName,
                _config.ApiKey,
                _config.ApiSecret
            );

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile? formFile)
        {
            // ── Validate ───────────────────────────────────────────────
            if (formFile == null || formFile.Length == 0)
                throw new ArgumentException("Invalid file.");

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/jpg" };
            if (!allowedTypes.Contains(formFile.ContentType.ToLower()))
                throw new ArgumentException("Only JPEG, PNG, and WebP are allowed.");

            if (formFile.Length > MaxSizeBytes)
                throw new ArgumentException("Image must be under 5MB.");


            // ── Upload to Cloudinary ───────────────────────────────────
            using var stream = formFile.OpenReadStream();

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(formFile.FileName, stream),
                Folder = "readings", // Organizes files in Cloudinary
                DisplayName = formFile.FileName,

                // ── Transformation (Replacing ImageSharp logic) ────────
                // This tells Cloudinary to resize it to 1024px max width 
                // and use "auto" quality/format for best compression.
                Transformation = new Transformation()
                    .Width(1024)
                    .Crop("limit")
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
                throw new Exception($"Cloudinary Upload Error: {uploadResult.Error.Message}");

            return uploadResult.SecureUrl.ToString();
            
        }


    }
}
