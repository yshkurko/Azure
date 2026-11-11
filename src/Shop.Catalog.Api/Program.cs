using Microsoft.EntityFrameworkCore;
using Shop.Catalog.Api.Data;
using Shop.Shared.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddShopTelemetry();

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ShopDb"), sql =>
    {
        // Своя таблица истории миграций, т.к. Catalog и Orders живут в одной базе.
        sql.MigrationsHistoryTable("__EFMigrationsHistory", CatalogDbContext.Schema);
        sql.EnableRetryOnFailure();
    }));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
}

app.UseExceptionHandler();
app.UseCors();

app.MapOpenApi();
app.MapHealthChecks("/health");

var products = app.MapGroup("/api/products");

products.MapGet("/", async (CatalogDbContext db, string? category, CancellationToken ct) =>
{
    var query = db.Products.AsNoTracking();
    if (!string.IsNullOrWhiteSpace(category))
    {
        query = query.Where(p => p.Category == category);
    }

    return await query.OrderBy(p => p.Id).ToListAsync(ct);
});

products.MapGet("/{id:int}", async (int id, CatalogDbContext db, CancellationToken ct) =>
    await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct) is { } product
        ? Results.Ok(product)
        : Results.NotFound());

app.Run();
