namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class CourseOfferingMapping
    {
        public static Domain.Entities.CourseOffering ToDomain(this Data.Entities.CourseOffering entity)
        {
            return new Domain.Entities.CourseOffering
            {
                Id = entity.OfferingId,
                UserId = entity.UserId,
                SemesterId = entity.SemesterId,
                SubjectId = entity.SubjectId,
                ClassName = entity.ClassName,
                GroupNumber = entity.GroupNumber,
                User = entity.User == null ? null : new Domain.Entities.User
                {
                    Id = entity.User.UserId,
                    UserName = entity.User.UserName,
                    Information = entity.User.Information == null ? null : new Domain.Entities.Information
                    {
                        FirstName = entity.User.Information.FirstName,
                        LastName = entity.User.Information.LastName
                    }
                },

                Subject = entity.Subject != null ? new Domain.Entities.Subject
                {
                    Id = entity.Subject.SubjectId,
                    Name = entity.Subject.SubjectName
                } : null,

                Semester = entity.Semester != null ? new Domain.Entities.Semester
                {
                    Id = entity.Semester.SemesterId,
                    Name = entity.Semester.SemesterName,

                    AcademyYear = entity.Semester.AcademyYear != null
                        ? new Domain.Entities.AcademyYear
                        {
                            Id = entity.Semester.AcademyYear.AcademyYearId,
                            Name = entity.Semester.AcademyYear.AcademyYearName
                        }
                        : null
                } : null
            };
        }

        public static Data.Entities.CourseOffering ToEntity(this Domain.Entities.CourseOffering domain)
        {
            return new Data.Entities.CourseOffering
            {
                OfferingId = domain.Id,
                UserId = domain.UserId,
                SemesterId = domain.SemesterId,
                SubjectId = domain.SubjectId,
                ClassName = domain.ClassName,
                GroupNumber = domain.GroupNumber
            };
        }
    }
}
