namespace OutboxSample.Domain;

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(line => line.Quantity * line.UnitPrice);

    public static Order Create(string customerEmail, IEnumerable<OrderLine> lines, DateTimeOffset now)
    {
        var order = new Order
        {
            Id = Guid.CreateVersion7(now),
            CustomerEmail = customerEmail,
            CreatedAt = now
        };
        order._lines.AddRange(lines);

        if (order._lines.Count == 0)
            throw new ArgumentException("An order needs at least one line.", nameof(lines));

        return order;
    }
}
