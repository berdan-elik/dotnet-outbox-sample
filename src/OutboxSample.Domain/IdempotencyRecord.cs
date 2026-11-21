namespace OutboxSample.Domain;

public sealed class IdempotencyRecord
{
    private IdempotencyRecord()
    {
    }

    public string Key { get; private set; } = null!;
    public string RequestHash { get; private set; } = null!;
    public string ResponseBody { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }

    public static IdempotencyRecord Create(string key, string requestHash, string responseBody, DateTimeOffset now) =>
        new()
        {
            Key = key,
            RequestHash = requestHash,
            ResponseBody = responseBody,
            CreatedAt = now
        };
}
