# BankSecurityAlert — Sistema de Alertas de Seguridad Bancaria con RabbitMQ

> **Caso real:** Sistema de mensajería asíncrona que procesa alertas de seguridad bancaria  
> usando **tres tipos de Exchange** de RabbitMQ: Topic, Fanout y Direct.

---

## Arquitectura del Sistema

```
                    ┌─────────────────────────────────────────────────────┐
                    │                   PRODUCER                          │
                    │          (Generador de Alertas Bancarias)           │
                    └──────────────────┬──────────────────────────────────┘
                                       │  Publica a 3 exchanges
                     ┌─────────────────┼──────────────────┐
                     ▼                 ▼                  ▼
           ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐
           │ Topic        │  │ Fanout       │  │ Direct           │
           │ Exchange     │  │ Exchange     │  │ Exchange         │
           │ severity.cat │  │ broadcast    │  │ user.<id>        │
           └──────┬───────┘  └──────┬───────┘  └────────┬─────────┘
                  │                 │                    │
         ┌────────┴─────┐          │              ┌─────┴──────────┐
         │              │          │              │                │
         ▼              ▼          ▼              ▼                │
┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
│queue.fraud   │ │queue.audit   │ │queue.dashboard│ │queue.user    │
│.detection    │ │.log          │ │.fanout        │ │.direct       │
│              │ │              │ │               │ │              │
│critical.#    │ │*.frauddetect │ │(todos)        │ │user.USR001   │
│high.#        │ │*.loginattempt│ │               │ │              │
└──────┬───────┘ └──────┬───────┘ └──────┬────────┘ └──────┬───────┘
       │                │                │                  │
       ▼                ▼                ▼                  │
┌──────────────┐ ┌──────────────┐ ┌──────────────┐         │
│CONSUMER 1    │ │CONSUMER 3    │ │CONSUMER 2    │◄────────┘
│FraudDetection│ │AuditLog      │ │AlertDashboard│
│(Topic)       │ │(Topic)       │ │(Fanout)      │
└──────────────┘ └──────────────┘ └──────────────┘
```

---

## Exchanges y su Rol en el Caso Real

### 1. Topic Exchange — `bank.alerts.topic`
**Routing Key format:** `{severity}.{category}`  
*Ejemplos:* `critical.frauddetection`, `high.loginattempt`, `medium.largetransaction`

| Patrón de Binding | Cola Destino | Recibe |
|---|---|---|
| `critical.#` | `queue.fraud.detection` | Cualquier alerta crítica |
| `high.#` | `queue.fraud.detection` | Cualquier alerta alta |
| `*.frauddetection` | `queue.audit.log` | Fraude de cualquier severidad |
| `*.loginattempt` | `queue.audit.log` | Logins de cualquier severidad |

### 2. Fanout Exchange — `bank.alerts.fanout`
**Routing Key:** ignorada (broadcast total)  
Todas las alertas van a `queue.dashboard.fanout` → Consumer Dashboard recibe el 100% del tráfico para su panel de monitoreo.

### 3. Direct Exchange — `bank.alerts.direct`
**Routing Key format:** `user.{userId}`  
Solo alertas High/Critical se envían directamente al usuario afectado.

| Routing Key | Cola Destino |
|---|---|
| `user.USR001` | `queue.user.direct` |

---

## Estructura del Proyecto

```
BankSecurityAlert/
├── BankSecurityAlert.sln
├── docker-compose.yml
├── rabbitmq-setup/
│   ├── definitions.json          # Pre-configura vhost, exchanges, queues, bindings
│   └── rabbitmq.conf
├── src/
│   ├── Domain/
│   │   └── SecurityAlert.cs      # Modelo de dominio
│   ├── Infrastructure/
│   │   ├── Config/
│   │   │   └── RabbitMQConstants.cs   # Nombres de exchanges/queues
│   │   └── RabbitMQ/
│   │       ├── RabbitMQTopology.cs    # Declara toda la topología
│   │       └── AlertPublisher.cs      # Publica a los 3 exchanges
│   ├── Producer/
│   │   ├── AlertGenerator.cs     # Genera alertas realistas
│   │   ├── Program.cs
│   │   └── Producer.csproj
│   └── Consumers/
│       ├── BaseAlertConsumer.cs  # Clase base (DRY)
│       ├── FraudDetection/       # Consumer 1 — Topic exchange
│       ├── AlertDashboard/       # Consumer 2 — Fanout exchange
│       └── AuditLog/             # Consumer 3 — Topic exchange
```

---

## Pasos para ejecutar

### Prerrequisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Paso 1: Levantar RabbitMQ con Docker

```bash
# Opción A: con definiciones automáticas (vhost + exchanges + queues pre-creados)
docker-compose up -d

# Opción B: manual
docker run -d --name rabbitmq \
  -p 5672:5672 -p 15672:15672 \
  -v rabbitmq_data:/var/lib/rabbitmq \
  rabbitmq:3.12-management
```

**Verificar:** Abrir http://localhost:15672 → usuario: `guest` / contraseña: `guest`

### Paso 2: Crear el Virtual Host `bank-security`

```bash
# Entrar al contenedor
docker exec -it rabbitmq-bank-security bash

# Crear el vhost
rabbitmqctl add_vhost bank-security

# Dar permisos al usuario guest
rabbitmqctl set_permissions -p bank-security guest ".*" ".*" ".*"
```

> Si usaste `docker-compose up` con el `definitions.json`, esto ya está hecho automáticamente.

### Paso 3: Compilar el proyecto

```bash
cd BankSecurityAlert

# Restaurar paquetes (RabbitMQ.Client 6.8.1)
dotnet restore src/Producer.csproj
dotnet restore src/Consumer.FraudDetection.csproj
dotnet restore src/Consumer.AlertDashboard.csproj
dotnet restore src/Consumer.AuditLog.csproj
```

### Paso 4: Ejecutar (4 terminales separadas)

**Terminal 1 — Consumer FraudDetection:**
```bash
dotnet run --project src/Consumer.FraudDetection.csproj
```

**Terminal 2 — Consumer AlertDashboard:**
```bash
dotnet run --project src/Consumer.AlertDashboard.csproj
```

**Terminal 3 — Consumer AuditLog:**
```bash
dotnet run --project src/Consumer.AuditLog.csproj
```

**Terminal 4 — Producer:**
```bash
dotnet run --project src/Producer.csproj
# Presiona ENTER para enviar una alerta, A para modo automático
```

---

## Configuración de RabbitMQ — Referencia

### Virtual Host
```
Nombre: /
```

### Exchanges
| Nombre | Tipo | Durable |
|---|---|---|
| `bank.alerts.topic` | topic |
| `bank.alerts.fanout` | fanout |
| `bank.alerts.direct` | direct |

### Queues
| Nombre | Tipo | TTL | Binding |
|---|---|---|---|
| `queue.fraud.detection` | classic | 24h | Topic: `critical.#`, `high.#` |
| `queue.audit.log` | classic | 24h | Topic: `*.frauddetection`, `*.loginattempt` |
| `queue.dashboard.fanout` | classic | 24h | Fanout: `""` |
| `queue.user.direct` | classic | 24h | Direct: `user.USR001` |

---

## Tecnologías

- **C# .NET 8** — Lenguaje y runtime
- **RabbitMQ.Client 6.8.1** — Librería oficial de RabbitMQ para .NET
- **RabbitMQ 3.12** — Message broker
- **Docker** — Contenedor para RabbitMQ

---

