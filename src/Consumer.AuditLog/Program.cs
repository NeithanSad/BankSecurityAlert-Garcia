
using BankSecurityAlert.Consumers.AuditLog;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var builder = WebApplication.CreateBuilder(args);

// ── SQLite ────────────────────────────────────────────────────
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "audit.db";
var repo = new AuditRepository(dbPath);
builder.Services.AddSingleton(repo);

// ── Background worker (consume RabbitMQ) ─────────────────────
builder.Services.AddHostedService<AuditWorker>();

// ── API ───────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

// GET /api/alerts — todos los mensajes consumidos (paginado)
app.MapGet("/api/alerts", (int page = 1, int pageSize = 20) =>
{
    var entries = repo.GetAll(page, pageSize);
    return Results.Ok(new
    {
        page,
        pageSize,
        count = entries.Count,
        results = entries
    });
})
.WithName("GetAlerts")
.WithDescription("Retorna los mensajes consumidos de la cola, paginados");

// GET /api/alerts/{id} — detalle de un mensaje específico
app.MapGet("/api/alerts/{id:int}", (int id) =>
{
    var entry = repo.GetById(id);
    return entry is null
        ? Results.NotFound(new { message = $"Entrada {id} no encontrada" })
        : Results.Ok(entry);
})
.WithName("GetAlertById");

// GET /api/alerts/stats — conteo por severidad y categoría
app.MapGet("/api/alerts/stats", () =>
{
    var stats = repo.GetStats();
    return Results.Ok(stats);
})
.WithName("GetStats");

// Health check básico
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "AuditLog" }));

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("╔══════════════════════════════════════════════════════╗");
Console.WriteLine("║  📋  CONSUMER 3: AUDIT LOG — WEB API                ║");
Console.WriteLine("║  RabbitMQ consumer + REST API en :8080               ║");
Console.WriteLine("╚══════════════════════════════════════════════════════╝");
Console.ResetColor();

app.Run();
