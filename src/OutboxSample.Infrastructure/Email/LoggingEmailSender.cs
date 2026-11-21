using Microsoft.Extensions.Logging;
using OutboxSample.Application.Orders;

namespace OutboxSample.Infrastructure.Email;

/// <summary>Stands in for a real provider: writes the email to the log instead of sending it.</summary>
internal sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendOrderConfirmationAsync(OrderPlaced order, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order confirmation for {OrderId} sent to {CustomerEmail}, total {Total}",
            order.OrderId, order.CustomerEmail, order.Total);

        return Task.CompletedTask;
    }
}
