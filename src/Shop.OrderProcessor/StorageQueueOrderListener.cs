using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using Shop.Shared.Contracts;
using Shop.Shared.Messaging;

namespace Shop.OrderProcessor;

/// <summary>Опрашивает Azure Storage Queue. Неудачные сообщения после MaxDequeueCount попыток уходят в очередь "*-poison".</summary>
public sealed class StorageQueueOrderListener(
    QueueClient queueClient,
    IOptions<MessagingOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<StorageQueueOrderListener> logger) : BackgroundService
{
    private const int MaxDequeueCount = 5;
    private static readonly TimeSpan EmptyQueueDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan VisibilityTimeout = TimeSpan.FromSeconds(60);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var poisonQueue = MessagingServiceCollectionExtensions.CreateQueueClient(options.Value, $"{options.Value.QueueName}-poison");

        logger.LogInformation("Listening Storage Queue {Queue}", queueClient.Name);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var messages = (await queueClient.ReceiveMessagesAsync(16, VisibilityTimeout, stoppingToken)).Value;

                if (messages.Length == 0)
                {
                    await Task.Delay(EmptyQueueDelay, stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    if (message.DequeueCount > MaxDequeueCount)
                    {
                        logger.LogError("Message {MessageId} moved to poison queue", message.MessageId);
                        await poisonQueue.SendMessageAsync(message.Body, cancellationToken: stoppingToken);
                        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var payload = message.Body.ToObjectFromJson<OrderSubmitted>(JsonSerializerOptions.Web)
                            ?? throw new InvalidOperationException("Empty message body.");

                        await using var scope = scopeFactory.CreateAsyncScope();
                        await scope.ServiceProvider.GetRequiredService<OrderProcessingService>().ProcessAsync(payload, stoppingToken);

                        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        // Сообщение не удаляем: оно снова станет видимым после VisibilityTimeout.
                        logger.LogError(ex, "Failed to process message {MessageId}, attempt {Attempt}", message.MessageId, message.DequeueCount);
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Queue polling failed");
                await Task.Delay(EmptyQueueDelay, stoppingToken);
            }
        }
    }
}
