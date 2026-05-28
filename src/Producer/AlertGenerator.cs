using BankSecurityAlert.Domain;

namespace BankSecurityAlert.Producer;

/// <summary>
/// Simulates realistic bank security events for demo purposes.
/// </summary>
public static class AlertGenerator
{
    private static readonly Random _rng = new();

    private static readonly string[] UserIds =
        ["USR001", "USR002", "USR003", "USR004", "USR005"];

    private static readonly string[] Emails =
    [
        "juan.perez@banco.com", "maria.garcia@banco.com",
        "carlos.lopez@banco.com", "ana.torres@banco.com", "luis.mora@banco.com"
    ];

    private static readonly string[] IPs =
    [
        "192.168.1.10", "203.0.113.45", "198.51.100.22",
        "172.16.0.5", "45.33.32.156"
    ];

    private static readonly string[] Countries =
        ["Ecuador", "Colombia", "USA", "China", "Russia"];

    public static SecurityAlert GenerateRandom()
    {
        var category = (AlertCategory)_rng.Next(0, 5);
        var severity = DetermineSeverity(category);
        var userIdx  = _rng.Next(0, UserIds.Length);

        return category switch
        {
            AlertCategory.FraudDetection => new SecurityAlert
            {
                UserId            = UserIds[userIdx],
                UserEmail         = Emails[userIdx],
                Severity          = severity,
                Category          = AlertCategory.FraudDetection,
                Message           = $"Patrón de fraude detectado: {_rng.Next(3, 15)} transacciones en 2 minutos",
                SourceIp          = IPs[_rng.Next(0, IPs.Length)],
                Country           = Countries[_rng.Next(0, Countries.Length)],
                TransactionAmount = _rng.Next(500, 50000)
            },

            AlertCategory.LoginAttempt => new SecurityAlert
            {
                UserId    = UserIds[userIdx],
                UserEmail = Emails[userIdx],
                Severity  = severity,
                Category  = AlertCategory.LoginAttempt,
                Message   = $"Intento fallido #{_rng.Next(3, 10)}: credenciales incorrectas",
                SourceIp  = IPs[_rng.Next(0, IPs.Length)],
                Country   = Countries[_rng.Next(0, Countries.Length)]
            },

            AlertCategory.LargeTransaction => new SecurityAlert
            {
                UserId            = UserIds[userIdx],
                UserEmail         = Emails[userIdx],
                Severity          = severity,
                Category          = AlertCategory.LargeTransaction,
                Message           = "Transferencia supera límite diario permitido",
                SourceIp          = IPs[_rng.Next(0, IPs.Length)],
                Country           = Countries[_rng.Next(0, Countries.Length)],
                TransactionAmount = _rng.Next(10000, 100000)
            },

            AlertCategory.AccountLockout => new SecurityAlert
            {
                UserId    = UserIds[userIdx],
                UserEmail = Emails[userIdx],
                Severity  = AlertSeverity.High,
                Category  = AlertCategory.AccountLockout,
                Message   = "Cuenta bloqueada por múltiples intentos fallidos",
                SourceIp  = IPs[_rng.Next(0, IPs.Length)],
                Country   = Countries[_rng.Next(0, Countries.Length)]
            },

            AlertCategory.SuspiciousLocation => new SecurityAlert
            {
                UserId    = UserIds[userIdx],
                UserEmail = Emails[userIdx],
                Severity  = severity,
                Category  = AlertCategory.SuspiciousLocation,
                Message   = $"Acceso desde ubicación inusual: {Countries[_rng.Next(0, Countries.Length)]}",
                SourceIp  = IPs[_rng.Next(0, IPs.Length)],
                Country   = Countries[_rng.Next(0, Countries.Length)]
            },

            _ => throw new InvalidOperationException()
        };
    }

    private static AlertSeverity DetermineSeverity(AlertCategory category) =>
        category switch
        {
            AlertCategory.FraudDetection    => _rng.Next(2) == 0 ? AlertSeverity.Critical : AlertSeverity.High,
            AlertCategory.LoginAttempt      => _rng.Next(2) == 0 ? AlertSeverity.Medium   : AlertSeverity.High,
            AlertCategory.LargeTransaction  => _rng.Next(2) == 0 ? AlertSeverity.High     : AlertSeverity.Medium,
            AlertCategory.AccountLockout    => AlertSeverity.High,
            AlertCategory.SuspiciousLocation=> _rng.Next(2) == 0 ? AlertSeverity.Critical : AlertSeverity.Medium,
            _                               => AlertSeverity.Low
        };
}
