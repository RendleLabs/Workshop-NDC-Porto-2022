using Grpc.Core;
using Orders.Protos;

namespace ShopConsole;

public class Worker : BackgroundService
{
    private readonly OrderService.OrderServiceClient _orders;
    private readonly ILogger<Worker> _logger;

    public Worker(OrderService.OrderServiceClient orders, ILogger<Worker> logger)
    {
        _orders = orders;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var call = _orders.Subscribe(new SubscribeRequest(), cancellationToken: stoppingToken);

                await foreach (var notification in call.ResponseStream.ReadAllAsync(stoppingToken))
                {
                    Console.WriteLine($"Order: {notification.CrustId}");
                    foreach (var toppingId in notification.ToppingIds)
                    {
                        Console.WriteLine($"    {toppingId}");
                    }

                    Console.WriteLine($"DUE BY: {notification.Time.ToDateTimeOffset():t}");
                }
            }
            catch (OperationCanceledException)
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
