namespace Shop.Shared.Contracts;

/// <summary>Сообщение, которое Orders API кладёт в очередь после создания заказа.</summary>
public sealed record OrderSubmitted(Guid OrderId, DateTimeOffset SubmittedAt);
