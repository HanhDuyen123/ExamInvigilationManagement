namespace ExamInvigilationManagement.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {

        Task<Domain.Entities.User?> GetByIdAsync(int userId);

        Task UpdateAsync(Domain.Entities.User user);
        Task UpdateProfileAsync(Domain.Entities.User user);
        Task<Domain.Entities.User?> GetByUsernameAndEmailAsync(string username, string email);
        Task<Domain.Entities.User?> GetByUsernameAsync(string username);


        Task SaveResetTokenAsync(int userId, string token, DateTime expiredAt);
        Task<Domain.Entities.PasswordResetToken?> GetValidTokenAsync(string token);
        Task UpdatePasswordAsync(int userId, string newPasswordHash);

        Task MarkTokenAsUsedAsync(string token);


        Task<Domain.Entities.User?> GetProfileByIdAsync(int userId);
    }
}
