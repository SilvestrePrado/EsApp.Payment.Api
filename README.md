# 💳 Prueba Técnica — Microservicio de Pagos con Evaluación de Riesgo (Kafka + EF Core + Docker)

Hola!, Este proyecto implementa un **microservicio de pagos** que simula el flujo de evaluación de riesgo mediante **Apache Kafka**.  
Incluye arquitectura (Domain, Application, Infrastructure, API), base de datos SQL Server, y un proceso adicional para evaluación automática de riesgo.

---

## 🧱 Arquitectura General

```
Payment.Api             → API principal (Endpoints HTTP)
Payment.Application     → Lógica de negocio y DTOs
Payment.Domain          → Entidades de dominio (Payment, etc.)
Payment.Infrastructure  → Persistencia EF Core + Kafka Consumer
RiskSimulatorApp        → Simulador de evaluación de riesgo (Productor Kafka)
```

---

## 🚀 Requisitos Previos

- Docker Desktop o Docker Engine
- .NET 8 SDK
- SQL Server (ya se levanta con Docker)
- Apache Kafka (también corre en Docker)

---

## 🐳 Cómo levantar todo con Docker

1️⃣ **Levanta el entorno de base de datos y Kafka**
```bash
docker compose up -d
```

Esto levanta:
- **SQL Server** → puerto `1433`
- **Zookeeper** → puerto `2181`
- **Kafka** → puerto `29092`

---

2️⃣ **Verifica que los contenedores estén corriendo**
```bash
docker ps
```

Debes ver algo como:
```
zookeeper        Up  (healthy)
kafka            Up  (healthy)
sqlserver        Up  (healthy)
```

---

3️⃣ **Crea los topics en Kafka**
```bash
docker exec -it kafka bash -c "kafka-topics --bootstrap-server localhost:29092 --create --topic risk-evaluation-request --partitions 1 --replication-factor 1"
docker exec -it kafka bash -c "kafka-topics --bootstrap-server localhost:29092 --create --topic risk-evaluation-response --partitions 1 --replication-factor 1"
```

Verifica:
```bash
docker exec -it kafka bash -c "kafka-topics --bootstrap-server localhost:29092 --list"
```

---

## ⚙️ Configuración del proyecto

Antes de correr las APIs, aplica las migraciones de Entity Framework:

```bash
cd Payment.Api
dotnet ef database update -p ../Payment.Infrastructure -s .
```
O si ya estas dentro de Payment-proyect\Payment.Api\Payment.Api
```bash
dotnet ef migrations add InitialCreate -p ../Payment.Infrastructure -s Payment.Api.csproj
```

Esto creará la base de datos **PaymentDb** y la tabla **Payments** en SQL Server.

---

## ▶️ Cómo levantar las APIs

### 1️⃣ Inicia el microservicio principal (Payment API)
```bash
cd Payment.Api
dotnet run
```

### 2️⃣ Inicia el simulador de riesgo
En otra terminal:
```bash
cd RiskSimulatorApp
dotnet run
```

---

## 📡 Endpoints — Payment.Api

### 🟩 **POST /api/payments**
Crea una nueva solicitud de pago.

#### Request
```json
{
  "customerId": "cfe8b150-2f84-4a1a-bdf4-923b20e34973",
  "serviceProviderId": "5fa3ab5c-645f-4cd5-b29e-5c5c116d7ea4",
  "paymentMethodId": 2,
  "amount": 150.00
}
```

#### Respuesta esperada
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "status": "evaluating"
}
```

> 🔹 Internamente el sistema enviará un mensaje a Kafka (`risk-evaluation-request`).

---

### 🟦 **GET /api/payments/{externalOperationId}**
Consulta el estado actual de una operación de pago.

#### Ejemplo
```
GET /api/payments/2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23
```

#### Respuesta
```json
{
  "externalOperationId": "2a7fb0cd-4c1c-4e6e-b8f9-ef83bb14cf23",
  "createdAt": "2025-07-17T08:15:30Z",
  "status": "accepted"
}
```

---

## ⚙️ Flujo Kafka — Evaluación de Riesgo

### 1️⃣ PaymentProducer (API)
- Envía mensaje al topic `risk-evaluation-request`:
```json
{
  "externalOperationId": "GUID",
  "customerId": "GUID",
  "amount": 150.00
}
```

### 2️⃣ RiskSimulator (servicio independiente)
- Escucha `risk-evaluation-request`
- Evalúa reglas:
  - Si `amount > 2000` → denied
  - Si acumulado diario > 5000 → denied
  - Caso contrario → accepted
- Envía respuesta al topic `risk-evaluation-response`

### 3️⃣ RiskResponseConsumerService (en Payment.Infrastructure)
- Escucha `risk-evaluation-response`
- Actualiza estado en base de datos:
  - `status = accepted` o `denied`
  - `updatedAt = DateTime.UtcNow`

---

## 🧩 Ejemplo de flujo completo

1️⃣ Envías un pago desde Postman  
   → `POST /api/payments`

2️⃣ El PaymentProducer manda un mensaje a Kafka (`risk-evaluation-request`)

3️⃣ El RiskSimulator procesa la solicitud y publica la respuesta (`risk-evaluation-response`)

4️⃣ El RiskResponseConsumerService actualiza la base de datos automáticamente.

5️⃣ Consultas con  
   → `GET /api/payments/{externalOperationId}`

---

## 🧰 Tecnologías Principales

- **.NET 8**
- **Entity Framework Core**
- **Confluent.Kafka**
- **Docker Compose**
- **SQL Server 2018**
- **Clean Architecture**

---

## 🧑‍💻 Buenas prácticas implementadas

✔️ Arquitectura por capas: Domain, Application, Infrastructure, Api  
✔️ Inyección de dependencias (DI) limpia  
✔️ Logging con manejo de excepciones  
✔️ BackgroundService para consumo Kafka  
✔️ Configuración flexible por `appsettings.json`  
✔️ Código documentado y fácil de extender

---

## ⚠️ Solución de problemas

Si Kafka no levanta:
```bash
docker compose down
docker compose up -d
```

Si no puedes crear los topics:
```bash
docker exec -it kafka bash
kafka-topics --bootstrap-server localhost:29092 --list
```

Si EF no migra:
```bash
dotnet ef migrations add InitialCreate -p ../Payment.Infrastructure -s .
dotnet ef database update -p ../Payment.Infrastructure -s .
```

---

## 🧩 Swagger UI

Al levantar la API, abre en tu navegador:
```
https://localhost:7103/swagger o http://localhost:5264
```

Allí podrás probar los endpoints directamente.

---

## 🏁 Conclusión

✅ Flujo completo de pago con evaluación de riesgo vía Kafka  
✅ Base de datos persistente con EF Core  
✅ Simulación de riesgo independiente  
✅ Proyecto profesional con Docker y arquitectura limpia  

---

## ✍️ Autor

**Silvestre Alejandro Prado Aldunate**  
Desarrollador Full Stack / Móvil — Flutter & .NET  
Bolivia 🇧🇴
