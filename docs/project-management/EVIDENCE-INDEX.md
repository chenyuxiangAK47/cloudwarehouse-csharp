# Examiner evidence index (short)

Use this when a reviewer claims “nothing in the repo”.

| Supervisor ask | Where in this repo |
|----------------|--------------------|
| Personal Planned vs Actual | `sprint-hours-chart-data.csv` (`Person=Solo Intern`), `sprint-hours-chart.html` |
| **Jira / Sprint / Burndown** | `jira/product-backlog.csv`, `jira/sprint-burndown-points.csv`, `jira/burndown-board.html` (open → screenshot), `.github/ISSUE_TEMPLATE/sprint-story.yml` |
| Strategy + detailed design | `CloudWarehouse.Pricing.Core/Billing/*`, `docs/diagrams/13-*.puml`, `14-sequence-waybill-dual-track.puml` |
| Multi-view architecture | `docs/diagrams/02,03,04,05,16` + `final-report-ch6-architecture-zh.md` |
| Monolith justification + extract triggers | `final-report-ch6-architecture-zh.md`, `01a` / `01b` puml |
| Mid-term response | `final-report-ch10-midterm-response-zh.md` |
| CI / coverage / SAST | `.github/workflows/ci.yml`, `codeql.yml`, Actions artifacts |
| **Playwright E2E** | `CloudWarehouse.E2ETests/`, `artifacts/e2e-playwright-test.txt` |
| **IaC (Bicep + Terraform + Compose)** | `infra/bicep/`, `infra/terraform/`, `docker-compose.yml`, `Dockerfile`, `.github/workflows/iac.yml` |
| AI tool use | `ai-assistance-disclosure.md` |
| Root entry | `/README.md` |

Phase 2 hours are filled in `sprint-hours-chart-data.csv` and report §4.7.3.
