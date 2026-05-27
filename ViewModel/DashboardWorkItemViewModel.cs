namespace ExamInvigilationManagement.ViewModel;

public class DashboardWorkItemViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Url { get; set; } = "#";
    public string Icon { get; set; } = "bi-info-circle";
    public string Tone { get; set; } = "primary";
    public string? BadgeText { get; set; }
}

public class DashboardIndexViewModel
{
    public string RoleName { get; set; } = string.Empty;
    public List<DashboardWorkItemViewModel> WorkItems { get; set; } = new();
}
