namespace BankSecurityAlert.Domain;

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum AlertCategory
{
    FraudDetection,
    LoginAttempt,
    LargeTransaction,
    AccountLockout,
    SuspiciousLocation
}

public class SecurityAlert
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string UserId { get; init; } = string.Empty;
    public string UserEmail { get; init; } = string.Empty;
    public AlertSeverity Severity { get; init; }
    public AlertCategory Category { get; init; }
    public string Message { get; init; } = string.Empty;
    public string SourceIp { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public decimal? TransactionAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Topic Exchange routing key: severity.category
    /// e.g. "critical.fraud", "high.login", "medium.transaction"
    /// </summary>
    public string TopicRoutingKey =>
        $"{Severity.ToString().ToLower()}.{Category.ToString().ToLower()}";

    /// <summary>
    /// Direct Exchange routing key: specific user routing
    /// e.g. "user.USR001"
    /// </summary>
    public string DirectRoutingKey => $"user.{UserId}";

    public override string ToString() =>
        $"[{Severity}] {Category} | User: {UserId} | {Message} | IP: {SourceIp} ({Country})";
}
