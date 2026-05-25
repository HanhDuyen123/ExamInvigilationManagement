using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AssignmentChangeHistory")]
public partial class AssignmentChangeHistory
{
    [Key]
    public long AssignmentChangeHistoryId { get; set; }

    public int? ExamInvigilatorId { get; set; }
    public int ExamScheduleId { get; set; }
    public int? OldAssigneeId { get; set; }
    public int? NewAssigneeId { get; set; }
    public byte? PositionNo { get; set; }

    [StringLength(50)]
    public string ChangeType { get; set; } = null!;

    public string? Reason { get; set; }
    public int? ActorUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CorrelationId { get; set; }
}
