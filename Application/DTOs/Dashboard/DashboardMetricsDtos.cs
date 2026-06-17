namespace ExamInvigilationManagement.Application.DTOs.Dashboard;

public class DashboardMetricsDto
{
    public string RoleName { get; set; } = string.Empty;
    public DashboardAdminMetricsDto? Admin { get; set; }
    public DashboardSecretaryMetricsDto? Secretary { get; set; }
    public DashboardDeanMetricsDto? Dean { get; set; }
    public DashboardLecturerMetricsDto? Lecturer { get; set; }
}

public class DashboardAdminMetricsDto
{
    public int MissingInvigilatorSchedules { get; set; }
    public int FailedOutboxMessages { get; set; }
    public int ActiveUsers { get; set; }
}

public class DashboardSecretaryMetricsDto
{
    public int WaitingAssignSchedules { get; set; }
    public int OverdueAssignSchedules { get; set; }
    public int PendingSendApprovalSchedules { get; set; }
    public int OverdueApprovalSchedules { get; set; }
    public int ProposedSubstitutions { get; set; }
}

public class DashboardDeanMetricsDto
{
    public int PendingApprovals { get; set; }
    public int OverdueApprovals { get; set; }
}

public class DashboardLecturerMetricsDto
{
    public int PendingResponses { get; set; }
    public int OverdueResponses { get; set; }
    public int RejectedWithoutSubstitution { get; set; }
}
