using BankSecurityAlert.Consumers;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using BankSecurityAlert.Consumers.FraudDetection;

/// <summary>
/// CONSUMER 1 — Fraud Detection System
/// Exchange: Topic | Queue: queue.fraud.detection
/// Routing patterns: "critical.#" and "high.#"
///
/// Simulates a fraud detection engine that:
///   - Flags critical alerts for immediate review
///   - Logs high-severity patterns
///   - Generates a risk score
/// </summary>


// ── Entry Point ──────────────────────────────────────────────────────────────
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Red;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  🔍  CONSUMER 1: FRAUD DETECTION SYSTEM             ║");
Console.WriteLine("║     Topic Exchange | Patrones: critical.# / high.#  ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝\n");
Console.ResetColor();

using var consumer = new FraudDetectionConsumer();
using var cts      = new CancellationTokenSource();

consumer.StartConsuming(cts.Token);
Console.WriteLine("\n[FraudDetection] Detenido. 👋");
