using TechStore.API.Services.Interfaces;

namespace TechStore.API.Services;

// Every few minutes: reminds customers about unpaid bank-transfer orders and cancels the ones
// past their deadline (OrderService.ProcessUnpaidOrdersAsync). It also runs as soon as the app
// starts, so it catches up if the app has been asleep.
public class UnpaidOrderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UnpaidOrderService> _logger;
    private readonly TimeSpan _interval;

    public UnpaidOrderService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<UnpaidOrderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromMinutes(config.GetValue("Orders:UnpaidCheckMinutes", 15));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderService>();
                var cancelled = await orders.ProcessUnpaidOrdersAsync();
                if (cancelled > 0)
                    _logger.LogInformation("Cancelled {Count} unpaid order(s)", cancelled);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unpaid order check failed");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
