namespace Shop.Shared.Messaging;

public enum MessagingProvider
{
    StorageQueue,
    ServiceBus
}

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";

    public MessagingProvider Provider { get; set; } = MessagingProvider.StorageQueue;

    public string QueueName { get; set; } = "orders";

    public StorageQueueOptions StorageQueue { get; set; } = new();

    public ServiceBusOptions ServiceBus { get; set; } = new();
}

public sealed class StorageQueueOptions
{
    /// <summary>Локально: "UseDevelopmentStorage=true" (Azurite). В Azure оставить пустым и задать ServiceUri.</summary>
    public string? ConnectionString { get; set; }

    /// <summary>Например https://stshop.queue.core.windows.net — доступ через Managed Identity.</summary>
    public string? ServiceUri { get; set; }
}

public sealed class ServiceBusOptions
{
    public string? ConnectionString { get; set; }

    /// <summary>Например sb-shop.servicebus.windows.net — доступ через Managed Identity.</summary>
    public string? FullyQualifiedNamespace { get; set; }
}
