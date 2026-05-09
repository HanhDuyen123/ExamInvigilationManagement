namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class LecturerBusySlotMapping
    {
        public static Domain.Entities.LecturerBusySlot ToDomain(this Data.Entities.LecturerBusySlot entity)
        {
            return new Domain.Entities.LecturerBusySlot
            {
                Id = entity.BusySlotId,
                UserId = entity.UserId,
                SlotId = entity.SlotId,
                BusyDate = entity.BusyDate,
                Note = entity.Note,
                CreateAt = entity.CreateAt
            };
        }

        public static Data.Entities.LecturerBusySlot ToEntity(this Domain.Entities.LecturerBusySlot domain)
        {
            return new Data.Entities.LecturerBusySlot
            {
                BusySlotId = domain.Id,
                UserId = domain.UserId,
                SlotId = domain.SlotId,
                BusyDate = domain.BusyDate,
                Note = domain.Note,
                CreateAt = domain.CreateAt
            };
        }
    }
}
