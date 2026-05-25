using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ApprovalHistory")]
public partial class ApprovalHistory
{
    [Key]
    public long ApprovalHistoryId { get; set; }

    public int? ApprovalRequestId { get; set; }
    public int? ApprovalId { get; set; }
    public int ExamScheduleId { get; set; }
    public int ActorUserId { get; set; }

    [StringLength(30)]
    public string? FromStatus { get; set; }

    [StringLength(30)]
    public string ToStatus { get; set; } = null!;

    [StringLength(50)]
    public string Action { get; set; } = null!;

    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CorrelationId { get; set; }

    [ForeignKey("ApprovalRequestId")]
    public virtual ApprovalRequest? ApprovalRequest { get; set; }
}
