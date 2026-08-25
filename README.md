# CloudWarehouse (+ PDA No-Order Reporting)

Solo internship project (NUS MTech SE): freight settlement for a cloud warehouse, plus a parallel PDA no-order shop-floor app.

**Repo:** https://github.com/chenyuxiangAK47/cloudwarehouse-csharp  
**Author:** Solo Intern (one developer). Second developer: N/A.

## What this repo is

| Deliverable | Stack (summary) | Status |
|-------------|-----------------|--------|
| CloudWarehouse | ASP.NET Core 9 modular monolith, Dapper, SQL Server, ClosedXML | MVP + Phase 2 billing/dual-track |
| PDA no-order reporting | Honeywell Android + Spring Boot + separate DB | Parallel MVP (source may live outside this repo; diagrams/docs here) |

Honest boundaries:

- Architecture is a **Modular Monolith** (intentional), not live microservices.
- **No production HA** (no load balancer / Always On claimed).
- CloudWarehouse and PDA are **not** production-API integrated this term.
- Built-in rule RAG is **assistive FAQ lookup** only; `FeeCalculationEngine` is the billing system of record.

## Quick start (CloudWarehouse)

1. SQL Server with schemas under `database/` (see `database/README.md`).
2. Configure connection via local `appsettings.json` (use `appsettings.example.json` as template — do not commit secrets).
3. Run:

```bash
dotnet run --project CloudWarehouse.Backend/CloudWarehouse.Backend.csproj --no-launch-profile
```

Typical demo URL: `http://localhost:5001`

```bash
dotnet test CloudWarehouse.sln
```

## CI / quality evidence

| Item | Path |
|------|------|
| CI workflow | `.github/workflows/ci.yml` |
| CodeQL SAST | `.github/workflows/codeql.yml` |
| Coverage | CI Artifact `coverage-report` (download from Actions run Summary) |
| Unit tests | `CloudWarehouse.Tests/` |
| Integration tests | `CloudWarehouse.IntegrationTests/` |

## Design & architecture evidence

Index: [`docs/diagrams/README.md`](docs/diagrams/README.md)

| Topic | Key files |
|-------|-----------|
| Strategy billing class diagram | `docs/diagrams/13-billing-strategy-class.puml` |
| Dual-track sequence | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| Logical / physical / deployment | `02`, `03`, `04` |
| DDD contexts / enterprise map | `05`, `16` |
| Monolith justification (report) | `docs/project-management/final-report-ch6-architecture-zh.md` |
| Mid-term feedback response | `docs/project-management/final-report-ch10-midterm-response-zh.md` |
| Solo hours (Planned vs Actual) | `docs/project-management/sprint-hours-chart-data.csv` + `sprint-hours-chart.html` |
| Speech scripts (EN/ZH) | `docs/speech-scripts/` |

Billing Strategy implementation: `CloudWarehouse.Pricing.Core/Billing/`.

## Personal hours (Solo)

Phase 1 personal Planned → Actual: **198h → 211h (+7%)**.  
CSV rows are **one person’s hours** (not a 3-person team total). Phase 2 CW/PDA rows: fill before final submission.

## AI assistance disclosure

See [`docs/project-management/ai-assistance-disclosure.md`](docs/project-management/ai-assistance-disclosure.md).

## Layout

```
CloudWarehouse.Backend/          Web API + wwwroot admin UI
CloudWarehouse.Pricing.Core/     Fee Strategy engine (library)
CloudWarehouse.Pricing.Api/      Optional thin pricing host
CloudWarehouse.Tests/            Unit tests
CloudWarehouse.IntegrationTests/ API / factory tests
database/                        SQL scripts
docs/diagrams/                   PlantUML
docs/project-management/         Report drafts, hours, review prompts
docs/speech-scripts/             Assessment video scripts
.github/workflows/               CI + CodeQL
```
