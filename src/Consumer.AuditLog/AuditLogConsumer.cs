using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSecurityAlert.Consumers.AuditLog
{
    internal class AuditLogConsumer : BaseAlertConsumer
    {
        protected override string QueueName => RabbitMQConstants.AuditLogQueue;
        protected override string ConsumerName => "AuditLog";
        protected override ConsoleColor AccentColor => ConsoleColor.Green;

        private static int _auditEntryNumber = 1000;

        protected override void ProcessAlert(SecurityAlert alert, string routingKey)
        {
            var entryId = ++_auditEntryNumber;
            var complianceTag = ClassifyCompliance(alert);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  📋 ENTRADA DE AUDITORÍA #{entryId}");
            Console.WriteLine($"  ┌─ ID Alerta  : {alert.Id}");
            Console.WriteLine($"  ├─ RoutingKey : {routingKey}");
            Console.WriteLine($"  ├─ Usuario    : {alert.UserId} | {alert.UserEmail}");
            Console.WriteLine($"  ├─ Evento     : [{alert.Category}] {alert.Message}");
            Console.WriteLine($"  ├─ Severidad  : {alert.Severity}");
            Console.WriteLine($"  ├─ Origen     : {alert.SourceIp} — {alert.Country}");
            if (alert.TransactionAmount.HasValue)
                Console.WriteLine($"  ├─ Monto      : ${alert.TransactionAmount:N2}");
            Console.WriteLine($"  ├─ Timestamp  : {alert.OccurredAt:O}");
            Console.WriteLine($"  └─ Compliance : {complianceTag}");
            Console.ResetColor();

            // Simulate writing to audit file / database
            WriteAuditEntry(entryId, alert, routingKey);
        }

        private static string ClassifyCompliance(SecurityAlert alert) =>
            alert.Category switch
            {
                AlertCategory.FraudDetection => "🔒 PCI-DSS Section 10.6 | Revisión requerida en 24h",
                AlertCategory.LoginAttempt => "🔐 ISO 27001 A.9.4.2 | Acceso no autorizado",
                AlertCategory.LargeTransaction => "💰 FATF Recomendación 10 | Monitoreo AML",
                AlertCategory.AccountLockout => "🔒 PCI-DSS Section 8.1.6 | Lockout policy",
                _ => "📄 Evento estándar de auditoría"
            };

        private static void WriteAuditEntry(int entryId, SecurityAlert alert, string routingKey)
        {
            // In production: write to Elasticsearch / database / S3
            var logLine = $"AUDIT|{entryId}|{alert.OccurredAt:O}|{alert.UserId}|{alert.Category}|{alert.Severity}|{routingKey}";

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"  💾 LOG: {logLine}");
            Console.ResetColor();
        }
    }
}
