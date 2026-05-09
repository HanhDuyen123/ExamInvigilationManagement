namespace ExamInvigilationManagement.ViewModel
{
    public class CrudIndexViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string CreateUrl { get; set; }

        // dùng render search form riêng
        public string SearchPartialView { get; set; }
        public string TableClass { get; set; } = "";
        public bool ShowCreateButton { get; set; } = true;
        public string? ImportUrl { get; set; }
        public string? TemplateUrl { get; set; }
    }
}
