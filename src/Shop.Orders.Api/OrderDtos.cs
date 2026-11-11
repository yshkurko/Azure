using Shop.Orders.Data;

namespace Shop.Orders.Api;

public sealed record CreateOrderItemRequest(int ProductId, int Quantity);

public sealed record CreateOrderRequest(string CustomerEmail, List<CreateOrderItemRequest> Items);

public sealed record OrderItemResponse(int ProductId, string ProductName, decimal UnitPrice, int Quantity);

public sealed record OrderResponse(
    Guid Id,
    string CustomerEmail,
    string Status,
    decimal Total,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(Order order) => new(
        order.Id,
        order.CustomerEmail,
        order.Status.ToString(),
        order.Total,
        order.CreatedAt,
        order.ProcessedAt,
        order.Items.Select(i => new OrderItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity)).ToList());
}
