using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EventHub.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EventHub.Services.Implementations
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<BlobStorageService> _logger;
        private const string ContainerName = "eventhub-images";

        public BlobStorageService(BlobServiceClient blobServiceClient, ILogger<BlobStorageService> logger)
        {
            _blobServiceClient = blobServiceClient;
            _logger = logger;
        }

        /// <summary>
        /// Upload image file to Azure Blob Storage
        /// </summary>
        public async Task<string> UploadImageAsync(IFormFile imageFile)
        {
            try
            {
                // Validate file
                if (imageFile == null || imageFile.Length == 0)
                {
                    throw new ArgumentException("Image file is empty");
                }

                // Create unique filename
                var fileName = $"event-{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";

                // Get container client
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                await containerClient.CreateIfNotExistsAsync();

                // Get blob client
                var blobClient = containerClient.GetBlobClient(fileName);

                // Upload file
                using (var stream = imageFile.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }

                _logger.LogInformation($"✅ Image uploaded to Blob: {fileName}");

                // Return blob URL
                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error uploading image to Blob Storage: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Delete image file from Azure Blob Storage
        /// </summary>
        public async Task DeleteImageAsync(string blobUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(blobUrl))
                    return;

                // Extract blob name from URL
                var uri = new Uri(blobUrl);
                var fileName = Path.GetFileName(uri.LocalPath);

                // Get container client
                var containerClient = _blobServiceClient.GetBlobContainerClient(ContainerName);
                var blobClient = containerClient.GetBlobClient(fileName);

                // Delete blob
                await blobClient.DeleteIfExistsAsync();
                _logger.LogInformation($"✅ Image deleted from Blob: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Error deleting image from Blob Storage: {ex.Message}");
                throw;
            }
        }
    }
}