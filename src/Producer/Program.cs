using BankSecurityAlert.Infrastructure.RabbitMQ;
using BankSecurityAlert.Producer;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║     BANK SECURITY ALERT — PRODUCER                  ║");
Console.WriteLine("║     Exchange: Topic + Fanout + Direct                ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();

// En Docker no hay consola interactiva — modo automático siempre
bool isDocker = Environment.GetEnvironmentVariable("RABBITMQ_HOST") != null;
int delayMs   = 3000; // una alerta cada 3 segundos

Console.Write("\n[Config] Conectando a RabbitMQ... ");
using var topology = new RabbitMQTopology();
var publisher      = new AlertPublisher(topology.Channel);
Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();

if (isDocker)
{
    Console.WriteLine($"[Auto] Modo Docker — enviando alertas cada {delayMs / 1000}s. Ctrl+C para detener.\n");
    using var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

    while (!cts.Token.IsCancellationRequested)
    {
        var alert = AlertGenerator.GenerateRandom();

        Console.ForegroundColor = alert.Severity switch
        {
            BankSecurityAlert.Domain.AlertSeverity.Critical => ConsoleColor.Red,
            BankSecurityAlert.Domain.AlertSeverity.High     => ConsoleColor.Yellow,
            BankSecurityAlert.Domain.AlertSeverity.Medium   => ConsoleColor.Blue,
            _                                               => ConsoleColor.Gray
        };
        Console.WriteLine($"\n[{DateTime.UtcNow:HH:mm:ss}] Publicando: {alert}");
        Console.ResetColor();

        publisher.Publish(alert);
        await Task.Delay(delayMs, cts.Token).ContinueWith(_ => { });
    }
}
else
{
    Console.WriteLine("Presiona [ENTER] para enviar una alerta, [A] auto, [Q] salir.\n");
    bool autoMode = false;

    while (true)
    {
        if (!autoMode)
        {
            Console.Write("Comando > ");
            var key = Console.ReadKey(intercept: false);
            Console.WriteLine();
            if (key.Key == ConsoleKey.Q) break;
            if (key.Key == ConsoleKey.A) { autoMode = true; Console.WriteLine("[Auto] Cada 2s. [Q] para detener.\n"); }
        }
        else
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q) break;
            await Task.Delay(2000);
        }

        var alert = AlertGenerator.GenerateRandom();
        Console.ForegroundColor = alert.Severity switch
        {
            BankSecurityAlert.Domain.AlertSeverity.Critical => ConsoleColor.Red,
            BankSecurityAlert.Domain.AlertSeverity.High     => ConsoleColor.Yellow,
            BankSecurityAlert.Domain.AlertSeverity.Medium   => ConsoleColor.Blue,
            _                                               => ConsoleColor.Gray
        };
        Console.WriteLine($"\n[{DateTime.UtcNow:HH:mm:ss}] {alert}");
        Console.ResetColor();
        publisher.Publish(alert);
    }
}

Console.WriteLine("\n[Producer] Conexion cerrada.");
