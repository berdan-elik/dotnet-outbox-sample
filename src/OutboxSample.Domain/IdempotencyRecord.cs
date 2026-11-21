namespace OutboxSample.Domain;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public string Key { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public int StatusCode { get; private set; }
    public string ResponseBody { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static IdempotencyRecord Create(
        string key, string requestHash, int statusCode, string responseBody, DateTimeOffset now) =>
        new()
        {
            Key = key,
            RequestHash = requestHash,
            StatusCode = statusCode,
            ResponseBody = responseBody,
            CreatedAt = now
        };
}
