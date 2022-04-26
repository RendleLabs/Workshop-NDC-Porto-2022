using System.Security.Claims;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.IIS.Core;
using Orders;
using Orders.Ingredients.Protos;
using Orders.Protos;
using Orders.PubSub;

namespace Orders.Services;

public class OrderServiceImpl : OrderService.OrderServiceBase
{
    private readonly IngredientsService.IngredientsServiceClient _ingredients;
    private readonly IOrderPublisher _orderPublisher;
    private readonly IOrderMessages _orderMessages;
    private readonly ILogger<OrderServiceImpl> _logger;

    public OrderServiceImpl(IngredientsService.IngredientsServiceClient ingredients,
        IOrderPublisher orderPublisher,
        IOrderMessages orderMessages,
        ILogger<OrderServiceImpl> logger)
    {
        _ingredients = ingredients;
        _orderPublisher = orderPublisher;
        _orderMessages = orderMessages;
        _logger = logger;
    }

    [Authorize]
    public override async Task<PlaceOrderResponse> PlaceOrder(PlaceOrderRequest request, ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        var name = user.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.Equals(name, "frontend", StringComparison.InvariantCultureIgnoreCase))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Only Frontend can place orders"));
        }
        
        var decrementToppingsRequest = new DecrementToppingsRequest
        {
            ToppingIds = {request.ToppingIds}
        };

        var toppingsTask = _ingredients.DecrementToppingsAsync(decrementToppingsRequest);

        var decrementCrustsRequest = new DecrementCrustsRequest
        {
            CrustId = request.CrustId
        };

        var crustsTask = _ingredients.DecrementCrustsAsync(decrementCrustsRequest);

        await Task.WhenAll(toppingsTask.ResponseAsync, crustsTask.ResponseAsync);

        await _orderPublisher.PublishOrder(request.CrustId, request.ToppingIds, DateTimeOffset.UtcNow.AddMinutes(30));

        return new PlaceOrderResponse
        {
            Time = DateTimeOffset.UtcNow.AddMinutes(30).ToTimestamp()
        };
    }

    public override async Task Subscribe(SubscribeRequest request,
        IServerStreamWriter<OrderNotification> responseStream,
        ServerCallContext context)
    {
        var token = context.CancellationToken;
        while (!token.IsCancellationRequested)
        {
            try
            {
                var message = await _orderMessages.ReadAsync(token);
                var notification = new OrderNotification
                {
                    CrustId = message.CrustId,
                    ToppingIds = {message.ToppingIds},
                    Time = message.Time.ToTimestamp()
                };

                try
                {
                    await responseStream.WriteAsync(notification);
                }
                catch
                {
                    await _orderPublisher.PublishOrder(message.CrustId, message.ToppingIds, message.Time);
                    throw;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }
    }
}