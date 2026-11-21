using System.Security.Cryptography;
using System.Text.Json;

namespace OutboxSample.Application.Idempotency;

public static class RequestHash
{
    // Hashing the deserialized request rather than the raw body means whitespace
    // and property order in the client's JSON do not change the hash.
    public static string Compute(object request)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(request, request.GetType());
        return Convert.ToHexStringLower(SHA256.HashData(json));
    }
}
