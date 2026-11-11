using Shop.OrderProcessor;
using Shop.Orders.Data;
using Shop.Shared.Hosting;
using Shop.Shared.Messaging;

// Это веб-приложение (а не чистый Worker), чтобы у него был /health:
// App Service и Kubernetes используют его для проверки, что процесс жив.
var builder = WebApplication.CreateBuilder(args);

builder.AddShopTelemetry();

builder.Services.AddHealthChecks();
builder.Services.AddOrdersDb(builder.Configuration.GetConnectionString("ShopDb"));
builder.Services.AddShopMessaging(builder.Configuration);
builder.Services.AddScoped<OrderProcessingService>();

var provider = builder.Configuration.GetValue<MessagingProvider>("Messaging:Provider");
if (provider == MessagingProvider.ServiceBus)
{
    builder.Services.AddHostedService<ServiceBusOrderListener>();
}
else
{
    builder.Services.AddHostedService<StorageQueueOrderListener>();
}

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/", () => $"Shop.OrderProcessor is running ({provider}).");

app.Run();
