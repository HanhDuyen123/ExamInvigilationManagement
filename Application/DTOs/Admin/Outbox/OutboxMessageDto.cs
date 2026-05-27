namespace ExamInvigilationManagement.Application.DTOs.Admin.Outbox;

public class OutboxMessageDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid CorrelationId { get; set; }
    public string Payload { get; set; } = string.Empty;
}
