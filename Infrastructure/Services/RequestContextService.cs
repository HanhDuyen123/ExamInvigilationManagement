using ExamInvigilationManagement.Application.Interfaces.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ExamInvigilationManagement.Infrastructure.Services;

public class RequestContextService : IRequestContextService
{
    private readonly IHttpContextAccessor _httpContext;
    private readonly IConfiguration _configuration;

    public RequestContextService(IHttpContextAccessor httpContext, IConfiguration configuration)
    {
        _httpContext = httpContext;
        _configuration = configuration;
    }

    public async Task SignOutAsync()
    {
        await _httpContext.HttpContext!.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    public string BuildAbsoluteUrl(string path)
    {
        var baseUrl = _configuration["App:BaseUrl"]?.Trim().TrimEnd('/');
        var requestContext = _httpContext.HttpContext?.Request;
        if (string.IsNullOrWhiteSpace(baseUrl) && requestContext != null)
        {
            baseUrl = $"{requestContext.Scheme}://{requestContext.Host}".TrimEnd('/');
        }

        return string.IsNullOrWhiteSpace(baseUrl)
            ? path
            : $"{baseUrl}/{path.TrimStart('/')}";
    }
}
