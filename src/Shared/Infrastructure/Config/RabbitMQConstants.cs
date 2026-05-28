namespace BankSecurityAlert.Infrastructure.Config;

public static class RabbitMQConstants
{
    // ── Connection ──────────────────────────────────────────────
    public const string Host         = "localhost";
    public const int    Port         = 5672;
    public const string VirtualHost  = "/";
    public const string Username     = "guest";
    public const string Password     = "guest";

    // ── Exchanges ────────────────────────────────────────────────
    /// <summary>Routes by severity.category pattern — Topic Exchange</summary>
    public const string TopicExchange  = "bank.alerts.topic";

    /// <summary>Broadcasts ALL alerts to every monitoring system — Fanout Exchange</summary>
    public const string FanoutExchange = "bank.alerts.fanout";

    /// <summary>Routes critical alerts directly to a specific user — Direct Exchange</summary>
    public const string DirectExchange = "bank.alerts.direct";

    // ── Queues ───────────────────────────────────────────────────
    /// <summary>Receives critical.# and high.# alerts (Topic)</summary>
    public const string FraudDetectionQueue = "queue.fraud.detection";

    /// <summary>Receives *.fraud and *.login patterns (Topic)</summary>
    public const string AuditLogQueue       = "queue.audit.log";

    /// <summary>Receives ALL alerts via Fanout</summary>
    public const string DashboardFanoutQueue = "queue.dashboard.fanout";

    /// <summary>Receives alerts for a specific user via Direct</summary>
    public const string UserDirectQueue     = "queue.user.direct";

    // ── Routing Keys ─────────────────────────────────────────────
    public const string RoutingCriticalAll  = "critical.#";
    public const string RoutingHighAll      = "high.#";
    public const string RoutingAnyFraud     = "*.frauddetection";
    public const string RoutingAnyLogin     = "*.loginattempt";
    public const string DirectUserPrefix    = "user.";
}
