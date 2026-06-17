using ExamInvigilationManagement.Application.Interfaces.Common;

namespace ExamInvigilationManagement.Infrastructure.Services;

public class TemplatePathService : ITemplatePathService
{
    private readonly IWebHostEnvironment _environment;

    public TemplatePathService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string GetTemplatePath(string fileName)
    {
        return Path.Combine(_environment.WebRootPath, "templates", fileName);
    }
}
