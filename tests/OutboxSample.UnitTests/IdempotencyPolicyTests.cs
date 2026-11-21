using OutboxSample.Application.Idempotency;
using OutboxSample.Application.Orders;
using Xunit;

namespace OutboxSample.UnitTests;

public class IdempotencyPolicyTests
{
    [Fact]
    public void Unknown_key_is_executed()
    {
        var decision = IdempotencyPolicy.Decide(storedRequestHash: null, requestHash: "abc");

        Assert.Equal(IdempotencyDecision.Execute, decision);
    }

    [Fact]
    public void Known_key_with_the_same_request_is_replayed()
    {
        var decision = IdempotencyPolicy.Decide(storedRequestHash: "abc", requestHash: "abc");

        Assert.Equal(IdempotencyDecision.Replay, decision);
    }

    [Fact]
    public void Known_key_with_a_different_request_is_rejected()
    {
        var decision = IdempotencyPolicy.Decide(storedRequestHash: "abc", requestHash: "xyz");

        Assert.Equal(IdempotencyDecision.Reject, decision);
    }

    [Fact]
    public void Equal_requests_hash_the_same()
    {
        var first = RequestHash.Compute(Command(quantity: 2));
        var second = RequestHash.Compute(Command(quantity: 2));

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void A_changed_field_changes_the_hash()
    {
        var first = RequestHash.Compute(Command(quantity: 2));
        var second = RequestHash.Compute(Command(quantity: 3));

        Assert.NotEqual(first, second);
    }

    private static CreateOrderCommand Command(int quantity) =>
        new("key-1", "ada@example.com", [new CreateOrderLine("SKU-1", quantity, 19.90m)]);
}
