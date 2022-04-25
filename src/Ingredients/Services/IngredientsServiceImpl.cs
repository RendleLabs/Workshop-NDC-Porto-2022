using Grpc.Core;
using Ingredients.Data;
using Ingredients.Protos;

namespace Ingredients.Services;

internal class IngredientsServiceImpl : Protos.IngredientsService.IngredientsServiceBase
{
    private readonly IToppingData _toppingData;
    private readonly ICrustData _crustData;
    private readonly ILogger<IngredientsServiceImpl> _logger;
    
    public IngredientsServiceImpl(ILogger<IngredientsServiceImpl> logger, IToppingData toppingData, ICrustData crustData)
    {
        _logger = logger;
        _toppingData = toppingData;
        _crustData = crustData;
    }

    public override async Task<GetToppingsResponse> GetToppings(
        GetToppingsRequest request, ServerCallContext context
            )
    {
        try
        {
            var toppings = await _toppingData.GetAsync(context.CancellationToken);

            var response = new GetToppingsResponse
            {
                Toppings =
                {
                    toppings.Select(t => new Topping
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Price = t.Price
                    })
                }
            };

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }
    }

    public override async Task<GetCrustsResponse> GetCrusts(GetCrustsRequest request, ServerCallContext context)
    {
        try
        {
            var crusts = await _crustData.GetAsync(context.CancellationToken);

            var response = new GetCrustsResponse
            {
                Crusts =
                {
                    crusts.Select(t => new Crust
                    {
                        Id = t.Id,
                        Name = t.Name,
                        Size = t.Size,
                        Price = t.Price
                    })
                }
            };

            return response;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, ex.Message, ex));
        }
    }

    public override async Task<DecrementToppingsResponse> DecrementToppings(DecrementToppingsRequest request, ServerCallContext context)
    {
        var tasks = request.ToppingIds
            .Select(id => _toppingData.DecrementStockAsync(id, context.CancellationToken));
        await Task.WhenAll(tasks);
        return new DecrementToppingsResponse();
    }

    public override async Task<DecrementCrustsResponse> DecrementCrusts(DecrementCrustsRequest request, ServerCallContext context)
    {
        await _crustData.DecrementStockAsync(request.CrustId, context.CancellationToken);
        return new DecrementCrustsResponse();
    }
}
