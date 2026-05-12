using ExamInvigilationManagement.Application.DTOs;
using ExamInvigilationManagement.Application.Interfaces.Repositories;
using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.EntityFrameworkCore;

namespace ExamInvigilationManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repo;

        public UserService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProfileDto> GetProfileAsync(int userId)
        {
            var user = await _repo.GetProfileByIdAsync(userId);

            if (user == null) return null;

            return new ProfileDto
            {
                UserName = user.UserName,

                FirstName = user.Information.FirstName,
                LastName = user.Information.LastName,

                Dob = user.Information.Dob,
                Phone = user.Information.Phone,
                Address = user.Information.Address,

                Email = user.Information.Email,
                Avt = user.Information.Avt,
                Gender = user.Information.Gender,

                PositionName = user.Information.Position?.Name,
                IsActive = user.IsActive
            };
        }

        public async Task UpdateProfileAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _repo.GetByIdAsync(userId);

            if (user == null || user.Information == null)
                return;

            var gender = NormalizeGender(dto.Gender);
            if (!string.IsNullOrWhiteSpace(dto.Gender) && string.IsNullOrWhiteSpace(gender))
                throw new ArgumentException("Giới tính không hợp lệ.");

            user.Information.FirstName = (dto.FirstName ?? string.Empty).Trim();
            user.Information.LastName = (dto.LastName ?? string.Empty).Trim();
            user.Information.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            user.Information.Address = string.IsNullOrWhiteSpace(dto.Address) ? null : dto.Address.Trim();
            user.Information.Dob = dto.Dob;
            user.Information.Gender = gender;
            if (!string.IsNullOrWhiteSpace(dto.Avt))
                user.Information.Avt = dto.Avt;

            await _repo.UpdateProfileAsync(user);
        }

        private static string? NormalizeGender(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            return value.Trim().ToLowerInvariant() switch
            {
                "male" or "nam" => "Male",
                "female" or "nữ" or "nu" => "Female",
                _ => null
            };
        }
    }
}
