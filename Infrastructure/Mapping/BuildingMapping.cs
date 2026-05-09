namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class BuildingMapping
    {
        public static Domain.Entities.Building ToDomain(this Infrastructure.Data.Entities.Building entity)
        {
            return new Domain.Entities.Building
            {
                Id = entity.BuildingId,
                Name = entity.BuildingName
            };
        }

        public static Data.Entities.Building ToEntity(this Domain.Entities.Building domain)
        {
            return new Data.Entities.Building
            {
                BuildingId = domain.Id,
                BuildingName = domain.Name
            };
        }
    }
}
