using BankSecurityAlert.Infrastructure.Config;
using RabbitMQ.Client;

namespace BankSecurityAlert.Infrastructure.RabbitMQ;

/// <summary>
/// Creates the RabbitMQ connection and declares the full topology:
///   - 3 Exchanges: Topic, Fanout, Direct
///   - 4 Queues with their bindings
/// </summary>
public sealed class RabbitMQTopology : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel      _channel;

    public IModel Channel => _channel;

    public RabbitMQTopology()
    {
        var factory = new ConnectionFactory
        {
            HostName    = RabbitMQConstants.Host,
            Port        = RabbitMQConstants.Port,
            VirtualHost = RabbitMQConstants.VirtualHost,
            UserName    = RabbitMQConstants.Username,
            Password    = RabbitMQConstants.Password
        };

        _connection = factory.CreateConnection("BankSecurityAlert");
        _channel    = _connection.CreateModel();

        DeclareTopology();
    }

    private void DeclareTopology()
    {
        // ── 1. Declare Exchanges ──────────────────────────────────
        _channel.ExchangeDeclare(
            exchange: RabbitMQConstants.TopicExchange,
            type:     ExchangeType.Topic,
            durable:  true,
            autoDelete: false);

        _channel.ExchangeDeclare(
            exchange: RabbitMQConstants.FanoutExchange,
            type:     ExchangeType.Fanout,
            durable:  true,
            autoDelete: false);

        _channel.ExchangeDeclare(
            exchange: RabbitMQConstants.DirectExchange,
            type:     ExchangeType.Direct,
            durable:  true,
            autoDelete: false);

        // ── 2. Declare Queues ─────────────────────────────────────
        var queueArgs = new Dictionary<string, object>
        {
            { "x-message-ttl", 86_400_000 } // 24h TTL
        };

        _channel.QueueDeclare(RabbitMQConstants.FraudDetectionQueue,  durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueDeclare(RabbitMQConstants.AuditLogQueue,        durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueDeclare(RabbitMQConstants.DashboardFanoutQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        _channel.QueueDeclare(RabbitMQConstants.UserDirectQueue,      durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

        // ── 3. Bindings: Topic Exchange ───────────────────────────
        // FraudDetection queue receives: critical.* and high.*
        _channel.QueueBind(RabbitMQConstants.FraudDetectionQueue,
                           RabbitMQConstants.TopicExchange,
                           RabbitMQConstants.RoutingCriticalAll);

        _channel.QueueBind(RabbitMQConstants.FraudDetectionQueue,
                           RabbitMQConstants.TopicExchange,
                           RabbitMQConstants.RoutingHighAll);

        // AuditLog queue receives: *.frauddetection and *.loginattempt
        _channel.QueueBind(RabbitMQConstants.AuditLogQueue,
                           RabbitMQConstants.TopicExchange,
                           RabbitMQConstants.RoutingAnyFraud);

        _channel.QueueBind(RabbitMQConstants.AuditLogQueue,
                           RabbitMQConstants.TopicExchange,
                           RabbitMQConstants.RoutingAnyLogin);

        // ── 4. Bindings: Fanout Exchange ──────────────────────────
        // Dashboard receives ALL alerts (fanout ignores routing key)
        _channel.QueueBind(RabbitMQConstants.DashboardFanoutQueue,
                           RabbitMQConstants.FanoutExchange,
                           routingKey: string.Empty);

        // ── 5. Bindings: Direct Exchange ──────────────────────────
        // Will be bound dynamically per user in producer (see producer)
        // Pre-bind a demo user
        _channel.QueueBind(RabbitMQConstants.UserDirectQueue,
                           RabbitMQConstants.DirectExchange,
                           routingKey: $"{RabbitMQConstants.DirectUserPrefix}USR001");

        Console.WriteLine("[Topology] ✅ All exchanges, queues and bindings declared.");
    }

    public void Dispose()
    {
        _channel.Close();
        _connection.Close();
    }
}
