using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Shop.Shared.Messaging;

public sealed class ServiceBusPublisher(ServiceBusSender sender) : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
    {
        var body = BinaryData.FromObjectAsJson(message, JsonSerializerOptions.Web);
        var sbMessage = new ServiceBusMessage(body)
        {
            ContentType = "application/json",
            Subject = typeof(T).Name
        };
        await sender.SendMessageAsync(sbMessage, cancellationToken);
    }
}
