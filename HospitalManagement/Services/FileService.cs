namespace HospitalManagement.Services
{
    public class FileService
    {

        public static async Task<string> SaveImageAsync(IFormFile file, string subFolder, long maxSizeInBytes = 5 * 1024 * 1024)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new InvalidOperationException("Loại file không hợp lệ! Chỉ chấp nhận ảnh");

            if (file.Length > maxSizeInBytes)
                throw new InvalidOperationException("File quá lớn, phải nhỏ hơn 5MB.");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", subFolder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }
        public static async Task<string> SaveTestFileAsync(IFormFile file, string subFolder, long maxSizeInBytes = 5 * 1024 * 1024)
        {
            var allowedTypes = new[]
            {
                "application/pdf",
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/jpg"
            };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new InvalidOperationException("File không hợp lệ. Chỉ cho phép ảnh hoặc PDF.");

            if (file.Length > maxSizeInBytes)
                throw new InvalidOperationException("Kích thước file vượt quá giới hạn (tối đa 5MB).");

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", subFolder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            var fullPath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }
        public static void DeleteImage(string folderName, string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return;

            var rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var fullPath = Path.Combine(rootPath, "img", folderName, fileName);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
    }
}
