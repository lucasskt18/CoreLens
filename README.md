# CoreLens

Monitor de hardware Windows em tempo real. Um agent no host lê CPU, RAM, disco, rede e sensores; a API empurra as métricas ao vivo para um dashboard Angular. Histórico fica em TimescaleDB. A ingestão não chama IA — existe só a porta `IInsightProvider` para plugar isso depois.

## Arquitetura

```
Windows host                         Docker
┌─────────────┐   HTTP batch    ┌──────────┐     ┌─────────────┐
│ Agent (.NET)│ ──────────────► │ Core API │ ──► │ TimescaleDB │
└─────────────┘                 │ SignalR  │     └─────────────┘
                                └────┬─────┘
                                     │ WebSocket
                                ┌────▼─────┐
                                │ Angular  │
                                └──────────┘
```

| Peça | Função |
| --- | --- |
| **Hardware Agent** | Worker no Windows. GUID da máquina, Performance Counters / APIs nativas + LibreHardwareMonitor. Não roda em container. |
| **Core API** | ASP.NET Core, Clean Architecture, ingest em batch, broadcast SignalR, alertas por limiar. |
| **Dashboard** | SPA Angular: painéis de CPU, RAM, disco, rede, GPU e temperaturas. |
| **TimescaleDB** | Série temporal estreita (`metric_samples`). Bruto 24h, 1 min por 30 dias, 1 h por 90 dias. |

## Stack

- Agent e API: C# / .NET 8
- Tempo real: SignalR
- Frontend: Angular + ECharts
- Banco: TimescaleDB (PostgreSQL) via Docker

## Como rodar

1. Docker Desktop (só o banco)
2. .NET 8 SDK e Node.js

```powershell
.\start.ps1
```

- Dashboard: http://localhost:4200
- API: http://localhost:5080

Ou, em terminais separados:

```powershell
docker compose up -d
dotnet run --project src/CoreLens.Api
dotnet run --project src/CoreLens.Agent
cd frontend; npm start
```

Token padrão do agent: `dev-local-token-change-me`. Temperaturas e GPU podem aparecer como N/A se o agent não estiver elevado.

## Fora do v1

gRPC, agent Linux, UI de frota, ML.NET/OpenAI e instalador MSI. O contrato já carrega `computerId` + token para receber mais de uma máquina no futuro.
