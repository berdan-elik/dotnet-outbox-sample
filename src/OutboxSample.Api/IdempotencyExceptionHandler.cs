using Microsoft.AspNetCore.Diagnostics;
using OutboxSample.Application.Idempotency;

namespace OutboxSample.Api;

public sealed class IdempotencyExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not IdempotencyKeyReusedException)
            return false;

        await Results
            .Problem(exception.Message, statusCode: StatusCodes.Status422UnprocessableEntity)
            .ExecuteAsync(httpContext);
        return true;
    }
}
