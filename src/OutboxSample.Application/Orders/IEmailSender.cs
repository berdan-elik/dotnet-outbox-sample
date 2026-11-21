namespace OutboxSample.Application.Orders;

public interface IEmailSender
{
    Task SendOrderConfirmationAsync(OrderPlaced order, CancellationToken cancellationToken);
}
