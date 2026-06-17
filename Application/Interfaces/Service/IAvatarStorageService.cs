using ExamInvigilationManagement.Application.DTOs.Import;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IAvatarStorageService
    {
        Task<string> SaveAvatarAsync(int userId, ImportFileDto file, CancellationToken cancellationToken = default);
    }
}
