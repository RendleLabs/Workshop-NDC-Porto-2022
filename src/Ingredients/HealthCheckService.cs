using Grpc.Health.V1;
using Grpc.HealthCheck;
using Ingredients.Data;

namespace Ingredients;

public class HealthCheckService : BackgroundService
{
    private readonly IToppingData _data;
    private readonly HealthServiceImpl _health;

    public HealthCheckService(IToppingData data, HealthServiceImpl health)
    {
        _data = data;
        _health = health;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var _ = await _data.GetAsync(stoppingToken);
                _health.SetStatus("Ingredients", HealthCheckResponse.Types.ServingStatus.Serving);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _health.SetStatus("Ingredients", HealthCheckResponse.Types.ServingStatus.NotServing);
            }
        }
    }
}