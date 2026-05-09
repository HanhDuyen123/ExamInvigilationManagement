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

            user.Information.FirstName = dto.FirstName;
            user.Information.LastName = dto.LastName;
            user.Information.Phone = dto.Phone;
            user.Information.Address = dto.Address;

            await _repo.UpdateProfileAsync(user);
        }
    }
}
