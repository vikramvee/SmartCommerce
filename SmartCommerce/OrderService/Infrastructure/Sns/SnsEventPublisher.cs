using System.Text.Json;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Microsoft.Extensions.Options;
using OrderService.Application.Interfaces;
using OrderService.Domain.Common;

namespace OrderService.Infrastructure.Sns;

public sealed class SnsEventPublisher : IEventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly SnsSettings _settings;
    private readonly ILogger<SnsEventPublisher> _logger;

    public SnsEventPublisher(
        IAmazonSimpleNotificationService sns,
        IOptions<SnsSettings> settings,
        ILogger<SnsEventPublisher> logger)
    {
        _sns      = sns;
        _settings = settings.Value;
        _logger   = logger;
    }

    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

        var request = new PublishRequest
        {
            TopicArn = _settings.OrdersTopicArn,
            Message  = payload,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue
                {
                    DataType    = "String",
                    StringValue = domainEvent.EventType
                },
                ["TenantId"] = new MessageAttributeValue
                {
                    DataType    = "String",
                    StringValue = domainEvent is ITenantEvent te ? te.TenantId : "unknown"
                }
            }
        };

        var response = await _sns.PublishAsync(request, ct);

        _logger.LogInformation(
            "Published event {EventType} to SNS. MessageId: {MessageId}",
            domainEvent.EventType, response.MessageId);
    }
}