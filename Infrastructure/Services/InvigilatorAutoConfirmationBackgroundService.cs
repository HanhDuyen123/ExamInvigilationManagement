using ExamInvigilationManagement.Application.Interfaces.Repositories;

namespace ExamInvigilationManagement.Infrastructure.Services;

public sealed class InvigilatorAutoConfirmationBackgroundService : BackgroundService
{
    private static readonly TimeSpan ResponseWindow = TimeSpan.FromHours(48);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvigilatorAutoConfirmationBackgroundService> _logger;

    public InvigilatorAutoConfirmationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InvigilatorAutoConfirmationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredConfirmationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể tự động xác nhận lịch coi thi quá hạn.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredConfirmationsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IInvigilatorResponseRepository>();

        var confirmedCount = await repository.AutoConfirmExpiredAsync(ResponseWindow, cancellationToken);
        if (confirmedCount > 0)
            _logger.LogInformation("Đã tự động xác nhận {ConfirmedCount} lịch coi thi quá hạn phản hồi.", confirmedCount);
    }
}
