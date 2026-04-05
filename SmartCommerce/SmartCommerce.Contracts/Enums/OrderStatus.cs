namespace SmartCommerce.Contracts.Enums;

public enum OrderStatus
{
    Pending,
    Confirmed,
    InventoryReserved,
    Shipped,
    Delivered,
    Cancelled,
    Failed
}