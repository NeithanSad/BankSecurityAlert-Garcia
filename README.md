# BankSecurityAlert

Un sistema distribuido de alertas de seguridad bancaria construido con .NET 8, RabbitMQ y Docker. La arquitectura sigue un patrón de microservicios orientados a eventos, donde un Productor central publica alertas de seguridad a través de múltiples tipos de exchanges (intercambiadores) de RabbitMQ, y tres consumidores independientes procesan estas alertas de acuerdo con sus responsabilidades específicas.

---

## Resumen de la Arquitectura

```
                        +----------------------+
                        |      Productor       |
                        | (Generador Alertas)  |
                        +----------+-----------+
                                   |
              +--------------------+--------------------+
              |                    |                    |
     Topic Exchange         Fanout Exchange       Direct Exchange
   bank.alerts.topic      bank.alerts.fanout    bank.alerts.direct
              |                    |                    |
    +---------+---------+ +--------+---------+ +--------+---------+
    | Patrones:         | | Recibe TODAS     | | Clave:           |
    |  critical.#       | | las alertas      | |  user.<userId>   |
    |  high.#           | | (difusión)       | |  (objetivo)      |
    +---------+---------+ +--------+---------+ +--------+---------+
              |                    |                    |
    +---------+---------+ +--------+---------+ +--------+---------+
    | Consumidor 1      | | Consumidor 2      | | Consumidor 3     |
    | Detección Fraude  | | Panel de Alertas  | | API de Auditoría |
    +-------------------+ +-------------------+ +------------------+
```

### Estrategia de Exchanges

| Exchange | Tipo | Propósito |
|---|---|---|
| `bank.alerts.topic` | Topic | Enruta alertas según patrones de severidad y categoría utilizando comodines |
| `bank.alerts.fanout` | Fanout | Difunde cada alerta a todas las colas vinculadas de forma incondicional |
| `bank.alerts.direct` | Direct | Enruta alertas a una cola de usuario específica mediante coincidencia exacta |

### Colas y Enrutamiento (Bindings)

| Cola | Exchange | Patrón de Enrutamiento | Consumido por |
|---|---|---|---|
| `queue.fraud.detection` | Topic | `critical.#`, `high.#` | Consumer.FraudDetection |
| `queue.dashboard.fanout` | Fanout | (todas) | Consumer.AlertDashboard |
| `queue.audit.log` | Topic | `#` (todas las alertas) | Consumer.AuditLog |
| `queue.user.direct` | Direct | `user.<userId>` | Consumer.AuditLog |

---

## Proyectos (Microservicios)

### Producer (Productor)

Aplicación de consola que genera eventos aleatorios tipo `SecurityAlert` y los publica simultáneamente a los exchanges Topic, Fanout y Direct.

Cuando la variable de entorno `RABBITMQ_HOST` está configurada (por ejemplo, en Docker), se ejecuta de forma automática y publica una alerta cada 3 segundos. Si falta la variable de entorno, entra en modo interactivo para uso local.

### Consumer.FraudDetection (Detección de Fraudes)

Aplicación de consola que se suscribe al **Topic Exchange** utilizando los patrones `critical.#` y `high.#`. Solo recibe alertas con severidad `High` o `Critical`, evalúa una puntuación de riesgo y marca los casos para revisión inmediata.

### Consumer.AlertDashboard (Panel de Alertas)

Aplicación de consola que se suscribe al **Fanout Exchange**. Recibe todas las alertas sin importar su clave de enrutamiento y simula un panel de monitoreo en tiempo real actualizando los contadores de severidad.

### Consumer.AuditLog (Registro de Auditoría y API)

Aplicación en ASP.NET Core que combina dos responsabilidades:

- Un proceso en segundo plano (`AuditWorker`) que se suscribe al **Topic Exchange** con el patrón `#` (todas las alertas) y las guarda mediante persistencia en una base de datos SQLite.
- Una Minimal API que expone los registros de auditoría almacenados a través de HTTP en el puerto `8080`.

Este es el único microservicio que cuenta con endpoints HTTP.

### BankSecurityAlert.Shared

Librería de clases común que es referenciada por todos los demás proyectos. Contiene el modelo de dominio, la sintaxis de topología de RabbitMQ, las constantes de colas y exchanges, la lógica de publicación, y la clase abstracta para todos los consumidores.

---

## Endpoints de la API REST — Consumer.AuditLog

URL Base (Docker): `http://localhost:8080`

### GET /api/alerts

Retorna una lista paginada de todas las alertas consumidas y almacenadas.

**Parámetros de Consulta (Query)**

| Parámetro | Tipo | Por defecto | Descripción |
|---|---|---|---|
| `page` | `int` | `1` | Número de página |
| `pageSize` | `int` | `20` | Número de registros por página |

**Respuesta 200**

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
      "message": "Transaccion sospechosa detectada",
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

Retorna una sola entrada de auditoría mediante su ID (autoincremental).

**Parámetros de Ruta (Path)**

| Parámetro | Tipo | Descripción |
|---|---|---|
| `id` | `int` | ID interno de la entrada de auditoría |

**Respuesta 200** — Retorna el objeto `AuditEntry` correspondiente de la base de datos.
**Respuesta 404** — Si el registro no se encuentra, retorna un mensaje con código de error.

---

### GET /api/alerts/stats

Retorna los conteos globales agrupados por nivel de severidad y por categoría.

**Respuesta 200**

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

Punto de control de salud para confirmar que la API web se encuentra activa.

**Respuesta 200**

```json
{
  "status": "healthy",
  "service": "AuditLog"
}
```

---

## Cómo Empezar

### Requisitos Previos

- [Docker](https://www.docker.com/) y Docker Compose
- .NET 8 SDK (Requerido solo para desarrollo local sin Docker)

### Ejecución usando Docker Compose

```bash
docker compose up --build
```

Esto iniciará los cinco servicios definidos:

| Contenedor | Descripción | Puerto Expuesto |
|---|---|---|
| `rabbitmq` | Broker de mensajes con la interfaz de gestión UI | `5672`, `15672` |
| `producer` | Publica de manera automática alertas | — |
| `consumer-fraud` | Consumidor de detección de fraude | — |
| `consumer-dashboard` | Consumidor de panel general | — |
| `consumer-auditlog` | Consumidor de auditoría y API REST | `8080` |

Una vez en marcha:

- Management UI de RabbitMQ: http://localhost:15672 (Credenciales: `guest` / `guest`)
- API REST Audit Log: http://localhost:8080/api/alerts

### Desarrollo Local (sin Docker)

1. Iniciar un contenedor/instancia local de RabbitMQ y crear un Virtual Host `bank-security`.
2. Lanzar cada microservicio en diferentes ventanas de la consola de comandos:

```bash
dotnet run --project src/Consumer.FraudDetection
dotnet run --project src/Consumer.AlertDashboard
dotnet run --project src/Consumer.AuditLog
dotnet run --project src/Producer
```
