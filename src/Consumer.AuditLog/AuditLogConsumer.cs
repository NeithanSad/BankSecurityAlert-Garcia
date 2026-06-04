
using BankSecurityAlert.Consumers;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;

namespace BankSecurityAlert.Consumers.AuditLog;

public class AuditLogConsumer : BaseAlertConsumer
{
    private readonly AuditRepository _repo;

    public AuditLogConsumer(AuditRepository repo)
    {
        _repo = repo;
    }

    protected override string QueueName => RabbitMQConstants.AuditLogQueue;
    protected override string ConsumerName => "AuditLog";
    protected override ConsoleColor AccentColor => ConsoleColor.Green;

    protected override void ProcessAlert(SecurityAlert alert, string routingKey)
    {
        var complianceTag = ClassifyCompliance(alert);

        var entry = new AuditEntry
        {
            AlertId = alert.Id.ToString(),
            RoutingKey = routingKey,
            UserId = alert.UserId,
            UserEmail = alert.UserEmail,
            Category = alert.Category.ToString(),
            Severity = alert.Severity.ToString(),
            Message = alert.Message,
            SourceIp = alert.SourceIp,
            Country = alert.Country,
            Amount = alert.TransactionAmount,
            ComplianceTag = complianceTag,
            OccurredAt = alert.OccurredAt.ToString("O"),
            ProcessedAt = DateTime.UtcNow.ToString("O"),
        };

        _repo.Save(entry);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"  📋 [{alert.Severity}] {alert.Category} | {alert.UserId} | {complianceTag}");
        Console.WriteLine($"  💾 Guardado en SQLite — RoutingKey: {routingKey}");
        Console.ResetColor();
    }

    private static string ClassifyCompliance(SecurityAlert alert) =>
        alert.Category switch
        {
            AlertCategory.FraudDetection => "PCI-DSS Section 10.6",
            AlertCategory.LoginAttempt => "ISO 27001 A.9.4.2",
            AlertCategory.LargeTransaction => "FATF Recomendacion 10",
            AlertCategory.AccountLockout => "PCI-DSS Section 8.1.6",
            _ => "Evento estandar de auditoria"
        };
}
