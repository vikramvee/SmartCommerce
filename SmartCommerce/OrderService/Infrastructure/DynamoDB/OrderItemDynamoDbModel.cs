public sealed class OrderItemDynamoDbModel
{
    public string ItemId { get; set; } = default!;
    public string ProductId { get; set; } = default!;
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}