using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using Shop.Orders.Api;
using Shop.Orders.Data;
using Shop.Shared.Contracts;
using Shop.Shared.Hosting;
using Shop.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddShopTelemetry();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

builder.Services.AddOrdersDb(builder.Configuration.GetConnectionString("ShopDb"));
builder.Services.AddShopMessaging(builder.Configuration);

builder.Services.AddHttpClient<CatalogClient>(client =>
    client.BaseAddress = new Uri(builder.Configuration["Services:CatalogApiUrl"]
        ?? throw new InvalidOperationException("Services:CatalogApiUrl is not configured.")));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors();

app.MapOpenApi();
app.MapHealthChecks("/health");

var orders = app.MapGroup("/api/orders");

orders.MapGet("/", async (OrdersDbContext db, CancellationToken ct) =>
{
    var latest = await db.Orders.AsNoTracking()
        .Include(o => o.Items)
        .OrderByDescending(o => o.CreatedAt)
        .Take(50)
        .ToListAsync(ct);

    return latest.Select(OrderResponse.From);
});

orders.MapGet("/{id:guid}", async (Guid id, OrdersDbContext db, CancellationToken ct) =>
    await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct) is { } order
        ? Results.Ok(OrderResponse.From(order))
        : Results.NotFound());

orders.MapPost("/", async (
    CreateOrderRequest request,
    OrdersDbContext db,
    CatalogClient catalog,
    IMessagePublisher publisher,
    CancellationToken ct) =>
{
    var errors = Validate(request);
    if (errors.Count > 0)
    {
        return Results.ValidationProblem(errors);
    }

    var order = new Order
    {
        Id = Guid.CreateVersion7(),
        CustomerEmail = request.CustomerEmail.Trim(),
        CreatedAt = DateTimeOffset.UtcNow
    };

    // Цену берём из каталога, а не от клиента, чтобы её нельзя было подменить.
    foreach (var line in request.Items.GroupBy(i => i.ProductId))
    {
        var product = await catalog.GetProductAsync(line.Key, ct);
        if (product is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = [$"Product {line.Key} not found."]
            });
        }

        order.Items.Add(new OrderItem
        {
            ProductId = product.Id,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = line.Sum(i => i.Quantity)
        });
    }

    order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

    db.Orders.Add(order);
    await db.SaveChangesAsync(ct);

    await publisher.PublishAsync(new OrderSubmitted(order.Id, order.CreatedAt), ct);

    return Results.Created($"/api/orders/{order.Id}", OrderResponse.From(order));
});

app.Run();

static Dictionary<string, string[]> Validate(CreateOrderRequest request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.CustomerEmail) || !MailAddress.TryCreate(request.CustomerEmail.Trim(), out _))
    {
        errors["customerEmail"] = ["A valid email is required."];
    }

    if (request.Items is not { Count: > 0 and <= 50 })
    {
        errors["items"] = ["Order must contain from 1 to 50 items."];
    }
    else if (request.Items.Any(i => i.Quantity is < 1 or > 100))
    {
        errors["items"] = ["Quantity must be between 1 and 100."];
    }

    return errors;
}
