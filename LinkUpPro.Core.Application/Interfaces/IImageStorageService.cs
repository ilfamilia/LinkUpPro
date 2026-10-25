using Microsoft.AspNetCore.Http;

namespace LinkUpPro.Core.Application.Interfaces
{
    public interface IImageStorageService
    {
        /// <summary>
        /// Validates extension, signature, content and size (5 MB) and stores the image under
        /// Images/{folderName}/{ownerId}. Returns the relative path; throws InvalidDataException with a user message.
        /// </summary>
        Task<string> SaveAsync(IFormFile file, string ownerId, string folderName);

        /// <summary>Deletes a stored image by its relative path; paths outside the Images folder are ignored.</summary>
        bool Delete(string? imagePath);
    }
}
