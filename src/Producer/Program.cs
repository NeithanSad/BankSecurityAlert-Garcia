using BankSecurityAlert.Infrastructure.RabbitMQ;
using BankSecurityAlert.Producer;

Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║       BANK SECURITY ALERT — PRODUCER                 ║");
Console.WriteLine("║     Exchange: Topic + Fanout + Direct                ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();

Console.Write("\n[Config] Conectando a RabbitMQ... ");

using var topology = new RabbitMQTopology();
var publisher = new AlertPublisher(topology.Channel);

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("OK");
Console.ResetColor();

Console.WriteLine("\nPresiona [ENTER] para enviar una alerta, [A] para envío automático, [Q] para salir.\n");

bool autoMode = false;

while (true)
{
    if (!autoMode)
    {
        Console.Write("Comando > ");
        var key = Console.ReadKey(intercept: false);
        Console.WriteLine();

        if (key.Key == ConsoleKey.Q) break;
        if (key.Key == ConsoleKey.A) { autoMode = true; Console.WriteLine("[Auto] Enviando alertas cada 2 segundos. [Q] para detener.\n"); }
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

    Console.WriteLine($"\nPublicando alerta:");
    Console.WriteLine($"   {alert}");
    Console.WriteLine($"   TopicKey={alert.TopicRoutingKey} | DirectKey={alert.DirectRoutingKey}");
    Console.ResetColor();

    publisher.Publish(alert);

    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine($"   ─────────────────────────────────────────");
    Console.ResetColor();
}

Console.WriteLine("\n[Producer] Conexión cerrada. ¡Hasta pronto!");
