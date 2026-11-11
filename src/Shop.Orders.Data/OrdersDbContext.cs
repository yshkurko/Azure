using Microsoft.EntityFrameworkCore;

namespace Shop.Orders.Data;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public const string Schema = "orders";

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Order>(order =>
        {
            order.Property(o => o.CustomerEmail).HasMaxLength(256);
            order.Property(o => o.Status).HasConversion<string>().HasMaxLength(32);
            order.Property(o => o.Total).HasPrecision(18, 2);
            order.HasMany(o => o.Items).WithOne().HasForeignKey(i => i.OrderId);
            order.HasIndex(o => o.CreatedAt);
        });

        modelBuilder.Entity<OrderItem>(item =>
        {
            item.Property(i => i.ProductName).HasMaxLength(200);
            item.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });
    }
}
