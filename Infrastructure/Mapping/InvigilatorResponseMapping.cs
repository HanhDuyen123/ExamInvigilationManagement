namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class InvigilatorResponseMapping
    {
        public static Domain.Entities.InvigilatorResponse ToDomain(this Data.Entities.InvigilatorResponse entity)
        {
            return new Domain.Entities.InvigilatorResponse
            {
                Id = entity.ResponseId,
                UserId = entity.UserId,
                ExamInvigilatorId = entity.ExamInvigilatorId,
                Status = entity.Status,
                Note = entity.Note,
                ResponseAt = entity.ResponseAt
            };
        }

        public static Data.Entities.InvigilatorResponse ToEntity(this Domain.Entities.InvigilatorResponse domain)
        {
            return new Data.Entities.InvigilatorResponse
            {
                ResponseId = domain.Id,
                UserId = domain.UserId,
                ExamInvigilatorId = domain.ExamInvigilatorId,
                Status = domain.Status,
                Note = domain.Note,
                ResponseAt = domain.ResponseAt
            };
        }
    }
}
