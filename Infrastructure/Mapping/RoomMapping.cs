namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class RoomMapping
    {
        public static Domain.Entities.Room ToDomain(this Data.Entities.Room entity)
        {
            return new Domain.Entities.Room
            {
                Id = entity.RoomId,
                BuildingId = entity.BuildingId,
                Name = entity.RoomName,
                Capacity = entity.Capacity,
                Building = entity.Building == null ? null : new Domain.Entities.Building
                {
                    Id = entity.Building.BuildingId,
                    Name = entity.Building.BuildingName
                }
            };
        }

        public static Data.Entities.Room ToEntity(this Domain.Entities.Room domain)
        {
            return new Data.Entities.Room
            {
                RoomId = domain.Id,
                BuildingId = domain.BuildingId,
                RoomName = domain.Name,
                Capacity = domain.Capacity
            };
        }
    }
}
