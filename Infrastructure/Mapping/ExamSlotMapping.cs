namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamSlotMapping
    {
        public static Domain.Entities.ExamSlot ToDomain(this Data.Entities.ExamSlot entity)
        {
            return new Domain.Entities.ExamSlot
            {
                Id = entity.SlotId,
                SessionId = entity.SessionId,
                Name = entity.SlotName,
                TimeStart = entity.TimeStart
            };
        }

        public static Data.Entities.ExamSlot ToEntity(this Domain.Entities.ExamSlot domain)
        {
            return new Data.Entities.ExamSlot
            {
                SlotId = domain.Id,
                SessionId = domain.SessionId,
                SlotName = domain.Name,
                TimeStart = domain.TimeStart
            };
        }
    }
}
