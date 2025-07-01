namespace HospitalManagement.Services
{
    public class ImageService
    {
        public static async Task<string> SaveImageAsync(IFormFile file, string subFolder, long maxSizeInBytes = 5 * 1024 * 1024)
        {
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/jpg" };

            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                throw new InvalidOperationException("Loại file không hợp lệ.");

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
    }
}
