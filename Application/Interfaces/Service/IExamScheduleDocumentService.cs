using ExamInvigilationManagement.Application.DTOs.ExamSchedule;
using ExamInvigilationManagement.Application.DTOs.Import;

namespace ExamInvigilationManagement.Application.Interfaces.Service
{
    public interface IExamScheduleDocumentService
    {
        byte[] BuildExamScheduleExportExcel(IReadOnlyList<ExamScheduleDto> schedules, string? templatePath = null);
        byte[] BuildSupportRequestExcel(ExamScheduleSupportRequestDocumentDto request, byte[] templateBytes);
        string BuildSupportRequestFileName(ExamScheduleSupportRequestDocumentDto request);
        string BuildSupportRequestEmailBody(ExamScheduleSupportRequestDocumentDto request, string? replyTo);
        Task<byte[]> GetSupportTemplateBytesAsync(ImportFileDto? uploadedTemplate, string defaultTemplatePath);
    }
}
