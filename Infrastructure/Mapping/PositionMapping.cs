namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class PositionMapping
    {
        public static Domain.Entities.Position ToDomain(this Data.Entities.Position entity)
        {
            return new Domain.Entities.Position
            {
                Id = entity.PositionId,
                Name = entity.PositionName
            };
        }

        public static Data.Entities.Position ToEntity(this Domain.Entities.Position domain)
        {
            return new  Data.Entities.Position
            {
                PositionId = domain.Id,
                PositionName = domain.Name
            };
        }
    }
}
