using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSecurityAlert.Consumers.FraudDetection
{
    internal sealed class FraudDetectionConsumer : BaseAlertConsumer
    {
        protected override string QueueName => RabbitMQConstants.FraudDetectionQueue;
        protected override string ConsumerName => "FraudDetection";
        protected override ConsoleColor AccentColor => ConsoleColor.Red;

        protected override void ProcessAlert(SecurityAlert alert, string routingKey)
        {
            var riskScore = CalculateRiskScore(alert);

            Console.ForegroundColor = alert.Severity == AlertSeverity.Critical
                ? ConsoleColor.Red : ConsoleColor.Yellow;

            Console.WriteLine($"  🔍 ANÁLISIS DE FRAUDE");
            Console.WriteLine($"  ┌─ Usuario    : {alert.UserId} ({alert.UserEmail})");
            Console.WriteLine($"  ├─ Severidad  : {alert.Severity}");
            Console.WriteLine($"  ├─ Categoría  : {alert.Category}");
            Console.WriteLine($"  ├─ IP Origen  : {alert.SourceIp} [{alert.Country}]");
            Console.WriteLine($"  ├─ Mensaje    : {alert.Message}");
            if (alert.TransactionAmount.HasValue)
                Console.WriteLine($"  ├─ Monto      : ${alert.TransactionAmount:N2}");
            Console.WriteLine($"  ├─ Fecha/Hora : {alert.OccurredAt:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"  └─ Risk Score : {riskScore}/100 {RiskEmoji(riskScore)}");

            if (riskScore >= 80)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n  ⚠️  ACCIÓN REQUERIDA: Bloqueo preventivo de cuenta {alert.UserId}");
                Console.WriteLine($"  📧 Notificando equipo antifraude...");
            }

            Console.ResetColor();
        }

        private static int CalculateRiskScore(SecurityAlert alert)
        {
            int score = alert.Severity switch
            {
                AlertSeverity.Critical => 90,
                AlertSeverity.High => 70,
                AlertSeverity.Medium => 40,
                _ => 15
            };

            if (alert.Country is "China" or "Russia") score += 10;
            if (alert.TransactionAmount > 20000) score += 5;
            if (alert.Category == AlertCategory.FraudDetection) score += 5;

            return Math.Min(score, 100);
        }

        private static string RiskEmoji(int score) =>
            score >= 80 ? "🔴 ALTO" : score >= 50 ? "🟠 MEDIO" : "🟢 BAJO";
    }
}
