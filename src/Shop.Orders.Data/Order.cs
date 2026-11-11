namespace Shop.Orders.Data;

public enum OrderStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public sealed class Order
{
    public Guid Id { get; set; }

    public required string CustomerEmail { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public decimal Total { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
}

public sealed class OrderItem
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public int ProductId { get; set; }

    public required string ProductName { get; set; }

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }
}
