namespace OrderService.Infrastructure.Idempotency;

public sealed class IdempotencySettings
{
    public const string SectionName = "Idempotency";
    public string TableName { get; init; } = "SmartCommerce_Idempotency";
    public int TtlDays { get; init; } = 7;  // auto-expire records after 7 days
}