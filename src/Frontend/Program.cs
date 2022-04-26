using AuthHelp;
using Frontend.Auth;
using Frontend.Ingredients.Protos;
using Grpc.Core;
using Orders.Protos;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

var ingredientsUri = builder.Configuration.GetServiceUri("Ingredients")
                     ?? new Uri("https://localhost:5003");

builder.Services.AddHttpClient("ingredients")
    .ConfigurePrimaryHttpMessageHandler(DevelopmentModeCertificateHelper.CreateClientHandler);

builder.Services.AddGrpcClient<IngredientsService.IngredientsServiceClient>(options =>
    {
        options.Address = ingredientsUri;
    })
    .ConfigureChannel((provider, channel) =>
    {
        channel.HttpHandler = null;
        channel.HttpClient = provider.GetRequiredService<IHttpClientFactory>()
            .CreateClient("ingredients");
        channel.DisposeHttpClient = true;
    });

var ordersUri = builder.Configuration.GetServiceUri("Orders")
                ?? new Uri("https://localhost:5005");

builder.Services.AddGrpcClient<OrderService.OrderServiceClient>(options => { options.Address = ordersUri; })
    .ConfigureChannel((provider, channel) =>
    {
        var authHelper = provider.GetRequiredService<AuthHelper>();
        var callCredentials = CallCredentials.FromInterceptor(async (context, metadata) =>
        {
            var token = await authHelper.GetTokenAsync();
            metadata.Add("Authorization", $"Bearer {token}");
        });
        channel.Credentials = ChannelCredentials.Create(ChannelCredentials.SecureSsl, callCredentials);
    });
// .ConfigureHttpClient((provider, client) =>
// {
//     // You can also configure authorization on the HttpClient
//     var authHelper = provider.GetRequiredService<AuthHelper>();
//     var token = authHelper.GetTokenAsync().GetAwaiter().GetResult();
//     client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
// })

builder.Services.AddHttpClient<AuthHelper>()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = ordersUri;
        client.DefaultRequestVersion = new Version(2, 0);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();