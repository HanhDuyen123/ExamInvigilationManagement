namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class AcademyYearMapping
    {
        public static Domain.Entities.AcademyYear ToDomain(this Data.Entities.AcademyYear entity)
        {
            return new Domain.Entities.AcademyYear
            {
                Id = entity.AcademyYearId,
                Name = entity.AcademyYearName
            };
        }

        public static Data.Entities.AcademyYear ToEntity(this Domain.Entities.AcademyYear domain)
        {
            return new Data.Entities.AcademyYear
            {
                AcademyYearId = domain.Id,
                AcademyYearName = domain.Name
            };
        }

    }
}
