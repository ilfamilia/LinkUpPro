using SixLabors.ImageSharp;

namespace LinkUpPro.Helpers
{
    public static class FileManager
    {
        public static string Upload(IFormFile? file, string id, string folderName,
            bool isEditMode = false, string imagePath = "")
        {
            if (file == null)
            {
                return isEditMode ? imagePath : string.Empty;
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (file.Length == 0 || file.Length > maxFileSize)
            {
                throw new InvalidDataException(file.Length == 0
                    ? "El archivo seleccionado no tiene un formato de imagen válido."
                    : "La imagen seleccionada no puede superar los 5 MB.");
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp"))
            {
                throw new InvalidDataException("El archivo seleccionado no tiene un formato de imagen válido.");
            }

            if (!HasValidImageSignature(file, extension))
            {
                throw new InvalidDataException("El archivo seleccionado no tiene un formato de imagen válido.");
            }

            if (!IsValidImageContent(file))
            {
                throw new InvalidDataException("El archivo seleccionado no tiene un formato de imagen válido.");
            }

            string basePath = $"Images/{folderName}/{id}";
            string path = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/{basePath}");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string fileName = $"{Guid.NewGuid()}{extension}";
            string fullFilePath = Path.Combine(path, fileName);

            bool uploadCompleted = false;

            try
            {
                using (var stream = new FileStream(fullFilePath, FileMode.CreateNew))
                {
                    file.CopyTo(stream);
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

            if (isEditMode && !string.IsNullOrWhiteSpace(imagePath))
            {
                string[] oldImagePath = imagePath.Split('/');
                string oldFileName = oldImagePath[^1];
                string completeOldPath = Path.Combine(path, oldFileName);

                if (File.Exists(completeOldPath))
                {
                    File.Delete(completeOldPath);
                }
            }

            return $"{basePath}/{fileName}";
        }

        public static bool Delete(int id, string folderName)
        {
            string basePath = $"Images/{folderName}/{id}";
            string path = Path.Combine(Directory.GetCurrentDirectory(), $"wwwroot/{basePath}");

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);

                return true;
            }

            return false;
        }

        public static bool DeleteFile(string? imagePath)
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

            string webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string imagesDirectory = Path.GetFullPath(Path.Combine(webRoot, "Images"));

            string fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

            if (!fullPath.StartsWith(imagesDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!File.Exists(fullPath))
            {
                return false;
            }

            File.Delete(fullPath);

            return true;
        }

        private static bool HasValidImageSignature(IFormFile file, string extension)
        {
            using Stream stream = file.OpenReadStream();

            byte[] header = new byte[16];

            int bytesRead = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

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

        private static bool IsValidImageContent(IFormFile file)
        {
            try
            {
                using Stream stream = file.OpenReadStream();
                using Image image = Image.Load(stream);

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
