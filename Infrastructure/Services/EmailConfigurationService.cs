using ExamInvigilationManagement.Application.Interfaces.Service;
using Microsoft.Extensions.Options;

namespace ExamInvigilationManagement.Infrastructure.Services
{
    public class EmailConfigurationService : IEmailConfigurationService
    {
        private readonly EmailSettings _settings;

        public EmailConfigurationService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public string? GetSupportRequestRecipientEmail() => _settings.SupportRequestRecipientEmail;
    }
}
