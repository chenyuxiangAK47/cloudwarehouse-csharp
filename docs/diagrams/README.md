# CloudWarehouse UML Diagrams (PlantUML)

Render online: https://www.plantuml.com/plantuml/uml/

| File | Diagram |
|------|---------|
| `01-architecture-constraints-decisions.puml` | Combined (compact top/bottom) |
| `01a-architecture-decisions-adr.puml` | **Slide top:** ADR only |
| `01b-architecture-constraints-influence.puml` | **Slide bottom:** constraints → outcomes |
| `02-logical-architecture.puml` | Logical architecture |
| `03-physical-architecture.puml` | Physical architecture |
| `04-deployment-diagram.puml` | Deployment |
| `05-ddd-bounded-contexts.puml` | DDD contexts |
| `06-use-case-diagram.puml` | Use cases |
| `07-erd.puml` | ERD |
| `08-sequence-import.puml` | Import sequence |
| `09-cicd-pipeline.puml` | CI/CD activity |
| `13-billing-strategy-class.puml` | **Billing Strategy Pattern class diagram** (Phase 2) |
| `14-sequence-waybill-dual-track.puml` | **Waybill preview dual-track pricing sequence** (FE → API → DualTrack → Strategy → DB) |

DevSecOps evidence (repo root / CI):
- `.github/workflows/ci.yml` — tests, coverage, NuGet vulnerable scan artifact
- `.github/workflows/codeql.yml` — **CodeQL SAST** (C#)
- `Dockerfile` — optional container image for Trivy demos

VS Code: install **PlantUML** extension, open `.puml`, press `Alt+D`.

Each file ends with 3 English presentation sentences (comments).

## Fixes applied vs. earlier drafts

1. **Diagram 1:** Removed invalid `\| table \|` syntax inside `@startuml` (not supported in component diagrams).
2. **Diagram 4:** CI no longer incorrectly links local Backend to dotnet SDK; workflow runs independently on GitHub.
3. **Diagram 5:** `PriceRule` fields match code (`BillingType`, `MinWeight`, `MaxWeight`, etc.); renamed Import package to avoid reserved words; removed wrong `FirstWeight/FirstPrice` fields.
4. **Diagram 5 (Destination):** Uses `Area` not `Address` to match `Destination.cs`.
5. All labels and presentation scripts are in **English**.
