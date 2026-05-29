using BankSecurityAlert.Consumers;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using BankSecurityAlert.Consumers.AuditLog;

/// <summary>
/// CONSUMER 3 — Audit Log System
/// Exchange: Topic | Queue: queue.audit.log
/// Routing patterns: "*.frauddetection" and "*.loginattempt"
///
/// Also has a Direct Exchange branch (queue.user.direct) for personal alerts.
///
/// Simulates a compliance audit logger that:
///   - Writes structured audit entries
///   - Classifies events for regulatory reporting
///   - Flags GDPR/PCI-DSS relevant events
/// </summary>


// ── Entry Point ──────────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       CONSUMER 3: COMPLIANCE AUDIT LOG               ║");
Console.WriteLine("║     Topic Exchange | *.frauddetection / *.loginattempt║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");
Console.ResetColor();

using var consumer = new AuditLogConsumer();
using var cts      = new CancellationTokenSource();

consumer.StartConsuming(cts.Token);
Console.WriteLine("\n[AuditLog] Detenido. ");
