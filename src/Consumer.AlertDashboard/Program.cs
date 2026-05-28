using BankSecurityAlert.Consumers;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using BankSecurityAlert.Consumers.AlertDashboard;

/// <summary>
/// CONSUMER 2 — Real-time Alert Dashboard
/// Exchange: Fanout | Queue: queue.dashboard.fanout
/// Receives: ALL alerts regardless of routing key
///
/// Simulates a monitoring dashboard that:
///   - Displays all alerts in real time
///   - Maintains severity counters
///   - Shows trending alerts
/// </summary>

// ── Entry Point ──────────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  📊  CONSUMER 2: REAL-TIME ALERT DASHBOARD          ║");
Console.WriteLine("║     Fanout Exchange | Recibe TODAS las alertas       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");
Console.ResetColor();

using var consumer = new AlertDashboardConsumer();
using var cts      = new CancellationTokenSource();

consumer.StartConsuming(cts.Token);
Console.WriteLine("\n[Dashboard] Detenido. 👋");
