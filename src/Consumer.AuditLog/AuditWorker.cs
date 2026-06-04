
namespace BankSecurityAlert.Consumers.AuditLog;

public class AuditWorker : BackgroundService
{
    private readonly AuditRepository _repo;
    private readonly ILogger<AuditWorker> _logger;

    public AuditWorker(AuditRepository repo, ILogger<AuditWorker> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Corre el consumer en un thread separado para no bloquear el host
        return Task.Run(() =>
        {
            _logger.LogInformation("[AuditLog] Worker iniciado — conectando a RabbitMQ...");
            using var consumer = new AuditLogConsumer(_repo);
            consumer.StartConsuming(stoppingToken);
            _logger.LogInformation("[AuditLog] Worker detenido.");
        }, stoppingToken);
    }
}
