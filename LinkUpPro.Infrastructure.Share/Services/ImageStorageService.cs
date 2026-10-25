using LinkUpPro.Core.Application.Interfaces;
using LinkUpPro.Core.Domain.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace LinkUpPro.Infrastructure.Share.Services
{
    public class ImageStorageService : IImageStorageService
    {
        private const long MaxFileSize = 5 * 1024 * 1024;
        private const string InvalidFormatMessage = "El archivo seleccionado no tiene un formato de imagen válido.";
        private const string TooLargeMessage = "La imagen seleccionada no puede superar los 5 MB.";

        private readonly string _rootPath;

        public ImageStorageService(IOptions<ImageStorageSettings> settings)
        {
            _rootPath = settings.Value.RootPath;
        }

        public async Task<string> SaveAsync(IFormFile file, string ownerId, string folderName)
        {
            if (file.Length == 0)
            {
                throw new InvalidDataException(InvalidFormatMessage);
            }

            if (file.Length > MaxFileSize)
            {
                throw new InvalidDataException(TooLargeMessage);
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                throw new InvalidDataException(InvalidFormatMessage);
            }

            if (!await HasValidImageSignatureAsync(file, extension) || !await IsValidImageContentAsync(file))
            {
                throw new InvalidDataException(InvalidFormatMessage);
            }

            // Owner ids and folder names are server values; reject anything that could leave the Images folder.
            if (!IsSafeSegment(ownerId) || !IsSafeSegment(folderName))
            {
                throw new InvalidOperationException("Invalid image storage location.");
            }

            string basePath = $"Images/{folderName}/{ownerId}";
            string directory = Path.Combine(_rootPath, basePath);

            Directory.CreateDirectory(directory);

            // The stored name is always generated; the uploaded name is only used for its extension.
            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(directory, fileName);

            bool uploadCompleted = false;

            try
            {
                await using (var stream = new FileStream(fullFilePath, FileMode.CreateNew))
                {
                    await file.CopyToAsync(stream);
                }

                uploadCompleted = true;
            }
            finally
            {
                if (!uploadCompleted && File.Exists(fullFilePath))
                {
                    File.Delete(fullFilePath);
                }
            }

            return $"{basePath}/{fileName}";
        }

        public bool Delete(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                return false;
            }

            string relativePath = imagePath.Replace('\\', '/');

            if (Path.IsPathRooted(relativePath))
            {
                return false;
            }

            string imagesDirectory = Path.GetFullPath(Path.Combine(_rootPath, "Images"));
            string fullPath = Path.GetFullPath(Path.Combine(_rootPath, relativePath));

            // The shared default profile picture is never removed.
            string defaultsDirectory = Path.Combine(imagesDirectory, "Defaults");

            if (!fullPath.StartsWith(imagesDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(defaultsDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(fullPath))
            {
                return false;
            }

            File.Delete(fullPath);

            return true;
        }

        private static bool IsSafeSegment(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');
        }

        private static async Task<bool> HasValidImageSignatureAsync(IFormFile file, string extension)
        {
            await using Stream stream = file.OpenReadStream();

            byte[] header = new byte[16];

            int bytesRead = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);

            if (bytesRead < header.Length)
            {
                return false;
            }

            return extension switch
            {
                ".jpg" or ".jpeg" =>
                    header[0] == 0xFF &&
                    header[1] == 0xD8 &&
                    header[2] == 0xFF,

                ".png" =>
                    header[0] == 0x89 &&
                    header[1] == 0x50 &&
                    header[2] == 0x4E &&
                    header[3] == 0x47 &&
                    header[4] == 0x0D &&
                    header[5] == 0x0A &&
                    header[6] == 0x1A &&
                    header[7] == 0x0A,

                ".webp" =>
                    header[0] == 'R' &&
                    header[1] == 'I' &&
                    header[2] == 'F' &&
                    header[3] == 'F' &&
                    header[8] == 'W' &&
                    header[9] == 'E' &&
                    header[10] == 'B' &&
                    header[11] == 'P' &&
                    header[12] == 'V' &&
                    header[13] == 'P' &&
                    header[14] == '8' &&
                    header[15] is (byte)' ' or (byte)'L' or (byte)'X',

                _ => false
            };
        }

        private static async Task<bool> IsValidImageContentAsync(IFormFile file)
        {
            try
            {
                await using Stream stream = file.OpenReadStream();
                using Image image = await Image.LoadAsync(stream);

                return image.Width > 0 && image.Height > 0;
            }
            catch (ImageFormatException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
