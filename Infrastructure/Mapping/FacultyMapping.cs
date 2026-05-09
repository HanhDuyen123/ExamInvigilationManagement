namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class FacultyMapping
    {
        public static Domain.Entities.Faculty ToDomain(this Data.Entities.Faculty entity)
        {
            return new Domain.Entities.Faculty
            {
                Id = entity.FacultyId,
                Name = entity.FacultyName
            };
        }

        public static Data.Entities.Faculty ToEntity(this Domain.Entities.Faculty domain)
        {
            return new Data.Entities.Faculty
            {
                FacultyId = domain.Id,
                FacultyName = domain.Name
            };
        }
    }
}
