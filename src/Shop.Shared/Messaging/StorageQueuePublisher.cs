using System.Text.Json;
using Azure.Storage.Queues;

namespace Shop.Shared.Messaging;

public sealed class StorageQueuePublisher(QueueClient queueClient) : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var body = JsonSerializer.Serialize(message, JsonSerializerOptions.Web);
        await queueClient.SendMessageAsync(body, cancellationToken);
    }
}
