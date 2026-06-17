using ExamInvigilationManagement.Application.DTOs.Import;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IBulkImportService
    {
        IReadOnlyList<string> SupportedModules { get; }
        string GetModuleTitle(string module);
        string GetBackUrl(string module);
        List<ImportColumnDto> GetTemplateColumns(string module);
        byte[] BuildTemplate(string module);
        Task<ImportResultDto> ImportAsync(string module, ImportFileDto? file, int currentUserId, string currentRole, CancellationToken cancellationToken = default);
    }
}
