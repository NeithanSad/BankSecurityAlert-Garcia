
using BankSecurityAlert.Domain;
using Microsoft.Data.Sqlite;

namespace BankSecurityAlert.Consumers.AuditLog;

public class AuditEntry
{
    public int Id { get; set; }
    public string AlertId { get; set; } = "";
    public string RoutingKey { get; set; } = "";
    public string UserId { get; set; } = "";
    public string UserEmail { get; set; } = "";
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string SourceIp { get; set; } = "";
    public string Country { get; set; } = "";
    public decimal? Amount { get; set; }
    public string ComplianceTag { get; set; } = "";
    public string OccurredAt { get; set; } = "";
    public string ProcessedAt { get; set; } = "";
}

public class AuditStats
{
    public int Total { get; set; }
    public int Critical { get; set; }
    public int High { get; set; }
    public int Medium { get; set; }
    public int Low { get; set; }
    public Dictionary<string, int> ByCategory { get; set; } = new();
}

public class AuditRepository
{
    private readonly string _connStr;

    public AuditRepository(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
        InitDb();
    }

    private void InitDb()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        conn.CreateCommand().Tap(c =>
        {
            c.CommandText = """
                CREATE TABLE IF NOT EXISTS AuditEntries (
                    Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                    AlertId       TEXT NOT NULL,
                    RoutingKey    TEXT NOT NULL,
                    UserId        TEXT NOT NULL,
                    UserEmail     TEXT NOT NULL,
                    Category      TEXT NOT NULL,
                    Severity      TEXT NOT NULL,
                    Message       TEXT NOT NULL,
                    SourceIp      TEXT NOT NULL,
                    Country       TEXT NOT NULL,
                    Amount        REAL,
                    ComplianceTag TEXT NOT NULL,
                    OccurredAt    TEXT NOT NULL,
                    ProcessedAt   TEXT NOT NULL
                );
            """;
            c.ExecuteNonQuery();
        });
    }

    public void Save(AuditEntry entry)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        conn.CreateCommand().Tap(c =>
        {
            c.CommandText = """
                INSERT INTO AuditEntries
                    (AlertId,RoutingKey,UserId,UserEmail,Category,Severity,
                     Message,SourceIp,Country,Amount,ComplianceTag,OccurredAt,ProcessedAt)
                VALUES
                    ($aid,$rk,$uid,$uemail,$cat,$sev,
                     $msg,$ip,$ctr,$amt,$comp,$occ,$proc);
            """;
            c.Parameters.AddWithValue("$aid", entry.AlertId);
            c.Parameters.AddWithValue("$rk", entry.RoutingKey);
            c.Parameters.AddWithValue("$uid", entry.UserId);
            c.Parameters.AddWithValue("$uemail", entry.UserEmail);
            c.Parameters.AddWithValue("$cat", entry.Category);
            c.Parameters.AddWithValue("$sev", entry.Severity);
            c.Parameters.AddWithValue("$msg", entry.Message);
            c.Parameters.AddWithValue("$ip", entry.SourceIp);
            c.Parameters.AddWithValue("$ctr", entry.Country);
            c.Parameters.AddWithValue("$amt", entry.Amount.HasValue ? entry.Amount.Value : DBNull.Value);
            c.Parameters.AddWithValue("$comp", entry.ComplianceTag);
            c.Parameters.AddWithValue("$occ", entry.OccurredAt);
            c.Parameters.AddWithValue("$proc", entry.ProcessedAt);
            c.ExecuteNonQuery();
        });
    }

    public List<AuditEntry> GetAll(int page = 1, int pageSize = 20)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT * FROM AuditEntries
            ORDER BY Id DESC
            LIMIT $size OFFSET $offset;
        """;
        cmd.Parameters.AddWithValue("$size", pageSize);
        cmd.Parameters.AddWithValue("$offset", (page - 1) * pageSize);
        return ReadEntries(cmd);
    }

    public AuditEntry? GetById(int id)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM AuditEntries WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$id", id);
        return ReadEntries(cmd).FirstOrDefault();
    }

    public AuditStats GetStats()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();

        var stats = new AuditStats();

        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Severity, COUNT(*) FROM AuditEntries GROUP BY Severity;";
        using (var r = cmd.ExecuteReader())
            while (r.Read())
            {
                var count = r.GetInt32(1);
                stats.Total += count;
                switch (r.GetString(0))
                {
                    case "Critical": stats.Critical = count; break;
                    case "High": stats.High = count; break;
                    case "Medium": stats.Medium = count; break;
                    case "Low": stats.Low = count; break;
                }
            }

        var cmd2 = conn.CreateCommand();
        cmd2.CommandText = "SELECT Category, COUNT(*) FROM AuditEntries GROUP BY Category;";
        using (var r = cmd2.ExecuteReader())
            while (r.Read())
                stats.ByCategory[r.GetString(0)] = r.GetInt32(1);

        return stats;
    }

    private static List<AuditEntry> ReadEntries(SqliteCommand cmd)
    {
        var list = new List<AuditEntry>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new AuditEntry
            {
                Id = r.GetInt32(0),
                AlertId = r.GetString(1),
                RoutingKey = r.GetString(2),
                UserId = r.GetString(3),
                UserEmail = r.GetString(4),
                Category = r.GetString(5),
                Severity = r.GetString(6),
                Message = r.GetString(7),
                SourceIp = r.GetString(8),
                Country = r.GetString(9),
                Amount = r.IsDBNull(10) ? null : (decimal?)r.GetDouble(10),
                ComplianceTag = r.GetString(11),
                OccurredAt = r.GetString(12),
                ProcessedAt = r.GetString(13),
            });
        return list;
    }
}

// Extension helper para evitar variables temporales
internal static class SqliteCommandExt
{
    public static SqliteCommand Tap(this SqliteCommand cmd, Action<SqliteCommand> action)
    { action(cmd); return cmd; }
}
