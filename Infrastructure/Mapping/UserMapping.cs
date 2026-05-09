namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class UserMapping
    {
        public static Domain.Entities.User ToDomain(this Data.Entities.User entity)
        {
            return new Domain.Entities.User
            {
                Id = entity.UserId,
                RoleId = entity.RoleId,
                Role = entity.Role != null
                    ? new Domain.Entities.Role
                    {
                        Id = entity.Role.RoleId,
                        Name = entity.Role.RoleName
                    }
                    : null,
                InformationId = entity.InformationId,
                FacultyId = entity.FacultyId,
                Faculty = entity.Faculty != null
                    ? new Domain.Entities.Faculty
                    {
                        Id = entity.Faculty.FacultyId,
                        Name = entity.Faculty.FacultyName
                    }
                    : null,
                UserName = entity.UserName,
                PasswordHash = entity.PasswordHash,
                IsActive = entity.IsActive,
                FailedLoginAttempts = entity.FailedLoginAttempts ?? 0,
                LastLogin = entity.LastLogin,
                LockoutEnd = entity.LockoutEnd,
                Information = entity.Information?.ToDomain(),
            };
        }

        public static Data.Entities.User ToEntity(this Domain.Entities.User domain)
        {
            return new Data.Entities.User
            {
                UserId = domain.Id,
                RoleId = domain.RoleId,
                InformationId = domain.InformationId,
                FacultyId = domain.FacultyId,
                UserName = domain.UserName,
                PasswordHash = domain.PasswordHash,
                IsActive = domain.IsActive,
                FailedLoginAttempts = domain.FailedLoginAttempts,
                LastLogin = domain.LastLogin,
                LockoutEnd = domain.LockoutEnd
            };
        }
    }
}
