using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BankSecurityAlert.Consumers.AlertDashboard
{
   
        internal class AlertDashboardConsumer : BaseAlertConsumer
        {
            protected override string QueueName => RabbitMQConstants.DashboardFanoutQueue;
            protected override string ConsumerName => "Dashboard";
            protected override ConsoleColor AccentColor => ConsoleColor.Cyan;

            // In-memory counters (would be Redis/DB in production)
            private static int _criticalCount;
            private static int _highCount;
            private static int _mediumCount;
            private static int _lowCount;
            private static int _totalCount;

            protected override void ProcessAlert(SecurityAlert alert, string routingKey)
            {
                IncrementCounter(alert.Severity);

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  📊 DASHBOARD — ALERTA #{_totalCount}");
                Console.WriteLine($"  ┌─ [{alert.Severity,8}] {alert.Category}");
                Console.WriteLine($"  ├─ Usuario  : {alert.UserId} | {alert.Country}");
                Console.WriteLine($"  ├─ IP       : {alert.SourceIp}");
                Console.WriteLine($"  ├─ Mensaje  : {alert.Message}");
                Console.WriteLine($"  ├─ Hora     : {alert.OccurredAt:HH:mm:ss} UTC");
                Console.WriteLine($"  └─ Resumen  : 🔴 {_criticalCount} | 🟠 {_highCount} | 🔵 {_mediumCount} | ⚪ {_lowCount}");
                Console.ResetColor();

                // Simulate dashboard notification
                if (alert.Severity == AlertSeverity.Critical)
                {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine($"  🚨 ALERTA CRÍTICA — Notificando SOC team...");
                    Console.ResetColor();
                }
            }

            private static void IncrementCounter(AlertSeverity severity)
            {
                _totalCount++;
                switch (severity)
                {
                    case AlertSeverity.Critical: _criticalCount++; break;
                    case AlertSeverity.High: _highCount++; break;
                    case AlertSeverity.Medium: _mediumCount++; break;
                    default: _lowCount++; break;
                }
            }
        }

    
}
