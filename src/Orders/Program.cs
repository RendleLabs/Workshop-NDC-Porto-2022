using Orders.Ingredients.Protos;
using Orders.PubSub;
using Orders.Services;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

var ingredientsUri = builder.Configuration.GetServiceUri("Ingredients")
                     ?? new Uri("https://localhost:5003");

builder.Services.AddGrpcClient<IngredientsService.IngredientsServiceClient>(options =>
{
    options.Address = ingredientsUri;
});

builder.Services.AddOrderPubSub();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<OrderServiceImpl>();
app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();
