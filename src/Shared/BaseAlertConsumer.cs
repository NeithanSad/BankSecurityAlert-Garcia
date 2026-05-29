using System.Text;
using System.Text.Json;
using BankSecurityAlert.Domain;
using BankSecurityAlert.Infrastructure.Config;
using BankSecurityAlert.Infrastructure.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BankSecurityAlert.Consumers;

/// <summary>
/// Base class for all consumers. Handles connection, channel, and deserialization.
/// Subclasses implement <see cref="ProcessAlert"/> with their business logic.
/// </summary>
public abstract class BaseAlertConsumer : IDisposable
{
    private readonly RabbitMQTopology _topology;
    protected readonly IModel Channel;
    protected abstract string QueueName { get; }
    protected abstract string ConsumerName { get; }
    protected abstract ConsoleColor AccentColor { get; }

    protected BaseAlertConsumer()
    {
        _topology = new RabbitMQTopology();
        Channel   = _topology.Channel;
    }

    public void StartConsuming(CancellationToken ct)
    {
        // QoS: process one message at a time
        Channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(Channel);

        consumer.Received += (_, ea) =>
        {
            try
            {
                var json  = Encoding.UTF8.GetString(ea.Body.ToArray());
                var alert = JsonSerializer.Deserialize<SecurityAlert>(json)
                            ?? throw new InvalidOperationException("Null alert");

                PrintHeader(ea.RoutingKey);
                ProcessAlert(alert, ea.RoutingKey);
                PrintFooter();

                Channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"   Error procesando mensaje: {ex.Message}");
                Console.ResetColor();

                // Reject and requeue=false → goes to dead-letter (if configured)
                Channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        Channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

        Console.ForegroundColor = AccentColor;
        Console.WriteLine($"[{ConsumerName}]  Escuchando en cola '{QueueName}'...");
        Console.WriteLine($"[{ConsumerName}] Presiona [Q] para salir.\n");
        Console.ResetColor();

        while (!ct.IsCancellationRequested)
        {
            if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Q)
                break;
            Thread.Sleep(100);
        }
    }

    /// <summary>Override to implement consumer-specific processing logic.</summary>
    protected abstract void ProcessAlert(SecurityAlert alert, string routingKey);

    private void PrintHeader(string routingKey)
    {
        Console.ForegroundColor = AccentColor;
        Console.WriteLine($"\n[{ConsumerName}]  Mensaje recibido | routingKey={routingKey}");
        Console.ResetColor();
    }

    private void PrintFooter()
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"   ACK enviado");
        Console.ResetColor();
    }

    public void Dispose() => _topology.Dispose();
}
