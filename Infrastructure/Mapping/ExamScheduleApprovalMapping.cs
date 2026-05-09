namespace ExamInvigilationManagement.Infrastructure.Mapping
{
    public static class ExamScheduleApprovalMapping
    {
        public static Domain.Entities.ExamScheduleApproval ToDomain(this Data.Entities.ExamScheduleApproval entity)
        {
            return new Domain.Entities.ExamScheduleApproval
            {
                Id = entity.ApprovalId,
                ExamScheduleId = entity.ExamScheduleId,
                ApproverId = entity.ApproverId,
                Status = entity.Status,
                Note = entity.Note,
                ApproveAt = entity.ApproveAt,
                UpdateAt = entity.UpdateAt
            };
        }

        public static Data.Entities.ExamScheduleApproval ToEntity(this Domain.Entities.ExamScheduleApproval domain)
        {
            return new Data.Entities.ExamScheduleApproval
            {
                ApprovalId = domain.Id,
                ExamScheduleId = domain.ExamScheduleId,
                ApproverId = domain.ApproverId,
                Status = domain.Status,
                Note = domain.Note,
                ApproveAt = domain.ApproveAt,
                UpdateAt = domain.UpdateAt
            };
        }
    }
}
