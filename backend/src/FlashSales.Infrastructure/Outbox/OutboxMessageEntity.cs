namespace FlashSales.Infrastructure.Outbox;

public class OutboxMessageEntity
{
    public long Id { get; set; }
    public string EventType { get; set; } = "";
    public string Payload { get; set; } = "";
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
