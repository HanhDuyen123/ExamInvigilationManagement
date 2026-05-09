namespace ExamInvigilationManagement.ViewModel
{
    public class LookupFieldViewModel
    {
        public string? Label { get; set; }
        public string InputId { get; set; } = default!;
        public string HiddenId { get; set; } = default!;
        public string HiddenName { get; set; } = default!;
        public string MenuId { get; set; } = default!;
        public string? ButtonId { get; set; }
        public string Placeholder { get; set; } = "Chọn...";
        public string InputClass { get; set; } = "input-modern";
        public bool ShowButton { get; set; } = false;
        public string? InitialText { get; set; }
        public string? InitialValue { get; set; }
    }
}
