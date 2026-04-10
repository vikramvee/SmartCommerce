namespace OrderService.Domain.Entities;

public sealed class OrderItem
{
    public Guid ItemId { get; private set; }
    public string ProductId { get; private set; }
    public string ProductName { get; private set; }
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
}