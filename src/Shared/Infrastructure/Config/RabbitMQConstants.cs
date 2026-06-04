
namespace BankSecurityAlert.Infrastructure.Config;

public static class RabbitMQConstants
{
    // Lee RABBITMQ_HOST del entorno; si no existe usa localhost (desarrollo local)
    public static string Host =>
        Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost";

    public const int    Port        = 5672;
    public const string VirtualHost = "bank-security";
    public const string Username    = "guest";
    public const string Password    = "guest";

    // Exchanges
    public const string TopicExchange  = "bank.alerts.topic";
    public const string FanoutExchange = "bank.alerts.fanout";
    public const string DirectExchange = "bank.alerts.direct";

    // Queues
    public const string FraudDetectionQueue  = "queue.fraud.detection";
    public const string AuditLogQueue        = "queue.audit.log";
    public const string DashboardFanoutQueue = "queue.dashboard.fanout";
    public const string UserDirectQueue      = "queue.user.direct";

    // Routing Keys
    public const string RoutingCriticalAll = "critical.#";
    public const string RoutingHighAll     = "high.#";
    public const string RoutingAnyFraud    = "*.frauddetection";
    public const string RoutingAnyLogin    = "*.loginattempt";
    public const string DirectUserPrefix   = "user.";
}
