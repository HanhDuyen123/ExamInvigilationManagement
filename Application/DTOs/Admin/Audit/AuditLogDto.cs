namespace ExamInvigilationManagement.Application.DTOs.Admin.Audit;

public class AuditLogDto
{
    public long Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? ActorUserId { get; set; }
    public string? ActorUserName { get; set; }
    public string? ActorFullName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Note { get; set; }
    public Guid CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Source { get; set; }
}
