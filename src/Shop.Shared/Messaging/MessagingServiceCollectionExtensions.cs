using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Shop.Shared.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует клиент очереди (Storage Queue или Service Bus) в зависимости от Messaging:Provider.
    /// Если строка подключения пуста — используется DefaultAzureCredential (Managed Identity в Azure, az login локально).
    /// </summary>
    public static IServiceCollection AddShopMessaging(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MessagingOptions>(configuration.GetSection(MessagingOptions.SectionName));

        var options = configuration.GetSection(MessagingOptions.SectionName).Get<MessagingOptions>() ?? new MessagingOptions();

        switch (options.Provider)
        {
            case MessagingProvider.StorageQueue:
                services.AddSingleton(sp =>
                {
                    var o = sp.GetRequiredService<IOptions<MessagingOptions>>().Value;
                    return CreateQueueClient(o, o.QueueName);
                });
                services.AddSingleton<IMessagePublisher, StorageQueuePublisher>();
                break;

            case MessagingProvider.ServiceBus:
                services.AddSingleton(sp => CreateServiceBusClient(sp.GetRequiredService<IOptions<MessagingOptions>>().Value));
                services.AddSingleton(sp => sp.GetRequiredService<ServiceBusClient>()
                    .CreateSender(sp.GetRequiredService<IOptions<MessagingOptions>>().Value.QueueName));
                services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
                break;

            default:
                throw new InvalidOperationException($"Unknown messaging provider: {options.Provider}");
        }

        return services;
    }

    public static QueueClient CreateQueueClient(MessagingOptions options, string queueName)
    {
        var clientOptions = new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 };
        var storage = options.StorageQueue;

        if (!string.IsNullOrWhiteSpace(storage.ConnectionString))
        {
            // Локальный режим (Azurite): создаём очередь сами. В Azure очередь создаёт инфраструктура.
            var localClient = new QueueClient(storage.ConnectionString, queueName, clientOptions);
            localClient.CreateIfNotExists();
            return localClient;
        }

        if (string.IsNullOrWhiteSpace(storage.ServiceUri))
        {
            throw new InvalidOperationException("Set Messaging:StorageQueue:ConnectionString or Messaging:StorageQueue:ServiceUri.");
        }

        var queueUri = new Uri($"{storage.ServiceUri.TrimEnd('/')}/{queueName}");
        return new QueueClient(queueUri, new DefaultAzureCredential(), clientOptions);
    }

    private static ServiceBusClient CreateServiceBusClient(MessagingOptions options)
    {
        var sb = options.ServiceBus;

        if (!string.IsNullOrWhiteSpace(sb.ConnectionString))
        {
            return new ServiceBusClient(sb.ConnectionString);
        }

        if (string.IsNullOrWhiteSpace(sb.FullyQualifiedNamespace))
        {
            throw new InvalidOperationException("Set Messaging:ServiceBus:ConnectionString or Messaging:ServiceBus:FullyQualifiedNamespace.");
        }

        return new ServiceBusClient(sb.FullyQualifiedNamespace, new DefaultAzureCredential());
    }
}
