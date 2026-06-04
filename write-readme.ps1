$readme = @'
# BankSecurityAlert

A distributed bank security alert system built with .NET 8, RabbitMQ, and Docker. The architecture follows an event-driven microservices pattern where a central Producer publishes security alerts through multiple RabbitMQ exchange types, and three independent consumers process those alerts according to their specific responsibilities.

---

## Architecture Overview

```
                        +---------------------+
                        |      Producer       |
                        |   (Alert Generator) |
                        +----------+----------+
                                   |
              +--------------------+--------------------+
              |                    |                    |
     Topic Exchange         Fanout Exchange       Direct Exchange
   bank.alerts.topic      bank.alerts.fanout    bank.alerts.direct
              |                    |                    |
    +---------+---------+ +--------+---------+ +--------+---------+
    | Patterns:         | | Receives ALL     | | Key:             |
    |  critical.#       | | alerts           | |  user.<userId>   |
    |  high.#           | | (broadcast)      | |  (targeted)      |
    +---------+---------+ +--------+---------+ +--------+---------+
              |                    |                    |
    +---------+---------+ +--------+---------+ +--------+---------+
    | Consumer 1        | | Consumer 2        | | Consumer 3       |
    | Fraud Detection   | | Alert Dashboard   | | Audit Log API    |
    +-------------------+ +-------------------+ +------------------+
```

### Exchange Strategy

| Exchange | Type | Purpose |
|---|---|---|
| `bank.alerts.topic` | Topic | Routes alerts by severity and category using wildcard patterns |
| `bank.alerts.fanout` | Fanout | Broadcasts every alert to all bound queues unconditionally |
| `bank.alerts.direct` | Direct | Routes alerts to a specific user queue by exact key |

### Queues and Bindings

| Queue | Exchange | Binding / Routing Pattern | Consumed by |
|---|---|---|---|
| `queue.fraud.detection` | Topic | `critical.#`, `high.#` | Consumer.FraudDetection |
| `queue.dashboard.fanout` | Fanout | (all — no key required) | Consumer.AlertDashboard |
| `queue.audit.log` | Topic | `#` (all messages) | Consumer.AuditLog |
| `queue.user.direct` | Direct | `user.<userId>` | Consumer.AuditLog |

---

## Projects

### Producer

Console application that generates randomized `SecurityAlert` events and publishes them simultaneously to the Topic, Fanout, and Direct exchanges.

When the `RABBITMQ_HOST` environment variable is present (Docker mode), it runs automatically and publishes one alert every 3 seconds. When that variable is absent (local mode), it enters an interactive prompt.

### Consumer.FraudDetection

Console application that subscribes to the **Topic Exchange** using the binding patterns `critical.#` and `high.#`. It only receives alerts of severity `High` or `Critical`, evaluates a risk score, and flags cases for immediate review.

### Consumer.AlertDashboard

Console application that subscribes to the **Fanout Exchange**. It receives every alert regardless of routing key and simulates a real-time monitoring dashboard with severity counters.

### Consumer.AuditLog

ASP.NET Core application that combines two responsibilities:

- A background worker (`AuditWorker`) that subscribes to the **Topic Exchange** with pattern `#` and persists every alert to a SQLite database.
- A Minimal API that exposes the stored audit records over HTTP on port `8080`.

This is the only microservice that exposes HTTP endpoints.

### BankSecurityAlert.Shared

Class library referenced by all projects. Contains the domain model, the RabbitMQ topology declaration, exchange and queue name constants, the publisher, and the base consumer abstraction.

---

## Domain Model

### SecurityAlert

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Unique alert identifier, auto-generated |
| `UserId` | `string` | Identifier of the affected user |
| `UserEmail` | `string` | Email of the affected user |
| `Severity` | `AlertSeverity` | `Low`, `Medium`, `High`, or `Critical` |
| `Category` | `AlertCategory` | `FraudDetection`, `LoginAttempt`, `LargeTransaction`, `AccountLockout`, or `SuspiciousLocation` |
| `Message` | `string` | Human-readable description of the alert |
| `SourceIp` | `string` | IP address that originated the event |
| `Country` | `string` | Country of origin |
| `TransactionAmount` | `decimal?` | Monetary amount involved, if applicable |
| `OccurredAt` | `DateTime` | UTC timestamp of the event |

Routing keys are computed from the alert fields:

- **Topic key** — `{severity}.{category}`, for example: `critical.frauddetection`, `high.loginattempt`
- **Direct key** — `user.{userId}`, for example: `user.USR-001`

---

## REST API — Consumer.AuditLog

Base URL (Docker): `http://localhost:8080`

---

### GET /api/alerts

Returns a paginated list of all alerts that have been consumed and persisted.

**Query parameters**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `page` | `int` | `1` | Page number (1-based) |
| `pageSize` | `int` | `20` | Number of records per page |

**Response 200**

```json
{
  "page": 1,
  "pageSize": 20,
  "count": 2,
  "results": [
    {
      "id": 1,
      "alertId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "routingKey": "critical.frauddetection",
      "userId": "USR-001",
      "userEmail": "user@example.com",
      "category": "FraudDetection",
      "severity": "Critical",
      "message": "Suspicious transaction detected",
      "sourceIp": "192.168.1.1",
      "country": "MX",
      "amount": 15000.00,
      "complianceTag": "CRITICAL-FRAUD",
      "occurredAt": "2024-01-15T10:30:00Z",
      "processedAt": "2024-01-15T10:30:01Z"
    }
  ]
}
```

---

### GET /api/alerts/{id}

Returns a single audit entry by its internal auto-incremented ID.

**Path parameters**

| Parameter | Type | Description |
|---|---|---|
| `id` | `int` | Internal audit entry ID |

**Response 200** — Returns the full `AuditEntry` object (same shape as items in the list above).

**Response 404**

```json
{
  "message": "Entrada 99 no encontrada"
}
```

---

### GET /api/alerts/stats

Returns aggregated alert counts grouped by severity and by category.

**Response 200**

```json
{
  "total": 120,
  "critical": 30,
  "high": 45,
  "medium": 35,
  "low": 10,
  "byCategory": {
    "FraudDetection": 40,
    "LoginAttempt": 25,
    "LargeTransaction": 20,
    "AccountLockout": 20,
    "SuspiciousLocation": 15
  }
}
```

---

### GET /health

Basic health check to confirm the service is running.

**Response 200**

```json
{
  "status": "healthy",
  "service": "AuditLog"
}
```

---

## Getting Started

### Prerequisites

- [Docker](https://www.docker.com/) and Docker Compose
- .NET 8 SDK (required only for local development without Docker)

### Run with Docker Compose

```bash
docker compose up --build
```

This command builds and starts all five services:

| Container | Role | Exposed Port |
|---|---|---|
| `rabbitmq` | Message broker with management UI | `5672`, `15672` |
| `producer` | Publishes security alerts continuously | — |
| `consumer-fraud` | Fraud Detection consumer | — |
| `consumer-dashboard` | Alert Dashboard consumer | — |
| `consumer-auditlog` | Audit Log consumer and REST API | `8080` |

After startup:

- RabbitMQ Management UI: http://localhost:15672  (credentials: `guest` / `guest`)
- Audit Log API: http://localhost:8080/api/alerts

### Run Locally Without Docker

1. Start a local RabbitMQ instance and create the virtual host `bank-security`:

```bash
rabbitmqctl add_vhost bank-security
rabbitmqctl set_permissions -p bank-security guest ".*" ".*" ".*"
```

2. Run each project in a separate terminal from the repository root:

```bash
dotnet run --project src/Consumer.FraudDetection
dotnet run --project src/Consumer.AlertDashboard
dotnet run --project src/Consumer.AuditLog
dotnet run --project src/Producer
```

The Producer will enter interactive mode (press `A` for auto, `Enter` to send one alert, `Q` to quit) when `RABBITMQ_HOST` is not set.

---

## Project Structure

```
BankSecurityAlert.sln
docker-compose.yml
rabbitmq-setup/
  definitions.json        # Pre-configured exchanges, queues, and bindings
  rabbitmq.conf
src/
  Shared/                 # Shared library used by all projects
    Domain/
      SecurityAlert.cs    # Domain model and enums
    Infrastructure/
      Config/
        RabbitMQConstants.cs  # Exchange, queue, and routing key names
      RabbitMQ/
        RabbitMQTopology.cs   # Declares exchanges, queues, and bindings
        AlertPublisher.cs     # Publishes to Topic, Fanout, and Direct
    BaseAlertConsumer.cs      # Abstract base for all consumers
  Producer/               # Alert generator — console application
  Consumer.FraudDetection/  # Topic consumer (critical/high only) — console
  Consumer.AlertDashboard/  # Fanout consumer (all alerts) — console
  Consumer.AuditLog/      # Topic consumer (all) + SQLite + REST API
    AuditRepository.cs    # SQLite data access
    AuditWorker.cs        # Background service for RabbitMQ consumption
    Program.cs            # Minimal API endpoints
```

---

## Configuration

All services read connection parameters at startup. The RabbitMQ host is resolved from the environment, defaulting to `localhost` for local development.

| Environment Variable | Default | Used by | Description |
|---|---|---|---|
| `RABBITMQ_HOST` | `localhost` | All services | RabbitMQ broker hostname |
| `DB_PATH` | `audit.db` | Consumer.AuditLog | Path to the SQLite database file |
| `ASPNETCORE_URLS` | `http://+:8080` | Consumer.AuditLog | Address the HTTP server binds to |

RabbitMQ connection constants (port `5672`, virtual host `bank-security`, exchange and queue names) are centralised in `src/Shared/Infrastructure/Config/RabbitMQConstants.cs`.

---

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Message broker | RabbitMQ 3.12 |
| RabbitMQ client | RabbitMQ.Client |
| Database | SQLite via Microsoft.Data.Sqlite |
| HTTP API framework | ASP.NET Core Minimal APIs |
| Containerisation | Docker and Docker Compose |
'@

Set-Content -Path "C:\Dev\RabitMQProject\BA2\README.md" -Value $readme -Encoding UTF8
Write-Host "README written."
