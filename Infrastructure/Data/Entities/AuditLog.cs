using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("AuditLog")]
public partial class AuditLog
{
    [Key]
    public long AuditLogId { get; set; }

    [StringLength(100)]
    public string EventType { get; set; } = null!;

    [StringLength(100)]
    public string EntityName { get; set; } = null!;

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(50)]
    public string Action { get; set; } = null!;

    public int? ActorUserId { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? Note { get; set; }

    public Guid CorrelationId { get; set; }

    public DateTime CreatedAt { get; set; }

    [StringLength(100)]
    public string? Source { get; set; }
}
