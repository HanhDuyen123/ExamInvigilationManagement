using ExamInvigilationManagement.Application.DTOs.Import;
using ExamInvigilationManagement.Application.Interfaces.Service;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class AvatarStorageService : IAvatarStorageService
    {
        private readonly IWebHostEnvironment _environment;

        public AvatarStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<string> SaveAvatarAsync(int userId, ImportFileDto file, CancellationToken cancellationToken = default)
        {
            var allowed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = ".jpg",
                [".jpeg"] = ".jpg",
                [".png"] = ".png",
                [".webp"] = ".webp"
            };

            var ext = Path.GetExtension(file.FileName);
            if (!allowed.TryGetValue(ext, out var safeExt))
                throw new InvalidOperationException("Ảnh đại diện chỉ hỗ trợ JPG, PNG hoặc WEBP.");

            if (file.Length > 2 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh đại diện không được vượt quá 2MB.");

            var root = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(root);

            var fileName = $"user-{userId}-{Guid.NewGuid():N}{safeExt}";
            var path = Path.Combine(root, fileName);

            await using var stream = File.Create(path);
            await using var uploadStream = file.OpenReadStream();
            await uploadStream.CopyToAsync(stream, cancellationToken);

            return $"/uploads/avatars/{fileName}";
        }
    }
}
