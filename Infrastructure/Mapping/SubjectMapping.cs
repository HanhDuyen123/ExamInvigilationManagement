namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class SubjectMapping
    {
        public static Domain.Entities.Subject ToDomain(this Data.Entities.Subject entity)
        {
            return new Domain.Entities.Subject
            {
                Id = entity.SubjectId,
                FacultyId = entity.FacultyId,
                Name = entity.SubjectName,
                Credit = entity.Credit,
                Faculty = entity.Faculty == null ? null : new Domain.Entities.Faculty
                {
                    Id = entity.Faculty.FacultyId,
                    Name = entity.Faculty.FacultyName
                }
            };
        }

        public static Data.Entities.Subject ToEntity(this Domain.Entities.Subject domain)
        {
            return new Data.Entities.Subject
            {
                SubjectId = domain.Id,
                FacultyId = domain.FacultyId,
                SubjectName = domain.Name,
                Credit = domain.Credit
            };
        }
    }
}
