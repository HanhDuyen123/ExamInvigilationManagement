using ExamInvigilationManagement.Application.DTOs.Import;
using Microsoft.AspNetCore.Http;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IBulkImportService
    {
        IReadOnlyList<string> SupportedModules { get; }
        string GetModuleTitle(string module);
        string GetBackUrl(string module);
        List<ImportColumnDto> GetTemplateColumns(string module);
        byte[] BuildTemplate(string module);
        Task<ImportResultDto> ImportAsync(string module, IFormFile file, int currentUserId, string currentRole, CancellationToken cancellationToken = default);
    }
}
