namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class PasswordResetTokenMapping
    {
        public static Domain.Entities.PasswordResetToken ToDomain(this Data.Entities.PasswordResetToken entity)
        {
            return new Domain.Entities.PasswordResetToken
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Token = entity.Token,
                ExpiredAt = entity.ExpiredAt,
                IsUsed = entity.IsUsed
            };
        }
    }
}
