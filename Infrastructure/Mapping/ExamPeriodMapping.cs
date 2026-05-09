namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamPeriodMapping
    {
        public static Domain.Entities.ExamPeriod ToDomain(this Data.Entities.ExamPeriod entity)
        {
            return new Domain.Entities.ExamPeriod
            {
                Id = entity.PeriodId,
                SemesterId = entity.SemesterId,
                Name = entity.PeriodName
            };
        }

        public static Data.Entities.ExamPeriod ToEntity(this Domain.Entities.ExamPeriod domain)
        {
            return new Data.Entities.ExamPeriod
            {
                PeriodId = domain.Id,
                SemesterId = domain.SemesterId,
                PeriodName = domain.Name
            };
        }
    }
}
