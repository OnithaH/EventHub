using System.Threading.Tasks;

namespace EventHub.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadImageAsync(IFormFile imageFile);
        Task DeleteImageAsync(string blobUrl);
    }
}