namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamSessionMapping
    {
        public static Domain.Entities.ExamSession ToDomain(this Data.Entities.ExamSession entity)
        {
            return new Domain.Entities.ExamSession
            {
                Id = entity.SessionId,
                PeriodId = entity.PeriodId,
                Name = entity.SessionName
            };
        }

        public static Data.Entities.ExamSession ToEntity(this Domain.Entities.ExamSession domain)
        {
            return new Data.Entities.ExamSession
            {
                SessionId = domain.Id,
                PeriodId = domain.PeriodId,
                SessionName = domain.Name
            };
        }
    }
}
