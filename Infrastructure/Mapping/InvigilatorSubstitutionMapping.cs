namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class InvigilatorSubstitutionMapping
    {
        public static Domain.Entities.InvigilatorSubstitution ToDomain(this Data.Entities.InvigilatorSubstitution entity)
        {
            return new Domain.Entities.InvigilatorSubstitution
            {
                Id = entity.SubstitutionId,
                ExamInvigilatorId = entity.ExamInvigilatorId,
                UserId = entity.UserId,
                SubstituteUserId = entity.SubstituteUserId,
                Status = entity.Status,
                CreateAt = entity.CreateAt
            };
        }

        public static Data.Entities.InvigilatorSubstitution ToEntity(this Domain.Entities.InvigilatorSubstitution domain)
        {
            return new Data.Entities.InvigilatorSubstitution
            {
                SubstitutionId = domain.Id,
                ExamInvigilatorId = domain.ExamInvigilatorId,
                UserId = domain.UserId,
                SubstituteUserId = domain.SubstituteUserId,
                Status = domain.Status,
                CreateAt = domain.CreateAt
            };
        }
    }
}
