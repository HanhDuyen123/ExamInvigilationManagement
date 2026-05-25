using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamInvigilationManagement.Infrastructure.Data.Entities;

[Table("OutboxMessage")]
public partial class OutboxMessage
{
    [Key]
    public long OutboxMessageId { get; set; }

    [StringLength(100)]
    public string Type { get; set; } = null!;

    public string Payload { get; set; } = null!;

    [StringLength(30)]
    public string Status { get; set; } = null!;

    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid CorrelationId { get; set; }
}
