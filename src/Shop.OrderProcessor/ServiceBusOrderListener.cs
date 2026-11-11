using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using Shop.Shared.Contracts;
using Shop.Shared.Messaging;

namespace Shop.OrderProcessor;

/// <summary>Получает сообщения из Service Bus. После MaxDeliveryCount неудач Service Bus сам переносит сообщение в dead-letter очередь.</summary>
public sealed class ServiceBusOrderListener(
    ServiceBusClient client,
    IOptions<MessagingOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceBusOrderListener> logger) : BackgroundService
{
    private ServiceBusProcessor? _processor;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _processor = client.CreateProcessor(options.Value.QueueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 4
        });

        _processor.ProcessMessageAsync += HandleMessageAsync;
        _processor.ProcessErrorAsync += args =>
        {
            logger.LogError(args.Exception, "Service Bus error ({Source})", args.ErrorSource);
            return Task.CompletedTask;
        };

        logger.LogInformation("Listening Service Bus queue {Queue}", options.Value.QueueName);
        await _processor.StartProcessingAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task HandleMessageAsync(ProcessMessageEventArgs args)
    {
        try
        {
            var payload = args.Message.Body.ToObjectFromJson<OrderSubmitted>(JsonSerializerOptions.Web)
                ?? throw new InvalidOperationException("Empty message body.");

            await using var scope = scopeFactory.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<OrderProcessingService>().ProcessAsync(payload, args.CancellationToken);

            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to process message {MessageId}, delivery {Count}", args.Message.MessageId, args.Message.DeliveryCount);
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            await _processor.StopProcessingAsync(cancellationToken);
            await _processor.DisposeAsync();
        }

        await base.StopAsync(cancellationToken);
    }
}
