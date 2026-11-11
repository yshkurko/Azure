using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Shop.Catalog.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            migrationBuilder.CreateTable(
                name: "Products",
                schema: "catalog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "Products",
                columns: new[] { "Id", "Category", "Description", "Name", "Price" },
                values: new object[,]
                {
                    { 1, "Периферия", "Переключатели Brown, подсветка", "Клавиатура механическая", 89.90m },
                    { 2, "Периферия", "Bluetooth + 2.4 ГГц, 16000 DPI", "Мышь беспроводная", 49.50m },
                    { 3, "Мониторы", "IPS, 2560x1440, 144 Гц", "Монитор 27\"", 319.00m },
                    { 4, "Аудио", "Закрытые, шумоподавление", "Наушники", 129.00m },
                    { 5, "Периферия", "1080p, автофокус", "Веб-камера", 59.90m },
                    { 6, "Комплектующие", "NVMe PCIe 4.0", "SSD 1 ТБ", 79.00m },
                    { 7, "Периферия", "HDMI, Ethernet, 3xUSB", "Док-станция USB-C", 99.00m },
                    { 8, "Аксессуары", "900x400 мм", "Коврик для мыши XL", 19.90m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products",
                schema: "catalog");
        }
    }
}
