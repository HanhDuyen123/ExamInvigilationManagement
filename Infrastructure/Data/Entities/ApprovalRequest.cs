using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("ApprovalRequest")]
public partial class ApprovalRequest
{
    [Key]
    public int ApprovalRequestId { get; set; }

    public int RequestedById { get; set; }

    public int? FacultyId { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = null!;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid CorrelationId { get; set; }
}
