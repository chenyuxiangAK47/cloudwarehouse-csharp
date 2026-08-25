# CloudWarehouse (+ PDA No-Order Reporting)

Solo internship project (NUS MTech SE): freight settlement for a cloud warehouse, plus a parallel PDA no-order shop-floor app.

**Repo:** https://github.com/chenyuxiangAK47/cloudwarehouse-csharp  
**Author:** Solo Intern (one developer).

**Final report masters (edit these, not Word first):**

- ZH: `docs/project-management/Final-Report-ZH-Master.md`
- EN: `docs/project-management/Final-Report-EN-Master.md`

## Deliverables

| Deliverable | Stack | Status |
|-------------|-------|--------|
| CloudWarehouse | ASP.NET Core 9 Modular Monolith, Dapper, SQL Server, ClosedXML | MVP + Phase 2 Strategy billing / dual-track |
| PDA no-order reporting | Honeywell Android + Spring Boot + separate DB | Parallel MVP (diagrams/docs in this repo) |

Scope notes used in the report:

- Architecture: **Modular Monolith** (microservices extraction = backlog trigger).
- Rule RAG: assistive FAQ only; `FeeCalculationEngine` is system of record for fees.
- CW ↔ PDA production API join: backlog (contexts remain separate this term).

## Supervisor checklist (implemented in this repo)

| Item | Implementation |
|------|----------------|
| Analysis | Report §3.0 in ZH/EN masters |
| Class/object sequence | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| Sprint + backlog + burndown | `docs/project-management/jira/` (Jira CSV import + `burndown-board.html`) |
| Unit / integration / load | `CloudWarehouse.Tests`, `CloudWarehouse.IntegrationTests` |
| Coverage | CI Coverlet + ReportGenerator artefact |
| Playwright UI E2E | `CloudWarehouse.E2ETests` (4 smokes, Category=E2E) |
| SAST | `.github/workflows/codeql.yml` |
| DAST | `.github/workflows/dast-zap.yml` (OWASP ZAP baseline artefact) |
| Security resolution | NuGet scan in CI + Demo JWT + DAST artefact |
| IaC | `infra/bicep`, `infra/terraform`, `docker-compose.yml`, `.github/workflows/iac.yml` |
| Demo JWT | `POST /api/auth/token` — `CloudWarehouse.Backend/Modules/Security/DemoAuthController.cs` |
| Reflection | Report Chapter 12 |
| Evidence index | `docs/project-management/EVIDENCE-INDEX.md` |

## Quick start (CloudWarehouse)

1. SQL Server with scripts under `database/` (see `database/README.md`).
2. Copy `appsettings.example.json` → local `appsettings.json` (do not commit secrets).
3. Run:

```bash
dotnet run --project CloudWarehouse.Backend/CloudWarehouse.Backend.csproj --no-launch-profile
```

Demo URL: `http://localhost:5001`

```bash
dotnet test CloudWarehouse.sln
# UI E2E only:
dotnet test CloudWarehouse.E2ETests --filter Category=E2E
```

### Demo JWT (optional)

```bash
# enable via appsettings.DemoJwt.json or env Auth__DemoJwt__Enabled=true
curl -X POST http://localhost:5001/api/auth/token -H "Content-Type: application/json" -d "{\"username\":\"demo\",\"password\":\"demo\"}"
```

### IaC / Compose

```bash
docker compose up -d --build
# Azure:
# az deployment group create ... -f infra/bicep/main.bicep
# terraform -chdir=infra/terraform apply
```

See `infra/README.md`.

## CI / quality evidence

| Item | Path |
|------|------|
| CI + tests + coverage | `.github/workflows/ci.yml` |
| CodeQL SAST | `.github/workflows/codeql.yml` |
| IaC validate | `.github/workflows/iac.yml` |
| DAST ZAP | `.github/workflows/dast-zap.yml` |
| Pages QA report | `.github/workflows/pages.yml` |
| Unit tests | `CloudWarehouse.Tests/` |
| Integration tests | `CloudWarehouse.IntegrationTests/` |
| Playwright E2E | `CloudWarehouse.E2ETests/` |
| Local artefacts | `docs/project-management/artifacts/` |

## Design & architecture evidence

Index: [`docs/diagrams/README.md`](docs/diagrams/README.md)

| Topic | Key files |
|-------|-----------|
| Strategy billing class diagram | `docs/diagrams/13-billing-strategy-class.puml` |
| Dual-track sequence (class/object) | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| Logical / physical / deployment | `02`, `03`, `04` |
| DDD contexts / enterprise map | `05`, `16` |
| Jira backlog / burndown | `docs/project-management/jira/` |
| Solo hours Planned vs Actual | `sprint-hours-chart-data.csv` + `sprint-hours-chart.html` |
| Speech scripts | `docs/speech-scripts/` |

Billing Strategy: `CloudWarehouse.Pricing.Core/Billing/`.

## Personal hours (Solo)

- Phase 1: **198h → 211h (+7%)**
- Phase 2 + grand total: see `docs/project-management/sprint-hours-chart-data.csv` / report §4.7.3 (**328 → 350**)

## AI assistance disclosure

See [`docs/project-management/ai-assistance-disclosure.md`](docs/project-management/ai-assistance-disclosure.md).

## Layout

```
CloudWarehouse.Backend/           Web API + wwwroot + Demo JWT
CloudWarehouse.Pricing.Core/      Fee Strategy engine
CloudWarehouse.E2ETests/          Playwright UI smokes
CloudWarehouse.Tests/             Unit tests
CloudWarehouse.IntegrationTests/  API / factory tests
infra/bicep|terraform/            Azure IaC
docker-compose.yml / Dockerfile   Container path
database/                         SQL scripts
docs/diagrams/                    PlantUML
docs/project-management/          Report masters, jira/, artifacts/
.github/workflows/                ci, codeql, iac, dast-zap, pages
```
