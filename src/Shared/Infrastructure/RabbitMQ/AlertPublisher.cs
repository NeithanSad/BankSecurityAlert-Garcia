using System.Text;
using System.Text.Json;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using RabbitMQ.Client;

namespace BankSecurityAlert.Infrastructure.RabbitMQ;

/// <summary>
/// Publishes a SecurityAlert to all three exchange types simultaneously:
///   1. Topic Exchange  — routed by "severity.category" pattern
///   2. Fanout Exchange — broadcast to ALL consumers
///   3. Direct Exchange — targeted to specific user queue
/// </summary>
public sealed class AlertPublisher
{
    private readonly IModel _channel;

    public AlertPublisher(IModel channel)
    {
        _channel = channel;
    }

    public void Publish(SecurityAlert alert)
    {
        var body       = Encode(alert);
        var properties = BuildProperties(alert);

        // ── 1. Topic Exchange ─────────────────────────────────────
        _channel.BasicPublish(
            exchange:   RabbitMQConstants.TopicExchange,
            routingKey: alert.TopicRoutingKey,
            basicProperties: properties,
            body:       body);

        Console.WriteLine($"  [→ Topic]  key={alert.TopicRoutingKey}");

        // ── 2. Fanout Exchange ────────────────────────────────────
        _channel.BasicPublish(
            exchange:   RabbitMQConstants.FanoutExchange,
            routingKey: string.Empty,   // fanout ignores routing key
            basicProperties: properties,
            body:       body);

        Console.WriteLine($"  [→ Fanout] broadcast");

        // ── 3. Direct Exchange ────────────────────────────────────
        // Only send direct for High/Critical alerts so the user gets
        // an immediate personal notification
        if (alert.Severity is AlertSeverity.High or AlertSeverity.Critical)
        {
            _channel.BasicPublish(
                exchange:   RabbitMQConstants.DirectExchange,
                routingKey: alert.DirectRoutingKey,
                basicProperties: properties,
                body:       body);

            Console.WriteLine($"  [→ Direct] key={alert.DirectRoutingKey}");
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static ReadOnlyMemory<byte> Encode(SecurityAlert alert)
    {
        var json = JsonSerializer.Serialize(alert, new JsonSerializerOptions
        {
            WriteIndented = false
        });
        return Encoding.UTF8.GetBytes(json);
    }

    private IBasicProperties BuildProperties(SecurityAlert alert)
    {
        var props = _channel.CreateBasicProperties();
        props.Persistent    = true;
        props.ContentType   = "application/json";
        props.MessageId     = alert.Id.ToString();
        props.Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        props.Headers       = new Dictionary<string, object>
        {
            { "severity", alert.Severity.ToString() },
            { "category", alert.Category.ToString() },
            { "user-id",  alert.UserId }
        };
        return props;
    }
}
