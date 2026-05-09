namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class InformationMapping
    {
        public static Domain.Entities.Information ToDomain(this Data.Entities.Information entity)
        {
            return new Domain.Entities.Information
            {
                Id = entity.InformationId,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                Dob = entity.Dob,
                Phone = entity.Phone,
                Address = entity.Address,
                Email = entity.Email,
                Avt = entity.Avt,
                Gender = entity.Gender,
                PositionId = entity.PositionId,
                Position = entity.Position == null ? null : new Domain.Entities.Position
                {
                    Id = entity.Position.PositionId,
                    Name = entity.Position.PositionName
                }
            };
        }

        public static Data.Entities.Information ToEntity(this Domain.Entities.Information domain)
        {
            return new Data.Entities.Information
            {
                InformationId = domain.Id,
                FirstName = domain.FirstName,
                LastName = domain.LastName,
                Dob = domain.Dob,
                Phone = domain.Phone,
                Address = domain.Address,
                Email = domain.Email,
                Avt = domain.Avt,
                Gender = domain.Gender,
                PositionId = domain.PositionId
            };
        }
    }
}
