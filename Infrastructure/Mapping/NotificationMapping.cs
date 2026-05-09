namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class NotificationMapping
    {
        public static Domain.Entities.Notification ToDomain(this Data.Entities.Notification entity)
        {
            return new Domain.Entities.Notification
            {
                Id = entity.NotificationId,
                UserId = entity.UserId,
                Title = entity.Title,
                Content = entity.Content,
                Type = entity.Type,
                IsRead = entity.IsRead,
                CreatedAt = entity.CreatedAt,
                RelatedId = entity.RelatedId,
                CreatedBy = entity.CreatedBy
            };
        }

        public static Data.Entities.Notification ToEntity(this Domain.Entities.Notification domain)
        {
            return new Data.Entities.Notification
            {
                NotificationId = domain.Id,
                UserId = domain.UserId,
                Title = domain.Title,
                Content = domain.Content,
                Type = domain.Type,
                IsRead = domain.IsRead,
                CreatedAt = domain.CreatedAt,
                RelatedId = domain.RelatedId,
                CreatedBy = domain.CreatedBy
            };
        }
    }
}
