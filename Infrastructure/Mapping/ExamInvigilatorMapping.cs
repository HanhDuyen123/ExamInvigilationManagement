namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamInvigilatorMapping
    {
        public static Domain.Entities.ExamInvigilator ToDomain(this Data.Entities.ExamInvigilator entity)
        {
            return new Domain.Entities.ExamInvigilator
            {
                Id = entity.ExamInvigilatorId,
                AssigneeId = entity.AssigneeId,
                AssignerId = entity.AssignerId,
                NewAssigneeId = entity.NewAssigneeId,
                ExamScheduleId = entity.ExamScheduleId,
                PositionNo = entity.PositionNo,
                Status = entity.Status,
                CreateAt = entity.CreateAt,
                UpdateAt = entity.UpdateAt
            };
        }

        public static Data.Entities.ExamInvigilator ToEntity(this Domain.Entities.ExamInvigilator domain)
        {
            return new Data.Entities.ExamInvigilator
            {
                ExamInvigilatorId = domain.Id,
                AssigneeId = domain.AssigneeId,
                AssignerId = domain.AssignerId,
                NewAssigneeId = domain.NewAssigneeId,
                ExamScheduleId = domain.ExamScheduleId,
                PositionNo = domain.PositionNo,
                Status = domain.Status,
                CreateAt = domain.CreateAt,
                UpdateAt = domain.UpdateAt
            };
        }
    }
}
