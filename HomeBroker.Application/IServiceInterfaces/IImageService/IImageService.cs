using Microsoft.AspNetCore.Http;

namespace HomeBroker.Application.IImageService
{
    public interface IImageService
    {
        Task<string> UploadImageAsync(IFormFile? formFile);
    }

    public class ImageUploadResult
    {
        public string ImageUrl { get; set; } = string.Empty;
      
    }
}
