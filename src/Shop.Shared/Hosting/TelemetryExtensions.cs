using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Shop.Shared.Hosting;

public static class TelemetryExtensions
{
    /// <summary>Включает отправку телеметрии в Application Insights, только если задана строка подключения.</summary>
    public static IHostApplicationBuilder AddShopTelemetry(this IHostApplicationBuilder builder)
    {
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        {
            builder.Services.AddOpenTelemetry().UseAzureMonitor();
        }

        return builder;
    }
}
