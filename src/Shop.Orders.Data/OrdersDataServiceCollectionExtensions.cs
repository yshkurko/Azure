using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Shop.Orders.Data;

public static class OrdersDataServiceCollectionExtensions
{
    public static IServiceCollection AddOrdersDb(this IServiceCollection services, string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ShopDb' is not configured.");
        }

        return services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                // Своя таблица истории миграций, т.к. Catalog и Orders живут в одной базе.
                sql.MigrationsHistoryTable("__EFMigrationsHistory", OrdersDbContext.Schema);
                sql.EnableRetryOnFailure();
            }));
    }
}
