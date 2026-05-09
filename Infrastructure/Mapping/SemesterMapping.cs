namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class SemesterMapping
    {
        public static Domain.Entities.Semester ToDomain(this Data.Entities.Semester entity)
        {
            return new Domain.Entities.Semester
            {
                Id = entity.SemesterId,
                AcademyYearId = entity.AcademyYearId,
                Name = entity.SemesterName
            };
        }

        public static Data.Entities.Semester ToEntity(this Domain.Entities.Semester domain)
        {
            return new Data.Entities.Semester
            {
                SemesterId = domain.Id,
                AcademyYearId = domain.AcademyYearId,
                SemesterName = domain.Name
            };
        }
    }
}
