namespace ExamInvigilationManagement.Application.DTOs.Import
{
    public class ImportColumnDto
    {
        public string Key { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
    }

    public class ImportErrorDto
    {
        public int RowNumber { get; set; }
        public string Column { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ImportResultDto
    {
        public string Module { get; set; } = string.Empty;
        public string ModuleTitle { get; set; } = string.Empty;
        public int TotalRows { get; set; }
        public int InsertedRows { get; set; }
        public bool Success => Errors.Count == 0;
        public List<ImportErrorDto> Errors { get; set; } = new();
    }

    public class ImportPageDto
    {
        public string Module { get; set; } = string.Empty;
        public string ModuleTitle { get; set; } = string.Empty;
        public string BackUrl { get; set; } = string.Empty;
        public string TemplateUrl { get; set; } = string.Empty;
        public List<ImportColumnDto> Columns { get; set; } = new();
        public ImportResultDto? Result { get; set; }
    }
}
