# SmartCommerce# SmartCommerce 🛒

A **production-grade, multi-tenant Order Management System** built with microservices, event-driven architecture, and a GenAI layer on AWS.

Built as a portfolio project targeting **Staff/Principal Engineer** roles and **AWS Certified Generative AI Developer (AIP-C01)** certification prep.

---

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    API Gateway                          │
└────────────────────────┬────────────────────────────────┘
                         │
        ┌────────────────┼─────────────────┐
        ▼                ▼                 ▼
┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
│ OrderService │ │InventorySvc  │ │NotificationService│
│ ASP.NET Core │ │ ASP.NET Core │ │   Lambda .NET     │
│  + DynamoDB  │ │  + Aurora    │ │   + SES/SNS       │
└──────┬───────┘ └──────┬───────┘ └──────────────────┘
       │                │
       └────────────────┘
                │
    ┌───────────▼───────────┐
    │   SNS / SQS Event Bus  │
    │  (Saga Choreography)   │
    └───────────────────────┘
                │
    ┌───────────▼───────────┐
    │     GenAI Layer        │
    │  Amazon Bedrock        │
    │  Knowledge Bases       │
    │  Agents + Guardrails   │
    └───────────────────────┘
```

---

## 🧰 Tech Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 10 |
| Services | ASP.NET Core 8 Web API |
| Database | Amazon DynamoDB (single-table design) |
| Messaging | AWS SNS + SQS |
| GenAI | Amazon Bedrock, Knowledge Bases, Agents |
| Observability | Serilog + AWS CloudWatch + X-Ray |
| IaC | AWS CDK (C#) |
| CI/CD | GitHub Actions |
| Local Dev | Docker + DynamoDB Local |

---

## 📋 Prerequisites

Make sure you have the following installed on your system:

| Tool | Version | Install |
|---|---|---|
| .NET SDK | 10.0+ | [dot.net](https://dot.net) |
| Docker Desktop | Latest | [docker.com](https://docker.com) |
| AWS CLI | v2 | [AWS Docs](https://docs.aws.amazon.com/cli/latest/userguide/install-cliv2.html) |
| Git | Latest | [git-scm.com](https://git-scm.com) |

---

## 🚀 Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/vikramvee/SmartCommerce
cd SmartCommerce
```

### 2. Verify .NET is installed

```bash
dotnet --version
# Expected: 10.x.x
```

If not installed, download from [dot.net/download](https://dot.net/download)

### 3. Restore NuGet packages

```bash
cd SmartCommerce
dotnet restore
```

### 4. Configure AWS CLI for local development

```bash
aws configure
```

Enter these values for local development:

```
AWS Access Key ID:     local
AWS Secret Access Key: local
Default region name:   us-east-1
Default output format: json
```

> ⚠️ For real AWS deployment, use your actual AWS credentials.

### 5. Start local infrastructure

```bash
# From the SmartCommerce/ folder
docker-compose up -d
```

This starts:
- **DynamoDB Local** on port `8000`
- **DynamoDB Admin UI** on port `8001`
- **OrderService** on port `5001`

### 6. Create DynamoDB tables

```bash
chmod +x scripts/create-tables.sh
./scripts/create-tables.sh
```

Expected output:
```
Waiting for DynamoDB Local to be ready...
Creating SmartCommerce table...
✅ SmartCommerce table created!
```

### 7. Verify services are running

```bash
# Health check
curl http://localhost:5001/api/orders/health-check
```

Expected response:
```json
{
  "service": "OrderService",
  "status": "Healthy",
  "time": "2026-04-07T..."
}
```

### 8. Open Swagger UI

Navigate to [http://localhost:5001](http://localhost:5001) in your browser.

### 9. Open DynamoDB Admin UI

Navigate to [http://localhost:8001](http://localhost:8001) to visually inspect your DynamoDB tables.

---

## 🗂️ Project Structure

```
SmartCommerce/
├── .devcontainer/
│   └── devcontainer.json        # GitHub Codespaces config
├── SmartCommerce/
│   ├── SmartCommerce.slnx       # Solution file
│   ├── docker-compose.yml       # Local dev infrastructure
│   ├── scripts/
│   │   └── create-tables.sh     # DynamoDB table setup
│   ├── OrderService/            # Order microservice
│   │   ├── Controllers/
│   │   ├── Domain/
│   │   │   ├── Entities/        # Order, OrderItem
│   │   │   └── Events/          # Domain events
│   │   ├── Application/
│   │   │   ├── Commands/        # PlaceOrderCommand
│   │   │   └── Queries/         # GetOrderQuery
│   │   └── Infrastructure/
│   │       ├── DynamoDB/        # Repository
│   │       └── Outbox/          # Outbox pattern
│   ├── InventoryService/        # Inventory microservice
│   ├── NotificationService/     # Notification microservice
│   └── SmartCommerce.Contracts/ # Shared events + DTOs
└── README.md
```

---

## 🧪 Running Tests

```bash
cd SmartCommerce
dotnet test
```

---

## 🐳 Docker Commands Reference

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f order-service

# Rebuild after code changes
docker-compose up --build
```

---

## 🗺️ 12-Week Roadmap

| Week | Focus | Status |
|---|---|---|
| 1–2 | Foundation — scaffold, DynamoDB, Docker | ✅ Done |
| 3–4 | Event-driven — SNS/SQS, Outbox, Saga | 🔄 In Progress |
| 5–6 | GenAI I — Bedrock RAG + Knowledge Bases | ⏳ Upcoming |
| 7–8 | GenAI II — Bedrock Agents + Flows | ⏳ Upcoming |
| 9–10 | AI Safety — Guardrails, PII, Governance | ⏳ Upcoming |
| 11–12 | Observability — RAGAS, CloudWatch, CI/CD | ⏳ Upcoming |

---

## 🔑 Key Patterns Implemented

- **Outbox Pattern** — guaranteed at-least-once event delivery
- **Saga Choreography** — distributed transaction across services
- **DynamoDB Single-Table Design** — multi-tenant key structure
- **Clean Architecture** — domain, application, infrastructure layers
- **RAG Pipeline** — Bedrock Knowledge Bases + OpenSearch *(coming Week 5)*
- **Agentic AI** — Bedrock Agents + Lambda action groups *(coming Week 7)*

---

## ☁️ AWS Services Used

- **Amazon DynamoDB** — multi-tenant order storage
- **Amazon SNS / SQS** — event-driven messaging
- **Amazon Bedrock** — foundation model integration
- **AWS Lambda** — serverless event consumers
- **Amazon CloudWatch** — logging and observability
- **AWS CDK** — infrastructure as code

---

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch — `git checkout -b feat/your-feature`
3. Commit using Conventional Commits — `git commit -m "feat: add order cancellation"`
4. Push and open a Pull Request

---

## 📄 License

MIT License — see [LICENSE](LICENSE) for details.

---

> Built with ❤️ as a Staff Engineer portfolio project
