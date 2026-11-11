using Microsoft.EntityFrameworkCore;

namespace Shop.Catalog.Api.Data;

public sealed class Product
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public required string Description { get; set; }

    public required string Category { get; set; }

    public decimal Price { get; set; }
}

public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options)
{
    public const string Schema = "catalog";

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<Product>(product =>
        {
            product.Property(p => p.Name).HasMaxLength(200);
            product.Property(p => p.Description).HasMaxLength(1000);
            product.Property(p => p.Category).HasMaxLength(100);
            product.Property(p => p.Price).HasPrecision(18, 2);

            product.HasData(
                new Product { Id = 1, Name = "Клавиатура механическая", Description = "Переключатели Brown, подсветка", Category = "Периферия", Price = 89.90m },
                new Product { Id = 2, Name = "Мышь беспроводная", Description = "Bluetooth + 2.4 ГГц, 16000 DPI", Category = "Периферия", Price = 49.50m },
                new Product { Id = 3, Name = "Монитор 27\"", Description = "IPS, 2560x1440, 144 Гц", Category = "Мониторы", Price = 319.00m },
                new Product { Id = 4, Name = "Наушники", Description = "Закрытые, шумоподавление", Category = "Аудио", Price = 129.00m },
                new Product { Id = 5, Name = "Веб-камера", Description = "1080p, автофокус", Category = "Периферия", Price = 59.90m },
                new Product { Id = 6, Name = "SSD 1 ТБ", Description = "NVMe PCIe 4.0", Category = "Комплектующие", Price = 79.00m },
                new Product { Id = 7, Name = "Док-станция USB-C", Description = "HDMI, Ethernet, 3xUSB", Category = "Периферия", Price = 99.00m },
                new Product { Id = 8, Name = "Коврик для мыши XL", Description = "900x400 мм", Category = "Аксессуары", Price = 19.90m });
        });
    }
}
