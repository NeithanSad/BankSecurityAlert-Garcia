# Bank Security Alert — Arquitectura y Microservicios

Resumen

Este repositorio implementa una arquitectura basada en microservicios para la generación, distribución y auditoría de alertas de seguridad bancarias. La comunicación entre servicios se realiza mediante RabbitMQ; el diseño demuestra patrones de enrutamiento típicos (topic, fanout y direct) y ofrece una API de auditoría para consultas.

Componentes principales

- `Producer` (publicador): genera `SecurityAlert` y publica mensajes hacia los exchanges configurados en RabbitMQ.
- `Consumer.FraudDetection` (detector de fraude): procesa alertas de severidad alta/ crítica mediante un exchange tipo Topic.
- `Consumer.AlertDashboard` (panel): recibe todas las alertas mediante un exchange tipo Fanout para monitoreo en tiempo real.
- `Consumer.AuditLog` (auditoría): consume mensajes relevantes y persiste entradas en SQLite; expone una API REST para acceso a los registros.
- `Shared`: contiene modelos de dominio, configuración de RabbitMQ y utilidades compartidas.

Topología de RabbitMQ (resumen)

- Exchanges:
  - `bank.alerts.topic` (topic)
  - `bank.alerts.fanout` (fanout)
  - `bank.alerts.direct` (direct)

- Queues:
  - `queue.fraud.detection` (vinculada al topic con `critical.#` y `high.#`)
  - `queue.audit.log` (vinculada al topic con `*.frauddetection` y `*.loginattempt`)
  - `queue.dashboard.fanout` (vinculada al fanout para recibir todas las alertas)
  - `queue.user.direct` (ejemplo de cola para notificaciones directas por usuario)

- Routing keys:
  - Topic routing key: `{severity}.{category}` (por ejemplo, `critical.frauddetection`).
  - Direct routing key: `user.{UserId}` (por ejemplo, `user.USR001`).

Microservicios y endpoints

1) Producer
- Ubicación: `src/Producer`
- Tipo: aplicación de consola
- Función: genera alertas y publica a los exchanges Topic, Fanout y (condicionalmente) Direct.
- Variables de entorno importantes:
  - `RABBITMQ_HOST` (opcional): host de RabbitMQ; si no está definido, se usa `localhost`.
- Ejecución: `dotnet run --project src/Producer`

2) Consumer.FraudDetection
- Ubicación: `src/Consumer.FraudDetection`
- Tipo: aplicación de consola
- Exchange: `bank.alerts.topic`
- Cola: `queue.fraud.detection`
- Patrones de binding: `critical.#`, `high.#`
- Ejecución: `dotnet run --project src/Consumer.FraudDetection`

3) Consumer.AlertDashboard
- Ubicación: `src/Consumer.AlertDashboard`
- Tipo: aplicación de consola
- Exchange: `bank.alerts.fanout`
- Cola: `queue.dashboard.fanout` (recibe todas las alertas)
- Ejecución: `dotnet run --project src/Consumer.AlertDashboard`

4) Consumer.AuditLog (API)
- Ubicación: `src/Consumer.AuditLog`
- Tipo: ASP.NET minimal API + Hosted Service
- Cola: `queue.audit.log`
- Persistencia: SQLite (archivo por defecto `audit.db`, configurable con `DB_PATH`).

Endpoints HTTP expuestos por `Consumer.AuditLog`:
- `GET /api/alerts` — Retorna la lista paginada de entradas consumidas.
  - Parámetros opcionales: `page` (por defecto 1), `pageSize` (por defecto 20).
- `GET /api/alerts/{id}` — Retorna el detalle de una entrada de auditoría por identificador.
- `GET /api/alerts/stats` — Retorna estadísticas agregadas por severidad y por categoría.
- `GET /health` — Health check básico del servicio.

Ejemplos de uso (API)

- Obtener la primera página de alertas:
  - curl "http://localhost:8080/api/alerts"
- Obtener detalle de la alerta con id 10:
  - curl "http://localhost:8080/api/alerts/10"
- Obtener estadísticas de auditoría:
  - curl "http://localhost:8080/api/alerts/stats"

Requisitos y ejecución

Requisitos previos:
- .NET 8 SDK
- RabbitMQ (local o en contenedor)

Ejecutar RabbitMQ con Docker (ejemplo):

docker run -d --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:3.12-management

Crear vhost (opcional):

docker exec -it rabbitmq bash
rabbitmqctl add_vhost bank-security
rabbitmqctl set_permissions -p bank-security guest ".*" ".*" ".*"

Compilar y ejecutar servicios (desde la raíz del repositorio):

- `dotnet run --project src/Consumer.FraudDetection`
- `dotnet run --project src/Consumer.AlertDashboard`
- `dotnet run --project src/Consumer.AuditLog`
- `dotnet run --project src/Producer`

Configuración importante

- `RABBITMQ_HOST`: host de RabbitMQ (si no está definido, se usa `localhost`).
- `DB_PATH`: ruta del archivo SQLite para `Consumer.AuditLog`.

Archivos relevantes para la configuración

- `src/Shared/Infrastructure/Config/RabbitMQConstants.cs`: nombres de exchanges, queues y routing keys.
- `src/Shared/Infrastructure/RabbitMQ/RabbitMQTopology.cs`: declara exchanges, queues y bindings al iniciar.
- `src/Shared/Infrastructure/RabbitMQ/AlertPublisher.cs`: lógica de publicación a Topic, Fanout y Direct.

Recomendaciones

- Añadir autenticación y autorización a la API de auditoría para entornos de producción.
- Externalizar configuración sensible (credenciales, hosts) mediante variables de entorno o un secret manager.
- Considerar un almacén de métricas para el monitoreo histórico y alertas operacionales.

Contribuciones y contacto

Para contribuir o reportar problemas, abra un issue o cree un pull request en este repositorio.
