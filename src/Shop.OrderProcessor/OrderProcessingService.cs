using Microsoft.EntityFrameworkCore;
using Shop.Orders.Data;
using Shop.Shared.Contracts;

namespace Shop.OrderProcessor;

public sealed class OrderProcessingService(OrdersDbContext db, ILogger<OrderProcessingService> logger)
{
    public async Task ProcessAsync(OrderSubmitted message, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == message.OrderId, cancellationToken);

        if (order is null)
        {
            logger.LogWarning("Order {OrderId} not found, message skipped", message.OrderId);
            return;
        }

        // Сообщение может прийти повторно (at-least-once), поэтому обработка должна быть идемпотентной.
        if (order.Status is OrderStatus.Completed)
        {
            logger.LogInformation("Order {OrderId} already completed", order.Id);
            return;
        }

        order.Status = OrderStatus.Processing;
        await db.SaveChangesAsync(cancellationToken);

        // Имитация работы: резерв на складе, оплата, письмо клиенту и т.п.
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        order.Status = OrderStatus.Completed;
        order.ProcessedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Order {OrderId} processed, total {Total}", order.Id, order.Total);
    }
}
