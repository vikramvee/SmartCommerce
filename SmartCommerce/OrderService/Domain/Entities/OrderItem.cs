namespace OrderService.Domain.Entities;

public sealed class OrderItem
{
    public Guid ItemId { get; private set; }
// OrderItem.cs
    public required string ProductId { get; init; }
    public required string ProductName { get; init; }   
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal LineTotal => UnitPrice * Quantity;

    private OrderItem() { } // DynamoDB deserialization

    public static OrderItem Create(string productId, string productName, int quantity, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productId);
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(unitPrice);

        return new OrderItem
        {
            ItemId = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }

    internal static OrderItem Reconstitute(
    Guid itemId, string productId, string productName, int quantity, decimal unitPrice)
    {
        return new OrderItem
        {
            ItemId      = itemId,
            ProductId   = productId,
            ProductName = productName,
            Quantity    = quantity,
            UnitPrice   = unitPrice
        };
    }
}