namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamScheduleMapping
    {
        public static Domain.Entities.ExamSchedule ToDomain(this Data.Entities.ExamSchedule entity)
        {
            return new Domain.Entities.ExamSchedule
            {
                Id = entity.ExamScheduleId,
                SlotId = entity.SlotId,
                AcademyYearId = entity.AcademyYearId,
                SemesterId = entity.SemesterId,
                PeriodId = entity.PeriodId,
                SessionId = entity.SessionId,
                RoomId = entity.RoomId,
                OfferingId = entity.OfferingId,
                ExamDate = entity.ExamDate,
                Status = entity.Status
            };
        }

        public static Data.Entities.ExamSchedule ToEntity(this Domain.Entities.ExamSchedule domain)
        {
            return new Data.Entities.ExamSchedule
            {
                ExamScheduleId = domain.Id,
                SlotId = domain.SlotId,
                AcademyYearId = domain.AcademyYearId,
                SemesterId = domain.SemesterId,
                PeriodId = domain.PeriodId,
                SessionId = domain.SessionId,
                RoomId = domain.RoomId,
                OfferingId = domain.OfferingId,
                ExamDate = domain.ExamDate,
                Status = domain.Status
            };
        }
    }
}
