namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class EmailNotificationMapping
    {
        public static Domain.Entities.EmailNotification ToDomain(this Data.Entities.EmailNotification entity)
        {
            return new Domain.Entities.EmailNotification
            {
                Id = entity.EmailId,
                UserId = entity.UserId,
                Email = entity.Email,
                Status = entity.Status,
                SentAt = entity.SentAt,
                ErrorMessage = entity.ErrorMessage
            };
        }

        public static Data.Entities.EmailNotification ToEntity(this Domain.Entities.EmailNotification domain)
        {
            return new Data.Entities.EmailNotification
            {
                EmailId = domain.Id,
                UserId = domain.UserId,
                Email = domain.Email,
                Status = domain.Status,
                SentAt = domain.SentAt,
                ErrorMessage = domain.ErrorMessage
            };
        }
    }
}
