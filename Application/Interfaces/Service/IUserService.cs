using ExamInvigilationManagement.Application.DTOs;
using System.Threading.Tasks;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IUserService
    {
        Task<ProfileDto?> GetProfileAsync(int userId);
        Task UpdateProfileAsync(int userId, UpdateProfileDto dto);
    }
}
