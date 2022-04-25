using Grpc.Core;
using Orders;
using Orders.Protos;

namespace Orders.Services;

public class OrderServiceImpl : OrderService.OrderServiceBase
{
    private readonly ILogger<OrderServiceImpl> _logger;
    public OrderServiceImpl(ILogger<OrderServiceImpl> logger)
    {
        _logger = logger;
    }

    public override Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        return base.PlaceOrder(request, context);
    }
}
