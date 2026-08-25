# CloudWarehouse Freight Settlement & PDA No-Order Reporting — Final Internship Report (English)

## Final Internship Report (English Master)

This document is the English counterpart of `Final-Report-ZH-Master.md`, prepared for NUS MTech Software Engineering internship assessment. **Facts follow the Chinese master and corrected chapter drafts; figure placeholder blocks remain in Chinese** so that PNG paste instructions stay authoritative when converting to Word.

---

# Chapter 1 Project Overview

## 1.1 Background and One-Sentence Summary

This internship project addresses operational pain points in manufacturing and cloud-warehouse settings through digital systems. The dual deliverables are: (1) **CloudWarehouse**, an ASP.NET Core 9 freight settlement system supporting rate-rule import, fee trial calculation, and dual-track receivable/payable reconciliation on waybills; and (2) a **MES no-order shop-floor reporting** application built with a Spring Boot API and Honeywell PDA terminals, enabling start/report capture when no formal work order exists (e.g., night shifts). As a **solo intern**, the author owned the full lifecycle from requirements and design through implementation, testing, and documentation. Although both systems serve the same factory goal, they evolve as separate **bounded contexts** and are **not** production-integrated at the API level in this delivery.

## 1.2 Business Pain Points

**Warehouse / settlement side (addressed by CloudWarehouse):**

- **Data silos and inconsistent formats:** Supplier and carrier quotes and bills remain Excel-centric, with highly inconsistent layouts (including multi-level / three-row headers), hindering standardisation.
- **High reconciliation risk:** Manual reconciliation is error-prone and lacks version control. Computing historical bills with “latest prices” introduces systematic amount drift and weak auditability.
- **No repeatable trial-calculation path:** Without preview-and-submit freight trials, cost–revenue gaps cannot be estimated before formal settlement.

**Shop-floor side (addressed by the PDA application):**

- **Blind spots without formal orders:** Night shifts or rush inserts often lack MES work orders; paper or verbal reporting is hard to trace and easy to lose.
- **Hardware fit:** The shop floor needs industrial handheld PDAs with barcode scanning and an ultra-simple workflow for reliable persistence.

## 1.3 Project Objectives

**CloudWarehouse:**

- Maintainable master data (sites, destinations, customers).
- Structured Excel import with automatic header detection, following a full **preview → validate → transactional commit** path for cost rates and customer quotes.
- **Dual-track trial calculation:** waybill preview comparing **receivable quotes** vs **payable costs**, strictly using **historical rates as of ship / bill date**.
- Extensibility via the **Strategy Pattern** for billing variants (tier, overweight, volumetric).
- Engineering practice: automated tests, GitHub Actions CI, and CodeQL SAST.

**PDA no-order reporting:**

- Minimal closed loop: login → select line / machine group / machine → start / report / query.
- Hardware scanning with API persistence so work events remain traceable without formal orders.

## 1.4 Scope Boundaries

**In scope for this delivery:**

- CloudWarehouse MVP as a **Modular Monolith**, including Phase 2 billing engine, dual-track reconciliation, and rule retrieval.
- PDA no-order reporting MVP.
- Architecture / design diagrams, CI/CD configuration, and test evidence.

**Explicitly out of scope:**

- Production-grade integration bus between CloudWarehouse and PDA.
- Microservice production deployment (Modular Monolith retained).
- Full production JWT/RBAC authentication (deferred in ADR).
- Production high-availability (HA) clusters.
- Experimental “AI billing” or “RAG replacing the settlement engine”.

## 1.5 Stakeholders

- **Warehouse / settlement staff:** primary CloudWarehouse users; care about reconciliation speed and accuracy.
- **Line operators:** primary PDA users; care about ease of use and scan responsiveness.
- **Industry mentor / business owner:** clarifies requirements and demo feedback; scoring affects internship evaluation.
- **Academic supervisor:** focuses on design depth, patterns, multi-view architecture completeness, and substantive effort evidence.

## 1.6 Response Strategy to Mid-term Feedback

Mid-term feedback noted that the system seemed “too simple”, that the monolith needed justification, that billing variants needed design patterns, and that multi-view architecture diagrams were insufficient. Later chapters respond as follows (index only here):

- **Design depth:** Strategy Pattern class and sequence diagrams in Software Design.
- **Architecture rationale:** logical, physical, deployment, and DDD enterprise context-map views arguing for Modular Monolith.
- **Engineering evidence:** CI screenshots, coverage artefacts, and CodeQL results in DevSecOps / QA.
- **Effort evidence:** Planned vs Actual hours in Project Management.
- **Value increment:** dual-track historical pricing and PDA hardware closed-loop on the shop floor.

## 1.7 Deliverables Snapshot


> **【插图占位 1-1】** 系统首页 / 管理端总览（可选）
> - 来源：`本机运行截图 wwwroot/index.html`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 1-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：CloudWarehouse 管理端入口，证明可运行系统。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

| Deliverable | Status | Notes |
| --- | --- | --- |
| CloudWarehouse system | ✅ Completed | Import, trial calculation, dual-track waybill reconciliation |
| Billing Strategy Pattern | ✅ Completed | Class diagram, sequence diagram, detailed design |
| CI/CD and quality scanning | ✅ Completed | Automated test suite and CodeQL integration |
| Built-in rule RAG | ✅ Completed | FAQ lookup only; does **not** participate in settlement |
| PDA no-order reporting MVP | ✅ Completed | Honeywell PDA client |
| Final demo videos / report | 🔄 In progress | Includes seven assessment demo videos |

---

# Chapter 2 Technology Stack and Key Technical Decisions

Chapter 1 defined dual-system goals and boundaries. This chapter explains technology choices that support those goals and provides context for architecture, software design, and DevSecOps chapters that follow.

## 2.1 Technology Stack Overview

**CloudWarehouse**

| Layer | Technology |
| --- | --- |
| Language / runtime | C# / .NET 9, ASP.NET Core |
| API | REST Controllers (modules: MasterData / Import / Pricing / Billing / Assistant) |
| Front end | Static HTML/JS (`wwwroot/index.html`), not a React/Angular SPA |
| Data access | Dapper + parameterised SQL |
| Database | Microsoft SQL Server |
| Excel | ClosedXML |
| Billing core | `CloudWarehouse.Pricing.Core` (Strategy: Tier / Overweight / Volumetric) |
| Testing | xUnit, `WebApplicationFactory` integration tests, light concurrency / load smoke tests |
| CI / security | GitHub Actions, Coverlet / ReportGenerator, CodeQL SAST |

**PDA no-order reporting**

| Layer | Technology |
| --- | --- |
| Client | Android (Honeywell industrial PDA) + scan SDK |
| API | Spring Boot 3.x / Java 21 |
| Database | SQL Server (dedicated DB such as `PDA_NoOrder`) |
| Communication | Intranet HTTP (cleartext, matching shop-floor demo constraints) |

The dual stack is intentional (.NET settlement domain vs Java/Android shop-floor domain), not technology sprawl.

## 2.2 Layer Mapping (CloudWarehouse)

Request path:

1. **Presentation:** browser static pages  
2. **API:** Controllers for validation and orchestration  
3. **Application / Services:** Import / Calculate / BillImport / QuoteAssistant  
4. **Domain / Helpers + Pricing.Core:** Excel parsing, rule mapping, `FeeCalculationEngine` + Strategies  
5. **Infrastructure:** Dapper, `SqlConnection`, upload size / type limits  

Logical architecture reference: `docs/diagrams/02-logical-architecture.puml`.

## 2.3 Key Selection Comparisons

| Decision | Alternatives | Choice | Rationale |
| --- | --- | --- | --- |
| Data access | EF Core vs Dapper | Dapper | Import-heavy; fine-grained SQL/transactions; more controllable under solo timeline |
| Architecture style | Microservices vs Modular Monolith | Modular Monolith | Solo delivery, same-DB transactions; modules/DDD leave extraction seams |
| UI | SPA vs static pages | Static HTML/JS | Effort on backend architecture and billing design for MVP |
| Excel | EPPlus vs ClosedXML | ClosedXML | Complex header read + template/export in one library |
| Rule maintenance | Manual CRUD price tables vs Excel-first | Excel import primary | Fits supplier/carrier workflows (ADR) |
| Billing extension | Giant if/else vs Strategy | Strategy Pattern | Mid-term feedback; open–closed (volumetric verified) |
| CI | Local-only tests vs GitHub Actions | Actions | Verifiable evidence, coverage artefacts |
| SAST | None vs CodeQL | CodeQL | DevSecOps evidence; no claim of full DAST |

| PDA client | Mobile H5 vs native Android | Native Android | Shop-floor durability and scan-gun integration |

## 2.4 Development and Documentation Tools

- Git + GitHub  
- PlantUML (architecture / class / sequence; version-controllable)  
- SSMS / sqlcmd  
- .NET CLI; Android Studio / Gradle (PDA)  
- Planning artefacts: sprint plans, hours CSV, speech scripts, report outlines  

## 2.5 Runtime and Configuration Notes

- CloudWarehouse demo defaults to HTTP on port **5001**; SQL Server typically **1433**.  
- Configuration: `appsettings.json` + `appsettings.example.json` (sanitised sample).  
- Uploads: extension whitelist + size limits.  
- Authentication: deferred for MVP (ADR)—not claimed as implemented in this chapter.  
- PDA: intranet API address must match site IP; firewall allow-lists are operational details.

## 2.6 Link to Quality / Security Tooling

- Test projects: `CloudWarehouse.Tests` / `IntegrationTests` / `TestCommon`  
- CI: `ci.yml`; SAST: `codeql.yml`  
- Details appear in later QA / DevSecOps chapters; this chapter only states that the stack includes these gates.

## 2.7 Evidence Checklist

- Tables: stack overview, selection comparisons  
- Figures: logical layers / solution structure (multi-project `.sln`)  
- Figures: PDA project structure or device photo (optional)  
- Appendix: `Program.cs` DI Strategy registration order; CI badge / screenshot links  

---

# Chapter 3 System Use Cases and Business Modules

Building on the technical context of Chapter 2, this chapter turns outward to human–system interaction: roles, module decomposition, use cases, MoSCoW prioritisation, and diagrams. Descriptions are limited to delivered, runnable capabilities and establish the requirements baseline for context mapping and detailed design (including dual-track billing).

## 3.0 Requirements and System Analysis

> **Supervisor feedback:** the final report must present *Analysis*, not design-only narrative. This section precedes use cases and explains how analysis conclusions feed design (rubric: *analysis → design*).

### 3.0.1 AS-IS Problem Domain

| Domain | AS-IS | Pain | Evidence |
| --- | --- | --- | --- |
| Warehouse settlement | Cost/quote/waybill data in disparate Excel files | Inconsistent headers (incl. three-row), manual reconciliation, weak historical traceability | Sprint 2 overrun **+39%** on Excel parsing |
| Freight trial | No unified preview-before-commit path | Cannot compare receivable vs payable before settlement | Sponsor demo feedback: need explainable preview |
| Shop-floor reporting | Night/ad-hoc work without MES orders | Paper/verbal records lost | Recent mesdb volume skewed to no-order path (§11.4) |

### 3.0.2 Stakeholders and Constraints

| Stakeholder | Goal | Constraint |
| --- | --- | --- |
| Warehouse admin / billing clerk | Repeatable import, trial, dual-track preview | Solo delivery; demo env without JWT |
| Line operator | Scan start/report on PDA | Honeywell device; intranet HTTP |
| Enterprise mentor | Demonstrable MVP; field usability | No mandatory microservices / full go-live this term |
| Academic supervisor | Design depth; verifiable evidence | Class-level sequence; test/security artefacts |

**Analysis-time architecture constraint:** solo intern, ~20 weeks; **Modular Monolith** plus two bounded contexts (CW vs PDA); integration **Planned**.

### 3.0.3 Analysis Conclusions → Design Inputs

| Analysis conclusion | Design response | Evidence |
| --- | --- | --- |
| Variable billing rules (tier / overweight / volumetric) | **Strategy Pattern** + `FeeCalculationEngine` | §7.3 |
| Receivable vs payable semantics; historical rates by ship date | **Dual-track** `DualTrackFeeCalculator`; class-level sequence | §7.5; `14-sequence-waybill-dual-track.puml` |
| Uncontrolled external Excel | Preview → transactional commit; template + three-row detection | §7.7; `08-sequence-import.puml` |
| Rule explanation separate from settlement | Lexical rule RAG (read-only FAQ) | §3.2 Assistant |
| No-order shop-floor data must persist | Independent PDA context + Spring Boot API | §3.6 |

## 3.1 Actors

The ecosystem comprises two independently operated domains: the web CloudWarehouse management platform and the PDA terminal system for shop-floor capture. There is **no production-grade API integration**; any exchange relies on manual operations or batch jobs.

**Within CloudWarehouse:**

- **Primary actor:** warehouse administrator / billing specialist—maintains master data, pricing rules, and bill processing via the web UI.  
- **Indirect actors:** suppliers / shop technicians—supply cost Excel files but do not log in; administrators upload their files.

**Within PDA:**

- **Primary actor:** line operator—records start/report events on Honeywell PDAs when no formal MES order exists.  
- **Indirect actors:** supervisors / MES data consumers—review logs; optional dual-write to legacy MES is a downstream store without real-time bidirectional sync.

System components (pricing engine, API endpoints) are **not** actors. The two systems remain API-decoupled for independent deployment and evolution.

## 3.2 CloudWarehouse Module Decomposition

| Module | Responsibility | Main APIs | UI |
| --- | --- | --- | --- |
| MasterData | CRUD and import for sites, destinations, customers | `/api/Site`, `/Destination`, `/Customer` | Dedicated tabs |
| Import | Cost Excel preview, parse, templates, export | `/api/Import/...` | Cost import page |
| Pricing | View rate rules, simulate freight, import customer quotes | `/api/PriceRule`, `/CustomerQuote` | Rate rules / customer quote pages |
| Billing | Waybill import preview/ingest; receivable (customer quotes) vs payable (cost); both tracks use ship-date historical rates | `/api/Bill/waybill...` | Waybill import page |
| Assistant | Built-in rule RAG: retrieve knowledge base and generate cited answers (read-only) | `/api/Assistant/ask` | Rule RAG UI |
| Pricing.Core | Strategy-based fee engine (library used by Pricing and Billing) | Internal library | — |

Assistant / rule RAG supports lookup only and does **not** affect settlement amounts. Billing uses `FeeCalculationEngine` (via Pricing.Core) as the calculation path.

## 3.3 CloudWarehouse Use-Case Catalogue

| ID | Use case | Actor | Description |
| --- | --- | --- | --- |
| UC-01 | Manage sites | Admin | Pre: system reachable (MVP auth deferred). Main: create/read/update/delete site records. |
| UC-02 | Import site list | Admin | Pre: valid Excel. Main: upload/parse site list with format validation. |
| UC-03 | Manage destinations | Admin | Pre: system reachable. Main: destination CRUD. |
| UC-04 | Manage customers | Admin | Pre: system reachable. Main: customer profile CRUD. |
| UC-05 | Download price template | Admin | Pre: none. Main: generate standard Excel template (single-row headers typical; three-row headers supported for supplier compatibility). |
| UC-06 | Preview cost import | Admin | Pre: Excel ready. Main: parse/validate headers and data types. |
| UC-07 | Commit cost rates to DB | Admin | Pre: preview succeeded. Main: transactionally insert/replace `PriceRules`. |
| UC-08 | Simulate freight fee | Admin | Pre: valid inputs. Main: estimate fee via strategy-driven engine using site, destination, weight, date, etc. |
| UC-09 | Preview/import customer quotes | Admin | Pre: quote file ready. Main: validate and ingest customer-specific pricing rules. |
| UC-10 | Preview dual-track billing | Admin | Pre: waybill uploaded. Main: side-by-side receivable/payable amounts using historical rates for comparison. |
| UC-11 | Submit billing results | Admin | Pre: review done. Main: optionally persist final billing output. |
| UC-12 | Obtain rate-rule explanation | Admin | Pre: rule selected. Main: query human-readable rule logic from knowledge store; does **not** change calculation. |

All listed use cases map to real features; no microservice-granularity use cases are invented.

## 3.4 MoSCoW Prioritisation (Final)

**Must (delivered):**

- UC-06 / UC-07: cost import with preview and DB persistence  
- UC-09: customer quote rule import  
- UC-10: dual-track waybill billing preview with historical rates  
- UC-08: freight simulation  
- Core master-data CRUD (sites, destinations, customers)  
- Multiple strategy-driven fee variants via Pricing.Core  

**Should (partial / to improve):**

- UC-12: rule explanation retrieval (basic search available)  
- Richer Excel import error feedback  
- Broader test coverage and CI evidence packaging  

**Could (not implemented):**

- JWT authentication and RBAC  
- Integration with PDA  
- Streaming for very large Excel files  

**Won’t (excluded this phase):**

- Full WMS fulfilment (receive, put-away, inventory counts)  
- Microservice deployment to production  
- AI-driven automatic pricing replacing the engine  

## 3.5 Use-Case Diagram


> **【插图占位 3-1】** 用例图 Use Case Diagram
> - 来源：`docs/diagrams/06-use-case-diagram.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 3-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：CloudWarehouse 与外部参与者关系；PDA 用例见正文表。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

Primary use cases and actors are shown in Figure 3-1 (`docs/diagrams/06-use-case-diagram.puml`). The administrator sits inside the system boundary; suppliers are external. The diagram emphasises Phase 1 core cases; extended cases UC-09–UC-12 are detailed in the text tables. An optional enterprise context diagram (`docs/diagrams/16-enterprise-context-map.puml`) clarifies cross-system roles.

## 3.6 PDA Use Cases and Modules

Independent PDA system for “no production order” shop-floor reporting.

**Modules:**

- Terminal UI: login, line / group / machine selection, start / report, query  
- API endpoints: `/api/login`, `/devices`, `/work/start`, `/work/report`, `/records`, etc.  
- Data entities: start/end work records, machine–line master data, standard cycle times (if present)  

| ID | Use case | Key points |
| --- | --- | --- |
| P-UC-01 | Login | Employee ID or QR scan |
| P-UC-02 | Select line / group / machine | Device QR scan supported |
| P-UC-03 | Start operation | Batch number and context required |
| P-UC-04 | Pause / resume | Machine switch under defined constraints |
| P-UC-05 | Report completion | Auto-associates to last started machine |
| P-UC-06 | Query records | Historical activity trace |
| P-UC-07 | Exception checks (if implemented) | Flag mismatched totes or abnormal cycle times |

MoSCoW: P-UC-01–P-UC-06 are **Must** (delivered). P-UC-07 is **Should** or delivered depending on implementation completeness.

## 3.7 Evidence Overview

- Tables: module map, use-case catalogue, MoSCoW  
- Diagrams: use-case diagram; optional context map  
- Screenshots: dual-track waybill preview; PDA successful report  


> **【插图占位 3-2】** 运单双轨预览 UI
> - 来源：`管理端运单预览截图`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 3-2】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：应收/应付机器值与表内值对比。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*


> **【插图占位 3-3】** PDA 报工成功
> - 来源：`PDA 设备或模拟器截图`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 3-3】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：无订单报工闭环证据。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

---

# Chapter 4 Project Roadmap and Iterative Execution

Chapter 3 prioritised delivered use cases with MoSCoW. This chapter explains how capabilities were landed under **solo** development in **one-week sprints**, with auditable **Planned vs Actual** personal hours. Sources include `docs/project-management/sprint-hours-chart-data.csv` and Phase 2 work records. No multi-person team capacity assumptions are used.

## 4.1 Iteration Method and Cadence

Short agile iterations: one Sprint ≈ one calendar week, organised into Phase 1 and Phase 2.

| Phase | Sprints | Focus |
| --- | --- | --- |
| Phase 1 | Sprint 1–4 | CloudWarehouse MVP: master data, Excel import, freight trial, CI |
| Phase 2 | Sprint 5 onward | Billing strategies, dual-track waybills, historical prices, rule retrieval; **in parallel** PDA no-order reporting |

Solo project-management norms:

1. **Plan:** select Must tasks within weekly capacity; break into demoable, testable increments.  
2. **Execute:** repository task lists and Git commits as progress evidence—no fictional multi-person Kanban.  
3. **Retrospect:** compare planned vs actual hours; buffer high-uncertainty work (Excel parsing, hardware bring-up).  
4. **Governance:** present personal Planned vs Actual hours as required by supervisors.

### 4.1.1 Sprint Tracking Tools and Solo Agile Practice

| Supervisor question | This project | Evidence |
| --- | --- | --- |
| Jira or similar? | **No Jira.** Lightweight trail: `sprint-hours-chart-data.csv`, `4-week-sprint-plan.md`, Git history, GitHub Actions | CSV, HTML chart, Actions |
| Proper Sprints solo? | **Yes** for Phase 1 (four weekly Sprints). Phase 2 (~June onward) shifted to milestone-driven delivery while logging hours | §4.3–4.7 |
| Burndown? | Story-point burndown **not used**; **cumulative Planned vs Actual hours** instead (§4.8.1) | `sprint-burndown-cumulative.csv` |

**June rhythm change:** after mid-term, iterations were no longer strictly calendar-week boxes, but Phase 2 work packages still record Planned/Actual.

## 4.2 Milestone Overview


> **【插图占位 4-1】** 项目路线图里程碑
> - 来源：`docs/diagrams/10-roadmap-milestones.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 4-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：若图中 M6 仍为 Planned，以正文 Done 为准。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

| ID | Name | Sprint | Status | Primary evidence |
| --- | --- | --- | --- | --- |
| M1 | Foundation | S1 | Done | `database/schema.sql`; site / destination CRUD APIs |
| M2 | Import Preview | S2 | Done | Standard and legacy three-row header parsing; import preview flow |
| M3 | Rules & Pricing | S3 | Done | Transactional `PriceRules` write; calculate API and UI |
| M4 | QA & CI | S4 | Done | Unit / integration / light load tests; GitHub Actions; coverage artefacts |
| M5 | Documentation & Videos | S4–final | In progress | This report; seven assessment videos |
| M6 | Billing Strategy + Dual-track | S5 | Done | Strategy class/sequence diagrams; dual-track logic; historical pricing |
| M6b | Built-in Rule RAG | S5 | Done | `/api/Assistant/ask`; rule RAG UI (pipeline visualisation) |
| M6c | PDA No-order reporting MVP | Phase 2 parallel | Done | Honeywell PDA start/report demos |
| M7 | Authentication (JWT/RBAC) | Planned | Planned | ADR; deferred |
| M8 | Microservice extraction (on triggers) | Planned | Planned | See architecture chapter trigger conditions |

If diagram M6 still shows Planned, treat this chapter’s **Done** status as authoritative.

### 4.2.1 Product Backlog (Epic Level, Phase 1)

| Epic | Representative stories | Priority | Sprint | Status |
| --- | --- | --- | --- | --- |
| Master data | US-1.1 sites; US-1.2 destinations | Must | S1 | Done |
| DB & scaffold | US-1.3 ERD; US-1.4 ASP.NET + static UI | Must | S1 | Done |
| Excel import | US-2.1–2.5 template, dual headers, preview | Must | S2 | Done |
| Rules & trial | US-3.1–3.5 transactional upsert, calculate API | Must | S3 | Done |
| Quality & CI | US-4.1–4.3 tests, Actions | Must | S4 | Done |
| Docs & demos | US-4.5–4.6 diagrams, videos | Should | S4–M5 | In progress |
| Phase 2 billing | Strategy, dual-track, historical price, rule RAG | Must | S5+ | Done |
| PDA no-order | P-UC-01–06 login/select/start/report | Must | Phase 2 parallel | Done |
| JWT/RBAC | — | Could | — | Planned |
| CW↔PDA integration | — | Won't (this phase) | — | Planned |

Detail: `docs/project-management/4-week-sprint-plan.md`. **Solo:** all stories owned by one developer.

## 4.3 Sprint 1 — Foundation (Planned 48h / Actual 52h, +8%)

**Goal:** runnable foundation—schema, master-data CRUD, admin tabs, Dapper access.

**Outputs:** SQL Server schema/init for sites, destinations, price rules; Sites/Destinations CRUD + UI; Dapper and connection configuration.

**Variance:** +4h (+8%), mainly one-off local SQL Server / .NET 9 environment setup—controllable infrastructure overhead.

## 4.4 Sprint 2 — Excel Import (Planned 44h / Actual 61h, +39%)

**Goal:** supplier price-table import with template download, parse, and preview to avoid unvalidated writes.

**Outputs:** standard single-row template; ClosedXML auto-detection of legacy three-row headers; preview–confirm flow with optional trial-calculation linkage.

**Overrun driver:** real supplier header chaos, column misalignment, and multi-format compatibility far exceeded early estimates. Actual **61h (+39%)**—largest Phase 1 variance.

**Improvements applied in Sprints 3–4:** finer decomposition of external-file tasks; ~15% buffer for high-uncertainty work; strict preview-before-commit to cut rework.

## 4.5 Sprint 3 — Rules & Pricing (Planned 56h / Actual 51h, −9%)

**Goal:** transactionally write validated price tables to `PriceRules`; stable freight trial API.

**Outputs:** fail-all rollback on validation errors; lane + effective-date versioning; one Excel row → many `PriceRules` (tiers + overweight); `/api/PriceRule/calculate` and UI.

**Note:** no JSON dynamic rule engine—DB rule rows + calculation service. Underspend reflects better estimation after Sprint 2 and import-pipeline reuse.

## 4.6 Sprint 4 — QA & CI (Planned 50h / Actual 47h, −6%)

**Goal:** engineering quality gates with reproducible evidence.

**Outputs:** xUnit + `WebApplicationFactory` integration tests; light concurrency smoke tests; GitHub Actions: restore → `dotnet test` → reports → coverage artefacts; explainable skip of DB-dependent tests when cloud runners lack SQL Server.

Final report and seven videos belong to M5 and are deferred to avoid Sprint 4 scope creep. Actual **47h**, slightly under plan.

### 4.6.1 Sprint Backlog Extract (Phase 1)

See Chinese master §4.6.1 for full US tables (S1–S4 planned/actual hours). Source: `4-week-sprint-plan.md`.

## 4.7 Phase 2 (from Sprint 5) — Design Deepening and Parallel PDA

Mid-term feedback: early system felt light; billing needed patterns and detailed design; architecture narrative and physical evidence were insufficient. Phase 2 did **not** jump to microservices; it deepened CloudWarehouse capabilities and delivered PDA in parallel.

### 4.7.1 CloudWarehouse deepening (completed)

1. Strategy Pattern: `TierBillingStrategy`, `OverweightBillingStrategy`, `VolumetricBillingStrategy` orchestrated by `FeeCalculationEngine`.  
2. Dual-track waybills: receivable customer quotes vs payable cost, historical rules by ship date; preview vs in-sheet transfer-fee comparison.  
3. Built-in rule RAG (Retrieve→Augment→Generate) for FAQ lookup—**not** a settlement engine.  
4. Class diagrams, dual-track sequence diagrams, and tests (see Software Design / QA).

### 4.7.2 PDA no-order reporting (parallel, completed)

Honeywell PDA app for night/no-order scenarios: login, select line/group/machine, start/report/query; Spring Boot API persistence. **No production API integration** with CloudWarehouse—two independent contexts under one factory narrative.

### 4.7.3 Phase 2 personal hours

| Work package | Planned (h) | Actual (h) | Notes |
| --- | ---: | ---: | --- |
| CW: Strategy + dual-track + rule RAG + diagrams/report + Playwright E2E | 58 | 63 | Git Jun–Aug 2026 |
| PDA: API + Android + hardware + ops guide | 72 | 76 | Separate from CW hours |
| **Phase 2 total** | **130** | **139** | |
| **Grand total (Ph1+Ph2)** | **328** | **350** | Solo personal effort |

## 4.8 Phase 1 Planned vs Actual Summary

| Sprint | Goal summary | Planned (h) | Actual (h) | Variance |
| --- | --- | --- | --- | --- |
| S1 | Foundation | 48 | 52 | +8% |
| S2 | Excel import | 44 | 61 | +39% |
| S3 | Rules & trial calculation | 56 | 51 | −9% |
| S4 | QA & CI | 50 | 47 | −6% |
| **Phase 1 total** | | **198** | **211** | **+7%** |


> **【插图占位 4-2】** Phase 1 个人工时 Planned vs Actual
> - 来源：`docs/project-management/sprint-hours-chart.html`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 4-2】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：Solo 个人工时柱状图；数据见 sprint-hours-chart-data.csv。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

**Analysis:** Phase 1 overall +7%—plan controlled. Only Sprint 2 had a large overrun (external Excel unpredictability); Sprints 3–4 returned to ±10%, showing retrospective improvements worked. All hours are **solo** personal effort, meeting mid-term Planned vs Actual requirements.

### 4.8.1 Burndown Tracking — Solo Alternative

Classic burndown tracks **remaining story points**. This project uses **cumulative Planned vs Actual hours** (`sprint-burndown-cumulative.csv`):

| Milestone | Cumulative planned (h) | Cumulative actual (h) |
| --- | ---: | ---: |
| End Sprint 1 | 48 | 52 |
| End Sprint 2 | 92 | 113 |
| End Sprint 3 | 148 | 164 |
| End Phase 1 (S4) | 198 | 211 |
| End Phase 2 (project) | 328 | 350 |

**Figure 4-3 (suggested):** line chart of planned vs actual cumulative hours (Appendix A-07b).

## 4.9 Evidence Checklist

| Evidence | Location |
| --- | --- |
| Hours source data | `docs/project-management/sprint-hours-chart-data.csv` |
| Hours bar chart | `sprint-hours-chart.html` screenshot |
| Roadmap | `docs/diagrams/10-roadmap-milestones.puml` |
| Sprint 2 import | Import success/fail screenshots; `ExcelHelper` unit tests |
| Sprint 4 CI | GitHub Actions green runs; coverage artefacts |
| Sprint 5 Strategy & dual-track | Class diagram 13, sequence 14, waybill preview screenshots |
| PDA reporting | Start/report success screenshots or short recording |

## 4.10 Chapter Summary

Phase 1 delivered the CloudWarehouse MVP in four weekly sprints with controllable solo-hour variance. Phase 2 deepened design via Strategy and dual-track historical pricing and delivered PDA no-order reporting in parallel. Subsequent chapters cover persistence, multi-view architecture, and billing detailed design, always anchored by verifiable engineering evidence.

---

# Chapter 5 Database Design and Entity-Relationship Model

Chapter 4 described sprint delivery. This chapter focuses on the persistence model that enables dual-track price rules, one-to-many rule expansion, whole-lane version replacement, historical-price lookup, and receivable/payable settlement.

## 5.1 Design Goals

1. **Business coverage:** master data; dual-track versioned price rules (receivable vs payable); repeatable/idempotent import; traceable waybill settlement lines.  
2. **Technical fit:** relational model with explicit SQL via Dapper for performance and control.  
3. **Architectural consistency:** all CloudWarehouse business tables in one database (Modular Monolith); PDA uses a separate database for physical context isolation.

## 5.2 Conceptual Model and Bounded Table Groups

| Group | Tables (conceptual / script names) | Description |
|------|--------------------------------------|-------------|
| Master Data | Sites, Destinations, Customers, CustomerAccounts | Baseline entities with unique codes for referential integrity |
| Pricing (payable cost) | PriceRules | Lane (site–destination) + effective-date versioning; `BillingType` distinguishes tier vs overweight |
| Pricing (receivable quotes) | CustomerQuoteRules | **Separate from** PriceRules: customer + province (+ optional express type) + effective date |
| Billing | BillLines | **No separate Bill header table**; line-level amounts, batch, tiers, dual-track comparison fields |
| Import metadata | No dedicated job table | File-level import; state follows preview–confirm; repeatability via business replace strategy |

## 5.3 Key Design Decisions

1. **One-to-many rule mapping:** one Excel price row expands to many rule rows (weight bands such as 0–0.3 kg, 0.3–0.5 kg, plus overweight). Applies to both `PriceRules` and `CustomerQuoteRules`.

2. **Historical price versioning:** both rule tables use `EffectiveDate` and nullable `ExpiryDate`. Settlement filters by waybill **ship / bill date** so historical amounts are reproducible.

3. **Idempotent import (whole-lane replace):** cost import deletes **all** rules for a lane (`SiteId + DestId`) then inserts the new set—not a fine-grained upsert on lane + same EffectiveDate. Repeated file submission does not leave stale band rows.

4. **Billing type discriminator:** rule-table `BillingType` INT (1 = tier, 2 = overweight) routes the Strategy engine. Note: `BillLines.BillingType` may be a business string (e.g. “forward billing”) and is **not** the same semantic as the rule INT enum.

5. **Index correction:** `database/fix-price-rules-index.sql` **drops** the incorrect unique index `(SiteId, DestId, EffectiveDate)` because one lane and one effective date **must** allow multiple tier/overweight rows. Queries rely on non-unique lane indexes.

6. **Settlement line persistence:** `BillLines` stores computed amounts, batch, tier outcomes, etc.; matched rule-row IDs are generally **not** stored, avoiding brittle traceability when rule versions change.

## 5.4 Entity-Relationship Diagram (ERD)


> **【插图占位 5-1】** 实体关系图 ERD
> - 来源：`docs/diagrams/07-erd.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 5-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：以 schema.sql 为准；图若滞后于 BillLines/CustomerQuoteRules 请在 caption 说明。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

Source: `docs/diagrams/07-erd.puml`. Core associations:

- Site / Destination ↔ PriceRules / CustomerQuoteRules: one-to-many across effective periods and weight bands.  
- Customer ↔ CustomerQuoteRules: one-to-many for differentiated quotes.  
- BillLines link site, destination, and customer dimensions at line granularity; **no Bill header table**.

If the ERD figure lags (missing dual rule tables, `ExpiryDate`, or BillLines-only design), treat `database/schema.sql`, `billing-schema.sql`, and `customer-quote-schema.sql` as authoritative and note planned diagram sync.

## 5.5 Integrity and Transactions

- **Constraints:** FKs, non-null, unique codes (e.g. `SiteCode`); after removing the bad unique index, version uniqueness at lane+date is enforced by business logic rather than a wrong unique key.  
- **Import transaction:** whole-lane delete + bulk insert inside one `SqlTransaction`; any validation failure rolls back the entire batch.  
- **Workflow alignment:** preview validates in memory only; confirm opens the transaction—minimising dirty partial writes.

## 5.6 PDA Storage (Independent Deployment)

PDA uses dedicated database `PDA_NoOrder`, physically isolated from CloudWarehouse.

- Conceptual tables: users, machines/lines, start records, report records.  
- No shared-DB integration or real-time sync with CloudWarehouse in this delivery.

## 5.7 Evidence Checklist

| Evidence | Location |
|------|----------|
| ERD | `docs/diagrams/07-erd.puml` |
| Core schema | `database/schema.sql` |
| Billing schema | `database/billing-schema.sql` |
| Customer quote schema | `database/customer-quote-schema.sql` |
| Index fix | `database/fix-price-rules-index.sql` |
| Optional | SSMS table-list screenshot |

## 5.8 Chapter Summary

Dual-track rule tables, one-to-many expansion, and whole-lane replace underpin historical pricing and dual-track settlement; PDA’s separate database preserves context decoupling. The next chapter places these tables into multi-view system architecture.

---

# Chapter 6 System Architecture and Multi-View Design

Chapter 5 defined the persistence model. This chapter organises those capabilities into a deployable system via multi-view architecture: logical layers and bounded contexts, enterprise relationship to PDA, physical topology, and honest HA / future-split boundaries. Mid-term feedback required defending the monolith, documenting infrastructure on physical views, and explaining DDD—this chapter responds directly.

## 6.1 Architecture Style and Motivation

CloudWarehouse adopts a **Modular Monolith**:

- **Physically:** one deployable unit (`CloudWarehouse.Backend`, ASP.NET Core + same SQL Server database).  
- **Logically:** module folders by bounded context (`Modules/MasterData`, `Import`, `Pricing`, `Billing`, `Assistant`, …) with clear seams for conditional extraction later.

| Option | Pros | Poor fit for this project |
|------|------|------------------|
| Microservices | Independent deploy, parallel teams | Solo + four-week MVP; distributed TX / ops cost too high |
| Big-ball-of-mud monolith | Fast delivery | Fuzzy boundaries; hard to evolve or defend as “designed” |
| **Modular Monolith** | Fast single-process delivery + module boundaries | Optimal for this phase; extraction kept as conditional plan |

The monolith is an intentional decision under time, headcount, and consistency constraints (ADR / `docs/diagrams/01a-architecture-decisions-adr.puml`), not a lack of microservice skill.

PDA no-order reporting is a **separate deployable system** (Android + Spring Boot + independent DB)—not stuffed into the CloudWarehouse process and not pretended to be one service mesh.

## 6.2 Logical Architecture and Typical Request Flow


> **【插图占位 6-1】** 逻辑架构图
> - 来源：`docs/diagrams/02-logical-architecture.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 6-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：分层与模块依赖。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

| Layer | Representative components | Responsibility |
|----|----------|------|
| Presentation | `wwwroot/index.html` | Admin tabs: master data, import, trial calc, waybills, rule retrieval |
| API | Module `*Controller`s | HTTP adaptation; validate; delegate to application services |
| Application | Import / Calculate / BillImport / Assistant services | Use-case orchestration, TX boundaries, helper coordination |
| Domain / Helpers | Excel parse, `PriceRuleMapper`, `FeeCalculationEngine` + Strategy | Pure rules/calculation; unit-testable |
| Data | Dapper + SQL Server | Explicit SQL; same-DB transactions |

**Cost import (preview → confirm) data flow (summary):**

1. Browser uploads `.xlsx` → `ImportController`.  
2. `PriceRuleImportService` uses `ExcelHelper` to detect standard / three-row headers.  
3. `PriceRuleMapper` expands one Excel row into many `PriceRule`s.  
4. Preview: `save=false`, optional trial calc, **no DB write**.  
5. Confirm: within `SqlTransaction`, delete lane old rules and insert new; commit or full rollback.

Dual-track and Strategy sequencing details belong in Software Design (class diagram 13, sequence 14). This chapter only fixes logical placement: settlement orchestration in Pricing/Billing application layer; persistence as in Chapter 5.

## 6.3 Bounded Contexts and Code Mapping


> **【插图占位 6-2】** DDD 限界上下文
> - 来源：`docs/diagrams/05-ddd-bounded-contexts.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 6-2】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：Master Data / Import / Pricing 等边界。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

DDD here is **pragmatic context partitioning**, not full event sourcing or aggregate “magic”.

| Bounded context | Responsibility | Code locus | Key persistence |
|------------|------|------------------|----------|
| Master Data | Sites / destinations / customers | `Modules/MasterData` | Sites, Destinations, Customers, CustomerAccounts |
| Import | Cost table parse, validate, transactional commit | `Modules/Import` | Writes **PriceRules** (no job table; customer-quote import lives under Pricing) |
| Pricing | Cost trial calc, customer quotes, Strategy engine | `Modules/Pricing` + Pricing.Core | PriceRules, CustomerQuoteRules |
| Billing | Waybill import, receivable/payable compare persist | `Modules/Billing` | BillLines |
| Assistant | Built-in rule RAG (FAQ assist; not settlement SoR) | `Modules/Assistant` + KnowledgeBase | File knowledge base primarily |

**Language isolation example:** Import’s `PriceTableRow` (Excel view) maps to Pricing’s persistent `PriceRule` collection—do not mix field semantics at the UI layer.

Early Phase 1 controllers lived at the solution root; narrative uses the **current `Modules/*` structure**.

## 6.4 Enterprise Context Relationships (Including PDA)


> **【插图占位 6-3】** 企业 Context Map
> - 来源：`docs/diagrams/16-enterprise-context-map.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 6-3】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：云仓与 PDA 独立；集成为 Planned。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

| Relationship | Honest meaning |
|------|------------------|
| CloudWarehouse internal modules | Same-DB Modular Monolith; in-process calls |
| PDA ↔ line / MES-related capabilities | PDA start/report and backend persist delivered; MES linkage described only as actually implemented—**no exaggeration** |
| CloudWarehouse ↔ PDA | **Customer–Supplier / Integration Planned:** shared factory goals; **no production API co-DB or real-time sync this phase** |

Defence ban words: do not claim “microservices are live” or “CloudWarehouse and PDA settlement APIs are connected”.

## 6.5 Physical Deployment and Runtime Topology


> **【插图占位 6-4】** 物理 / 部署视图
> - 来源：`docs/diagrams/03-physical-architecture.puml 或 04-deployment-diagram.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 6-4】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：单实例拓扑；无 HA。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

| Node | Role | Notes |
|------|------|------|
| Developer / demo machine | Backend (Kestrel) + browser | Admin UI and API co-hosted or same publish package |
| SQL Server | CloudWarehouse DB | Local or LAN; typically port 1433 |
| GitHub Actions Runner | Ephemeral CI | `dotnet test`, coverage artefacts; DB-dependent tests may skip with explanation when no permanent business DB |
| PDA device + PDA API/DB | Parallel system | Honeywell ↔ Spring Boot ↔ `PDA_NoOrder` (independent) |

Optional: self-contained `publish/` package for on-site demos. IIS checklists ≠ production multi-active cluster.

**Security posture (MVP honesty):** local / controlled demo; auth/RBAC deferred per ADR; CORS/HTTP convenience settings must be tightened before production—details in DevSecOps / Risk chapters.

## 6.6 High Availability and Backup (Honest Statement)

| Aspect | Current state | Direction (not required this phase) |
|------|----------|--------------------------|
| App redundancy | Single instance, no load balancer | Container replicas + reverse proxy |
| DB redundancy | Single SQL Server | Managed HA / Always On, etc. |
| Backup | Manual `.bak` / script rebuild | Automated backup with explicit RPO |
| Disaster recovery | Git + `database/*.sql` rebuild | Documented RTO + drills |

Correct response to “document infrastructure/redundancy”: **state that there is no HA**, do not invent a cluster.

## 6.7 Microservice Extraction Triggers (Planned)

Module boundaries exist, but extraction requires triggers, e.g.:

- Independent teams or release cadences become necessary;  
- A context (e.g. fee calculation) shows distinct scale/performance needs;  
- Organisation can afford ops and observability cost.

Under solo staffing and current volume, early split adds network and distributed-consistency cost without proportional benefit. Milestone M8 remains **Planned**, aligned with Chapter 4.

## 6.8 Evidence Checklist

| Evidence | Location |
|------|------|
| Constraints / ADR | `docs/diagrams/01*.puml` |
| Logical architecture | `02-logical-architecture.puml` |
| Physical / deployment | `03-physical-architecture.puml`, `04-deployment-diagram.puml` |
| DDD bounded contexts | `05-ddd-bounded-contexts.puml` |
| Enterprise context map | `16-enterprise-context-map.puml` |
| Module code | `CloudWarehouse.Backend/Modules/*` |
| Publish / demo package (optional) | `publish/`, start scripts |

## 6.9 Chapter Summary

Modular Monolith is justified under constraints; logical layers, bounded contexts, and the enterprise context map address narrative gaps; physical topology and an honest **no-HA** inventory address infrastructure transparency. PDA remains a parallel context with integration **Planned**. The next chapter refines Strategy billing, dual-track sequencing, and key class structures into verifiable design artefacts.

---

# Chapter 7 Software Design: Strategy Pattern and Dual-Track Billing

Chapter 6 fixed Modular Monolith structure and context placement. This chapter presents verifiable detailed design: Strategy Pattern for billing algorithm variants; class-level sequence for dual-track receivable/payable collaboration on waybill preview; and the placement of historical-price filtering, weight rounding, and amount-comparison checks. It directly answers mid-term demands for design patterns plus class/sequence artefacts.

## 7.1 Design Scope and Core Use Case

**Primary use case:** waybill Excel import preview + dual-track fee comparison—after the administrator uploads bill lines, the system computes receivable and payable per row and compares them to in-sheet expected amounts.

| In scope here | Out of main path |
| --- | --- |
| Strategy class structure and resolver | Built-in rule RAG / Assistant (lookup only; no settlement amounts) |
| `FeeCalculationEngine` orchestration | PDA reporting workflows |
| Dual-track application services and sequencing | Microservice extraction design |
| Weight rounding; date-filtered historical prices | Authentication / RBAC |

Participants align with Chapter 6: Browser → `BillController` → `BillImportService` → dual-track calculator → cost/quote calculate services → fee engine → concrete strategies → SQL Server.

## 7.2 Billing Variants and Strategy Motivation

| Variant | Business meaning | Status | Strategy class |
| --- | --- | --- | --- |
| Tier | Weight ≤5 kg: discrete weight bands; unit price + waybill fee | Done | `TierBillingStrategy` |
| Overweight | Weight >5 kg: overweight unit pricing | Done | `OverweightBillingStrategy` |
| Volumetric | When L×W×H/6000 exceeds actual weight, bill by volumetric weight via tier/overweight | Done (engine + unit tests) | `VolumetricBillingStrategy` |
| Step / irregular / surcharges | Contract custom extensions | Planned | Reserved; not implemented |

Phase 1 could cover the first two variants with conditionals; continual contract algorithms would force core-path edits and hurt maintainability. Phase 2 abstracts algorithms as replaceable strategies; the engine only runs “filter effective rules → resolve strategy → calculate”. Recorded as **ADR-8 (Billing Strategy Pattern, Implemented)**.

## 7.3 Strategy Class Design


> **【插图占位 7-1】** 计费 Strategy 类图
> - 来源：`docs/diagrams/13-billing-strategy-class.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 7-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：Tier / Overweight / Volumetric + FeeCalculationEngine。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

Diagram: `docs/diagrams/13-billing-strategy-class.puml`. Core types live under `CloudWarehouse.Pricing.Core` Billing namespace, registered via Backend DI.

### 7.3.1 Key type responsibilities

| Type | Responsibility |
| --- | --- |
| `BillingContext` | Chargeable weight, effective rules, optional dimensions / volumetric divisor |
| `IBillingStrategy` | `CanHandle(context)` + `Calculate(context)` → `PriceCalculateResult` |
| `TierBillingStrategy` | Actual/chargeable weight in ≤5 kg band pricing |
| `OverweightBillingStrategy` | Weight >5 kg overweight pricing |
| `VolumetricBillingStrategy` | When dimensions exist and volumetric > actual, take over and delegate final weight to tier/overweight |
| `IBillingStrategyResolver` / `DefaultBillingStrategyResolver` | Walk registration order; first `CanHandle == true` wins |
| `FeeCalculationEngine` | Filter rules by order/bill date Effective/Expiry; build context; invoke resolver |
| `FeeRuleCalculator` | Static façade delegating to default engine (legacy callers) |
| `DualTrackFeeCalculator` | Application service: payable and receivable for the same waybill line; aggregate comparison |

### 7.3.2 Resolver order

`CreateDefault()` / DI registration order:

1. `VolumetricBillingStrategy` (prefer when dimensions imply larger volumetric weight)  
2. `TierBillingStrategy`  
3. `OverweightBillingStrategy`  

Order is part of the design: volumetric must precede actual-weight strategies or it would be short-circuited.

### 7.3.3 Link to the data model

Strategies consume filtered `PriceRule`-shaped lists: payable track from `PriceRules`; receivable track from `CustomerQuoteRules` mapped into the same structure. `BillingType` and weight bounds drive band matching; version selection happens in the engine filter stage—keeping strategies pure.

## 7.4 Open–Closed Principle and Extension Steps

Volumetric delivery validates OCP: add a strategy class + register it; weight-only callers remain compatible via `FeeRuleCalculator`.

Standard extension steps:

1. Implement `IBillingStrategy` with precise `CanHandle` conditions.  
2. Register in `DefaultBillingStrategyResolver.CreateDefault()` and `Program.cs` DI with controlled order.  
3. If new inputs are needed (dimensions, irregular flags), extend import columns or API context—without rewriting dual-track orchestration.

Planned Step / irregular strategies can reuse this path without rewriting `BillImportService`.

## 7.5 Dual-Track Waybill Sequence (Detailed Design)


> **【插图占位 7-2】** 运单双轨预览时序图
> - 来源：`docs/diagrams/14-sequence-waybill-dual-track.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 7-2】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：应收 CustomerQuoteRules vs 应付 PriceRules。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

**Dual-track semantics: receivable = customer-facing quotes; payable = supplier cost. This is NOT domestic vs international lanes.**

### 7.5.1 Preview happy path

1. Administrator selects waybill Excel and clicks Preview.  
2. `POST /api/Bill/waybill/preview` → `BillController` → `BillImportService.ProcessImportAsync(..., saveToDatabase=false)`.  
3. `WaybillExcelHelper` parses dual-row headers (bill detail + cost detail) or standard template; extracts in-sheet expected transfer fees if present.  
4. Preload caches for sites, destinations, customers, accounts, and rules.  
5. Per row: validate waybill no., province, weight → `WeightRounding` → resolve customer, site (often from express type → `SiteCode`), destination.  
6. `DualTrackFeeCalculator.CalculateAsync(row)`:  
   - **Payable:** `PriceRuleCalculateService` queries `PriceRules` by SiteId/DestId + bill date → `FeeCalculationEngine` → resolver → Tier/Overweight/Volumetric.  
   - **Receivable:** `CustomerQuoteCalculateService` queries `CustomerQuoteRules` by CustomerId / province + bill date → **same** engine and strategies.  
7. `BillLineTotals` aggregates receivable, payable, margin; tolerance compare vs Excel expected values (default 0.01); mark match/mismatch.  
8. Return preview set for UI (system values, sheet values, match statistics).

Confirm persist uses the same orchestration with `saveToDatabase=true` writing **`BillLines`**; TX boundary remains in the import service.

### 7.5.2 Design points

- Dual-track is an **application coordination** pattern, not two duplicated if/else kernels; both tracks share one Strategy engine for algorithmic consistency with separated financial semantics.  
- Historical pricing is centralised in the engine’s date filter—avoiding “always use latest price” reconciliation failures.  
- Comparison layer (`BillLineTotals`) turns design goals into demoable evidence: system vs manual sheet line-by-line.

## 7.6 Placement of Historical Price and Weight Rounding

| Concern | Design location | Notes |
| --- | --- | --- |
| Historical filter | `FeeCalculationEngine.Calculate` rule filter | `EffectiveDate <= bill date` and not past `ExpiryDate`; import may expand multi-version Excel columns via `MasterPriceHistoryHelper` |
| Weight rounding | `WeightRounding` before dual-track calc | Same forward-billing rounding on trial and batch preview paths |
| One-to-many rule rows | Chapter 5 model + Mapper | One Excel row → many rule rows for Tier band matching |

## 7.7 Secondary Use Case: Price-Table Import (Brief)

Import sequence: `docs/diagrams/08-sequence-import.puml` (layer flow in Chapter 6). From a design view: Import produces many `PriceRules` that feed the Strategy engine; Import implements **no** billing algorithms—only parse, map, and transactional replace. Customer-quote import follows the same pattern into `CustomerQuoteRules` for the receivable track.

## 7.8 Design Boundaries and Follow-ons

| Item | True current state |
| --- | --- |
| Volumetric billing | Engine API + unit tests covered; waybill Excel main path still primarily actual weight—dimension columns not yet ubiquitous in business files |
| Built-in rule RAG | Does not read/write settlement amounts; **FeeCalculationEngine** is the sole system of record for fees |
| Step / irregular / surcharge strategies | Planned; extension path in §7.4 |
| Settlement integration with PDA | Out of this chapter and out of this phase |

## 7.9 Evidence Checklist

| Evidence | Location |
| --- | --- |
| Strategy class diagram | `docs/diagrams/13-billing-strategy-class.puml` |
| Dual-track sequence | `docs/diagrams/14-sequence-waybill-dual-track.puml` |
| Import sequence (optional) | `docs/diagrams/08-sequence-import.puml` |
| Engine / strategy code | `CloudWarehouse.Pricing.Core` Billing types; `Modules/Billing/Services/DualTrackFeeCalculator.cs` |
| Unit tests | `CloudWarehouse.Tests/BillingStrategyTests.cs`, etc. |
| ADR | ADR-8 / mid-term writing guide §§20.2–20.3 |

## 7.10 Chapter Summary

Strategy Pattern addresses billing extensibility; dual-track sequencing addresses settlement collaboration: receivable vs payable separation, shared engine, date-based historical rates, and sheet comparison as demoable evidence. OCP is validated by incremental volumetric registration. The next chapter shows how these designs are constrained by automated tests and CI, not diagrams alone.

---

# Chapter 8 DevSecOps and Quality Assurance

Chapter 7 presented Strategy and dual-track detailed design. This chapter explains how those designs are constrained by automated quality gates and security scanning. Principle: claim only what evidence supports; mark gaps and plans honestly—no claim of a full DevSecOps platform or production-grade continuous delivery.

## 8.1 Scope and Project-Scale DevSecOps

| Layer | Meaning | Repository status |
| --- | --- | --- |
| Continuous Integration (CI) | Push/PR auto build and test | Done (GitHub Actions) |
| Quality gates | Unit, integration, light concurrency / perf smoke | Done |
| Security scanning | CodeQL SAST + NuGet vulnerability listing | Done (dependency scan `continue-on-error`—visibility first, not hard block) |
| Security baseline | Upload limits, sanitised config samples, demo assumptions | Partial Done; authentication Planned |

Evidence primarily from CloudWarehouse `.github/workflows/`. PDA is a separate system: API access, independent DB, intranet demo assumptions—not a unified security mesh with CloudWarehouse.

Pipeline activity diagram: `docs/diagrams/09-cicd-pipeline.puml`.


> **【插图占位 8-1】** CI/CD 活动图
> - 来源：`docs/diagrams/09-cicd-pipeline.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 8-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：CI 为主；完整 CD 未宣称。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*


> **【插图占位 8-2】** GitHub Actions 成功运行
> - 来源：`Actions 网页截图`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 8-2】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：绿勾证据。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*


> **【插图占位 8-3】** 覆盖率 Summary
> - 来源：`CI Artifact coverage-report`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 8-3】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：勿在正文写死百分比口号。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

## 8.2 Continuous Integration Pipeline

Workflow: `.github/workflows/ci.yml`.

- **Triggers:** push / pull_request on `main` / `master`.  
- **Runner:** `ubuntu-latest` + .NET SDK 9.0.x (cross-platform check vs local Windows).  

**Steps:**

1. `actions/checkout`  
2. `setup-dotnet`  
3. `dotnet restore CloudWarehouse.sln`  
4. Release `dotnet test` with Coverlet / `coverlet.runsettings` → `./coverage`  
5. ReportGenerator → HTML + text summary under `coveragereport/`  
6. Print `Summary.txt` to logs  
7. `dotnet list ... package --vulnerable --include-transitive` → `vulnerable-packages.txt` (failure does not fail the job; artefacts still upload)  
8. Upload artefacts: `coverage-report`, `coverage-cobertura`, `nuget-vulnerable-scan`  

This mitigates “passes only on my machine”. The pipeline is **CI + quality/security artefacts**, not full production CD. Release remains self-contained packages + manual checklist (architecture chapter / publish docs).

## 8.3 Test Pyramid and Verification Types

| Level | Projects / means | Focus |
| --- | --- | --- |
| Unit | `CloudWarehouse.Tests` | Strategies (tier / overweight / volumetric), Excel parse/map, historical helpers, weight rounding, rule retrieval, parse perf smoke |
| Integration | `CloudWarehouse.IntegrationTests` + `WebApplicationFactory` | HTTP end-to-end for master data, import, customer quotes, waybill billing |
| Light stress / concurrency | e.g. `StressLoadTests` | Concurrent smoke on template download / import preview—demo-level, not production SLA certification |

**Honest environment adaptation:** GitHub Actions runners usually lack a permanent SQL Server. DB-dependent integration tests use explainable skip (`DatabaseAvailability`, etc.) when DB is unreachable—avoiding environment-induced false reds. Local machines with SQL Server run the full path. Goal: regressibility of fee-engine refactors and dual-track logic—not inflated coverage slogans.

**Evidence (reproducible):** Repository `https://github.com/chenyuxiangAK47/cloudwarehouse-csharp` → **Actions** → workflow **CI** → latest green run → **Test with coverage** log. Local reproduction: `dotnet test CloudWarehouse.sln` (log backup: `docs/project-management/artifacts/dotnet-test-full.txt`).

### 8.3.1 Unit and Integration Test Results (local run, 25 Aug 2026)

This project is a **Modular Monolith**, not a microservice mesh. Results are grouped by **logical module**, not by fictional “microservices”.

**Summary (`dotnet test CloudWarehouse.sln`, Windows, Release):**

| Test project | Passed | Failed | Skipped | Duration (approx.) |
| --- | ---: | ---: | ---: | --- |
| `CloudWarehouse.Tests` (unit) | **83** | 0 | 0 | 6.6 s |
| `CloudWarehouse.IntegrationTests` (API) | **27** | 0 | 0 | 8.4 s |
| `CloudWarehouse.E2ETests` (Playwright UI) | **4** | 0 | 0 | ~2 s |
| **Total** | **114** | **0** | **0** | ~17 s |

**Figure 8-4 (screenshot):** GitHub Actions → CI → green run → **Test with coverage** tail showing all tests passed; or local terminal output (Appendix **A-12**).

**Unit tests by module (`CloudWarehouse.Tests`):**

| Module | Representative test classes | Focus | Result |
| --- | --- | --- | --- |
| Pricing / Strategy | `BillingStrategyTests`, `PriceCalculatorTests`, `FeeCalculationPerfSmokeTests` | Tier / overweight / volumetric; `FeeCalculationEngine`; resolver injection | All passed |
| Import / Excel | `ExcelHelperTests`, `WaybillExcelHelperTests`, `SiteExcelHelperTests`, `DestinationExcelHelperTests`, `CustomerExcelHelperTests`, `CustomerQuoteExcelHelperTests` | Price/waybill/master-data Excel parse and templates | All passed |
| Bill / dual-track | `BillLineTotalsTests`, `Waybill93FileTests`, `BillImportServiceRegionTests` | Receivable/payable totals, sheet comparison tolerance, region normalisation | All passed |
| Historical price / mapping | `MasterPriceHistoryHelperTests`, `PriceRuleMapperTests`, `MasterCostExcelTests` | Multi-version sheets, one-to-many rules, sample workbook 93 | All passed |
| Assistant (lexical FAQ) | `QuoteAssistantTests`, `QuoteAssistantEvalTests` | Retrieval + citations; **does not replace billing engine** | All passed |

**Integration tests (`WebApplicationFactory`):**

| API area | Test class | Focus | Result |
| --- | --- | --- | --- |
| Import | `ImportApiTests` | Price-table preview/import, bad extension, batch preview | All passed |
| Bill | `BillApiTests` | Waybill preview, dual-track fees when DB available, export | All passed |
| Customer quote | `CustomerQuoteApiTests` | Quote preview/import | All passed |
| Master data / static | `SiteAndStaticApiTests` | Site/Destination/Customer APIs; `index.html` served | All passed |
| Light concurrency | `StressLoadTests` | See §8.7 | All passed |

### 8.3.2 End-to-End Testing and Playwright

| Approach | Status | Notes |
| --- | --- | --- |
| **Playwright UI automation** | **Implemented** | Project `CloudWarehouse.E2ETests`: Kestrel on dynamic port + headless Chromium; `UiSmokeE2ETests` — **4** smoke cases (home nav, waybill import, customer quote import, Rule RAG panel). Reproduce: `dotnet test CloudWarehouse.E2ETests --filter Category=E2E`; log backup `docs/project-management/artifacts/e2e-playwright-test.txt`. |
| **API-level E2E** | **Implemented** | `WebApplicationFactory` exercises full HTTP paths for import, waybill dual-track, master data (27 integration tests in §8.3.1). |
| **Manual E2E (demo)** | **Implemented** | App Demo video: waybill Preview, price import, PDA start/report; Appendix **A-04–A-06, A-08** screenshots. |
| **Planned** | Later | Extend Playwright: file upload + Preview assertions, cross-browser matrix, visual regression. |

**Stack:** Microsoft.Playwright 1.50 + xUnit; CI runs `playwright.sh install --with-deps chromium` on `ubuntu-latest` before `dotnet test CloudWarehouse.sln` (`.github/workflows/ci.yml`).

**Figure 8-4b (screenshot):** Local or Actions log showing four `CloudWarehouse.E2ETests` passes.

## 8.4 Coverage Evidence (Citation Norm)

Coverage is collected by Coverlet, rendered by ReportGenerator, and archived as a CI artefact each build. Appendix should include: Actions green-run screenshot; coverage Summary page screenshot.

**Do not hard-code “coverage >80%” in the body.** Correct phrasing: coverage reports are traceable per CI artefact; core modules (Pricing Core billing, Import parsing, Bill dual-track) are protected by automated tests; **exact percentages are those on the appendix screenshot for that build date**.

### 8.4.1 How to Read Coverage (CI Artifact)

Each green CI build uploads **`coverage-report`** (HTML) and a text **Summary**. Interpretation guide:

| Area | Expected | Reason |
| --- | --- | --- |
| `CloudWarehouse.Pricing.Core` / Billing | Higher | Dense unit tests on Strategy + `FeeCalculationEngine` |
| Import helpers / Excel parsing | Higher | Multi-format Excel tests + workbook 93 fixtures |
| `Modules/Billing` (dual-track orchestration) | Medium–high | Integration tests + `BillLineTotals` unit tests |
| `wwwroot/index.html`, thin controllers | Lower | MVP relies on API tests; **Playwright smoke covers main nav and three panels** (§8.3.2) |
| Assistant module | Medium | `QuoteAssistantTests` + eval golden set |

**Figure 8-3 / Appendix A-02:** Download `coverage-report` from Actions for that build; screenshot Summary and module rows—**cite that build’s numbers only**; body text stays qualitative.

## 8.5 SAST and Dependency Supply-Chain Scanning

### 8.5.1 CodeQL SAST

Workflow: `.github/workflows/codeql.yml` (CodeQL SAST).

- Triggers: trunk push/PR + weekly cron  
- Language: C#  
- Queries: `security-and-quality`  
- Flow: init → restore/build → `codeql-action/analyze`  

SAST finds automated defect patterns before merge; it does not replace design review or equal DAST. Findings: fix → re-run green → then merge.

### 8.5.2 NuGet vulnerable package scan

CI runs `dotnet list package --vulnerable --include-transitive` and archives results. Current policy prioritises visibility (`continue-on-error: true`); may later become a hard gate.

## 8.6 Application-Layer Security Controls (Delivered)

| Control | Notes |
| --- | --- |
| Upload extension whitelist | Price/waybill uploads allow only `.xlsx` / `.xlsm` (etc.) |
| File size limit | Caps upload size; reduces large-file DoS surface (Risk T3) |
| Sanitised configuration | `appsettings.example.json`; real connection strings stay off-repo |
| Demo environment assumption | Local / controlled intranet; JWT/RBAC deferred per ADR; on risk register and Planned milestone |

These form a pragmatic MVP baseline—not a production zero-trust claim.

## 8.7 Performance Baseline (Lightweight Load / Smoke)

Numbers below are **reproducible smoke baselines** (xUnit + `Stopwatch` / concurrent `Task.WhenAll`), **not** k6/JMeter production load tests and **not** SLA certification.

**Measured (same run as §8.3.1, `dotnet test`, 25 Aug 2026, Windows):**

| Test | Scenario | Threshold | Measured | Result |
| --- | --- | --- | --- | --- |
| `Import1000RowPerfTests` | **1000-row** standard price table parse (no SQL) | &lt; 30 s | **114 ms** (`[PERF]` log line) | Pass |
| `FeeCalculationPerfSmokeTests` | **1000×** `CalculateActive` on fee engine | &lt; 200 ms | Test passed (see output) | Pass |
| `StressLoadTests.TemplateDownload_30Concurrent` | **30 concurrent** template downloads | All HTTP 200, &lt; 10 s | Passed (seconds-scale) | Pass |
| `StressLoadTests.PriceTablePreview_15Concurrent` | **15 concurrent** preview POSTs | All succeed | **~203 ms** total (that run) | Pass |

Reproduce:

```text
dotnet test CloudWarehouse.Tests --filter "FullyQualifiedName~Import1000Row|FullyQualifiedName~FeeCalculationPerf"
dotnet test CloudWarehouse.IntegrationTests --filter "Category=Stress"
```

**Figure 8-5 (screenshot):** CI or local log showing `[PERF] ExcelHelper.ReadPriceTable 1000 rows: 114 ms` and green `StressLoadTests` (Appendix **A-13**).

**Honest limits:** No long soak tests, no 10k-user simulation, no isolated SQL Server bench. CI without a business DB may skip some integration cases—cite Actions **Passed/Skipped** when comparing to local **114 passed**.

## 8.8 Honest Gaps and Plans

| Capability | Status | Notes |
| --- | --- | --- |
| DAST (e.g. OWASP ZAP) | Not a standing gate | May plan baseline scans on demo environment |
| Playwright / UI E2E automation | **Smoke implemented (4 tests)** | Upload + Preview full path, cross-browser — see §8.3.2 Planned |
| Infrastructure as Code (Terraform/Bicep) | Not implemented | Self-contained publish + `database/*.sql`; audit trail §8.8.1 |
| Full CD to production | Not implemented | CI + manual / checklist publish |
| Container image scanning (e.g. Trivy) | Not main path | Primary delivery is self-contained publish packages |
| JWT / RBAC | Planned | Per ADR |
| Force HTTPS / tighten CORS | Must before go-live | Convenience config in development |
| Secrets vault (e.g. Vault) | Not implemented | Example config + machine-local secrets |

### 8.8.1 Infrastructure as Code (IaC) and Audit Trail

No Terraform/Bicep/Ansible for cloud resources (no multi-tenant cloud MVP). Honest equivalents:

| Capability | Status | Notes |
| --- | --- | --- |
| Cloud IaC | **N/A** | `dotnet publish` + on-prem SQL Server |
| Database as code | **Done** | `database/schema.sql` in Git |
| CI as code | **Done** | `ci.yml`, `codeql.yml`, `pages.yml` |
| Config templates | **Done** | `appsettings.example.json` |
| Publish checklist | **Done** | `deploy-iis-publish-checklist.md` |
| Version audit | **Done** | Git + GitHub Actions build IDs |

**Planned:** Bicep or Docker Compose for a fixed demo server if needed.

### 8.8.2 Containers and Regulatory Compliance

| Item | Status |
| --- | --- |
| Container images / Trivy | **N/A** (self-contained publish primary) |
| SOC2 / HIPAA / GDPR certification | **Not applicable** — intranet demo; no regulated health/payment data processed |

Overall: CI + layered tests + SAST + dependency visibility + explicit backlog demonstrate engineering hygiene; transparent gaps avoid marketing overclaim.

## 8.9 Local Development vs CI

| Dimension | Local | CI (GitHub Actions) |
| --- | --- | --- |
| OS | Typically Windows | `ubuntu-latest` |
| SQL Server | Often available | Usually absent; DB tests skip / limited |
| Coverage | Optional manual | Forced each build + artefact upload |
| State | Stateful workstation | Ephemeral runner |

Cross-platform, ephemeral CI reduces “works on my machine” risk.

## 8.10 Evidence Checklist

| Evidence | Location |
| --- | --- |
| CI workflow | `.github/workflows/ci.yml` |
| CodeQL workflow | `.github/workflows/codeql.yml` |
| CI activity diagram | `docs/diagrams/09-cicd-pipeline.puml` |
| Actions green run | GitHub Actions screenshot |
| Test summary (110 passed) | Actions **Test with coverage** or `artifacts/dotnet-test-full.txt` |
| Load smoke `[PERF]` output | Same log / `artifacts/load-stress.txt` |
| Coverage artefact | `coverage-report` / Summary screenshot |
| NuGet scan artefact | `nuget-vulnerable-scan` |
| Tests | `CloudWarehouse.Tests`, `CloudWarehouse.IntegrationTests` |
| DB skip policy | `DatabaseAvailability` (and related) |
| Perf smokes | `Import1000RowPerfTests`, `FeeCalculationPerfSmokeTests` |

## 8.11 Chapter Summary

Strategy and dual-track changes sit under repeatable CI and a test pyramid, with CodeQL and dependency scanning for baseline security visibility; DAST, full CD, authentication, and transport hardening remain gaps or Planned. The next chapters cover risk management, mid-term feedback response, and conclusions.

---

# Chapter 9 Risk Management

Chapter 8 showed how quality and security become hard gates via automation. This chapter covers **project, technical, and security** risks: identification, mitigations, Planned items, and verification evidence. Risk governance is tied to weekly sprints: review the register at sprint start and fold mitigations into Must work—not post-hoc decorative tables.


> **【插图占位 9-1】** 风险登记示意
> - 来源：`docs/diagrams/12-risk-management.puml`
> - 操作：导出 PNG 后粘贴到下方虚线框内（约半页高度）
>
> ```
> ┌──────────────────────────────────────────────────────────┐
> │                                                          │
> │              【在此粘贴图片 9-1】                        │
> │                                                          │
> └──────────────────────────────────────────────────────────┘
> ```
>
> *图注：项目/技术/安全三类风险。*

*(Figure placeholder — paste PNG; Chinese instructions above are authoritative.)*

Diagram: `docs/diagrams/12-risk-management.puml`; oral one-pager: `docs/project-management/risk-management-slide.md`.

## 9.1 Risk Management Method

| Step | Practice on this project |
|----------|----------------|
| Identify | Abstract from real events (Excel import failures, CI env drift, demo exposure)—no invented catalogue filler |
| Assess | Qualitative matrix: likelihood × impact (see §9.5) |
| Mitigate | Close MVP-fixable risks immediately (preview, TX rollback, upload whitelist, CI); product decisions go to ADR / Planned |
| Track | Link to milestones and personal hour variance (e.g. R1 ↔ Sprint 2 +39%) |

This is a **solo** internship. Register and hours are personal. The supervisory item “new developer hours from some Sprint” is **N/A**—no second developer; do not invent team capacity.

## 9.2 Project Risks

| ID | Description | Impact | Mitigations done | Follow-on / Phase 2 |
|----|----------|----------|----------------|------------------|
| R1 | Sprint 2 Excel three-row complexity caused +39% hour overrun | Compresses later sprint feature time | Strict preview-then-commit; re-estimate after S2; buffer external-file work | Keep buffers for similar integrations |
| R2 | Solo scope creep (manual rule CRUD, early auth, excess diagrams) | Core quality drop; milestone slip | MoSCoW; ADR lock (Excel-first rules; auth deferred) | Maintain ADR discipline |
| R3 | Local SQL Server vs cloud CI drift | Local green / CI red or false green | Version all schema via `database/*.sql`; forced `dotnet test` on Actions; explainable DB skips | Keep schema and tests in sync |

**R1 effectiveness:** overrun concentrated in Sprint 2; Sprints 3–4 returned to ±10% (Chapter 4)—retrospective corrections worked.

## 9.3 Technical Risks

| ID | Description | Impact | Mitigations done | Follow-on |
|----|----------|----------|----------------|----------|
| T1 | Legacy three-row misalignment → parse → wrong fees | Business loss | `ExcelHelper` header auto-detect; standard templates; dual-format unit tests | Extreme-chaos column-mapping UI Planned (not this phase) |
| T2 | Partial import mixes old/new rules on a lane | Integrity / fee chaos | Whole import in `SqlTransaction`; fail → full rollback (ADR-4) | Sufficient at current scale |
| T3 | Huge Excel → OOM / timeouts | Unavailability | Extension whitelist + size cap (~10 MB) | **Planned:** streaming, chunking, background jobs, resume—**not delivered**; do not claim otherwise |
| T4 | Wrong unique index `(SiteId, DestId, EffectiveDate)` blocks one-to-many bands | Import failure | `fix-price-rules-index.sql` drops bad unique index | Schema changes only via scripts |

T1 and R1 share root cause (external file complexity → schedule risk). T4 is indexed in Chapter 5 decisions.

## 9.4 Security Risks

| ID | Description | Impact | MVP mitigations | Longer term |
|----|----------|----------|----------------|----------|
| S1 | No authZ on API/UI | Arbitrary tampering if exposed publicly | Local / controlled intranet demos; ADR defers auth | JWT + RBAC (options below) |
| S2 | Dev CORS `AllowAll` | Larger attack surface if cross-origin deploy | Docs mark as development-only | Production Origin whitelist |
| S3 | Secret leak / malicious upload | Credential theft / intrusion | `.gitignore`; `appsettings.example.json`; upload whitelist/size; CodeQL + dependency scan | User Secrets / vault; force HTTPS before production |

### S1 authentication options (two schemes as requested)

| Dimension | Option A: Existing WMS / enterprise SSO | Option B: Standalone JWT + RBAC |
|----------|-------------------------------|-------------------------|
| Integration cost | High—IdP dependency and joint windows | Medium—users/roles local |
| Account management | Centralised enterprise | System-local |
| Demo independence | Needs enterprise test env | Can demo standalone |
| Recommendation | Prefer if CloudWarehouse must embed in WMS | **Default recommended Phase 2 path:** controllable; fits Modular Monolith |

## 9.5 Risk Matrix (Pre-mitigation Qualitative)

|  | Low impact | Medium impact | High impact |
|--|--------|--------|--------|
| **High likelihood** |  |  | T1 legacy header parse errors |
| **Medium likelihood** | S2 loose CORS | R1 schedule overrun; T3 large-file perf | S1 no auth (impact rises to High if internet-exposed) |
| **Low likelihood** | S3 secret leak (with sanitisation habits) | R2 scope creep; T2 partial import (lower after TX) |  |

**Reading:** T1 was the most real development-phase risk and manifested as Sprint 2 overrun. S1 is controllable for local demos but **must be closed before any external deployment**.

## 9.6 Mitigation Effectiveness Evidence

| Risk ID | Suggested evidence |
|--------|--------------|
| T1 | `ExcelHelperTests`; import success/fail UI; standard templates |
| T2 | UI “not persisted” after failed import; sequence diagram 08 |
| T3 | Illegal extension rejected; size-limit error |
| T4 | `fix-price-rules-index.sql`; successful import after fix |
| R1 | Chapter 4 hours table + `sprint-hours-chart.html` |
| R3 | Actions green; `DatabaseAvailability` skip notes |
| S1–S3 | ADR; Chapter 8 controls and gaps; never show real connection strings |

## 9.7 Risks Related to Parallel PDA Delivery

| Risk | Mitigation |
|----------|----------|
| Dual systems compete for solo bandwidth | MoSCoW + separate CW/PDA hour columns |
| Overclaim “settlement API already connected” | Context map marks integration Planned; defence ban words |
| Hardware bring-up uncertainty | Buffer hours; done = demoable start/report closed loop |

## 9.8 Chapter Summary

Risk management is operational: R1 is evidenced as contained by later hour variance; T1/T2/T4 are closed in code/scripts; S1 acknowledges the gap with two concrete options. The next chapter maps each mid-term comment to design, architecture, evidence, and these risk actions.

---

# Chapter 10 Response to Mid-term Supervisory Feedback

Mid-term assessment required that the final report and presentation comprehensively answer every comment with verifiable evidence. Source notes (Chinese and English) are retained in the repository root `log` file. This chapter maps each comment to executed improvements, report sections, and artefact paths.

## 10.1 Feedback Sources and Response Principles

| Principle | Practice |
|------|--------------|
| Done only with evidence | Point to diagrams, tests, CI records, UI screenshots |
| No fake packaging | DAST, full CD, JWT, irregular/penalty strategies remain Planned |
| Solo hours tracked separately | Chapter 4 Planned vs Actual; second developer = **N/A** |
| Ban-word discipline | No: microservices live; AI billing; CW–PDA settlement API connected; production HA built |

English-window mid-term points align with Chinese responses: deepen billing complexity with patterns in Phase 2; multi-view architecture; physical topology stating infrastructure and redundancy (or honesty if none); defend the monolith; DDD supporting Modular Monolith; artefacts for all deliverables.

## 10.2 One-Page Mapping Table

| Mid-term comment | Response summary | Primary chapters |
|----------|--------------------|----------|
| Overall implementation too simple; need design depth | Phase 2: Strategy, dual-track historical pricing, rule retrieval, multi-view diagrams, CI/SAST | 6, 7, 8, 10 |
| Monolith needs justification | Modular Monolith intentional + extraction triggers | 6 |
| Billing variants need patterns + class/interaction diagrams | Tier/Overweight/Volumetric done; class 13, dual-track sequence 14 | 7; ADR-8 |
| Add multi-view architecture diagrams | Logical, physical, deployment, DDD, enterprise context map, CI activity | 6, 8; `docs/diagrams/*` |
| Physical architecture must state infra / redundancy | Nodes, ports, single-instance; **explicitly no HA** | 6.5–6.6 |
| Explain DDD thoroughly | Contexts map 1:1 to `Modules/*`; honest: not full domain-event framework | 6.3; figs 05, 16 |
| All work needs verifiable evidence | CI, CodeQL, coverage artefacts, tests, screenshots | 8, 9; Appendix |
| Split personal Planned vs Actual | Phase 1 198→211h; Sprint 2 +39% | 4 |
| Risk mitigations must be concrete | Preview, TX rollback, upload whitelist, index fix done; large-file streaming Planned; auth two options | 9 |
| Future plan must be quantified | Milestone statuses; Phase 2 Strategy packages Done | 4, 10.6 |

## 10.3 Highest-Priority Comments — Expanded

### 10.3.1 “Too simple” and billing variants need design patterns

**Core ask:** deepen billing rule variants in phase two; use design patterns where flexible; show class and interaction diagrams.

**Response:**

1. Strategy Pattern delivered: `TierBillingStrategy`, `OverweightBillingStrategy`, `VolumetricBillingStrategy` orchestrated by `FeeCalculationEngine` + resolver.  
2. Artefacts: `13-billing-strategy-class.puml`, `14-sequence-waybill-dual-track.puml`.  
3. Business depth: dual-track settlement (receivable customer quotes vs payable cost) + historical rates by ship/bill date.  
4. Honest boundary: irregular pieces, overtime penalties, etc. remain **Planned**—OCP path shown, not fake “all done”.

**Evidence:** Chapter 7; `BillingStrategyTests`; waybill match/mismatch UI (Appendix).

### 10.3.2 All work must have verifiable evidence

**Response:**

1. CI: `.github/workflows/ci.yml` + Actions green screenshots; coverage via artefacts—**never hard-code >80%**.  
2. SAST: `.github/workflows/codeql.yml` + successful runs.  
3. Tests: unit, integration, light concurrency/perf smokes.  
4. Functional screenshots: import success/fail, dual-track compare, PDA start/report in Appendix.

**Evidence:** Chapter 8 checklist; Appendix index.

### 10.3.3 Hours must split personal Planned vs Actual

**Response:**

1. Solo table: S1 48→52; S2 44→61; S3 56→51; S4 50→47; Phase 1 total **198→211 (+7%)**.  
2. Second developer: **N/A**.  
3. Phase 2: Chapter 4 reserved CW/PDA hour tables—fill real numbers before final submission.

**Evidence:** Chapter 4; `sprint-hours-chart-data.csv` / chart screenshot.

## 10.4 High-Priority Comments — Expanded

### 10.4.1 Architecture diagram upgrade (multi-view)

| View | File |
|------|------|
| Constraints / ADR | `01*.puml` |
| Logical | `02-logical-architecture.puml` |
| Physical | `03-physical-architecture.puml` |
| Deployment | `04-deployment-diagram.puml` |
| DDD bounded contexts | `05-ddd-bounded-contexts.puml` |
| Use cases | `06-use-case-diagram.puml` |
| ERD | `07-erd.puml` |
| Import / dual-track sequences | `08`, `14` |
| CI activity | `09-cicd-pipeline.puml` |
| Enterprise context map | `16-enterprise-context-map.puml` |
| Strategy class | `13-billing-strategy-class.puml` |

### 10.4.2 Physical architecture: infrastructure and redundancy

**Response:** Document demo topology (Kestrel, SQL Server 1433, CI runner, PDA parallel nodes); backups via manual bak / script rebuild; **explicitly no load balancer and no DB HA**. Honesty satisfies “write redundancy if any; write none if none”.

### 10.4.3 Monolith rationale + microservice evolution path

**Response:** Chapter 6 compares microservices / mud-ball / Modular Monolith; states extraction triggers (team size, independent scale, release-cadence conflict, tech heterogeneity, quantified QPS, etc.). M8 remains Planned. Candidate contexts: Import / Pricing / Master Data—extract only when triggered.

### 10.4.4 DDD must be explained thoroughly

1. Five CloudWarehouse contexts (Master Data, Import, Pricing, Billing, Assistant) plus independent PDA context.  
2. 1:1 mapping to `Modules/*`.  
3. Interaction honesty: same-process sync calls + same-DB transactions—not a live event bus.  
4. Boundary: DDD-informed modular design, not a full domain-event / aggregate-root framework clone.

## 10.5 Medium-Priority Comments — Expanded

### 10.5.1 Concrete risk mitigations

| Named risk | Report landing |
|------------|------------|
| Large-file upload | Whitelist + size limit done; streaming/chunk/resume **Planned** (T3) |
| Three-row header parse | Auto-detect + template + unit tests (T1); linked to Sprint 2 overrun |
| No login | JWT/RBAC vs WMS SSO comparison (S1) |

### 10.5.2 Quantified future plan

| Work package | Status | Notes |
|--------|------|------|
| Strategy + volumetric | Done | ~Sprint 5; Chapters 7, 4 |
| Rule knowledge retrieval | Done | Assistive lookup; not settlement SoR |
| Dual-track + historical price | Done | Sequence 14 |
| Perf baseline (1000-row parse, etc.) | Partial Done | Smokes done; numeric screenshots → Appendix |
| JWT/RBAC | Planned | Hours on plan table; owner = author (solo) |
| Import microservice study | Planned | Needs stable boundaries + triggers |
| Full CD | Planned | Currently CI + publish/checklist |

> Phase 2 personal hours must be filled in Chapter 4 before finalisation and kept consistent with this plan table.

### 10.5.3 Bonus-item status

| Suggested bonus | Status |
|------|------|
| Billing complexity analysis + pattern extension | Chapters 7, 10 variant table + three-step OCP path |
| Perf baseline: 1000-row parse | `Import1000RowPerfTests`; numbers in Appendix |
| Design decision comparisons (Dapper vs EF; monolith vs microservices) | Chapter 2 selections + Chapter 6 architecture |

## 10.6 Extra Delivery: Parallel PDA and Value Boundaries

Mid-term focus was CloudWarehouse depth. Phase 2 additionally delivered **Honeywell PDA no-order reporting**, enriching the factory-digitisation narrative while respecting:

- **No production settlement API integration** with CloudWarehouse;  
- Enterprise context map marks cross-system integration as **Planned**;  
- PDA hours and evidence listed separately—neither to hide CloudWarehouse design depth nor to exaggerate PDA integration.

## 10.7 Appendix Screenshot Checklist (Author Action)

Before final submission:

- [ ] GitHub Actions CI green  
- [ ] Coverage Summary  
- [ ] CodeQL success  
- [ ] Price import success / failure  
- [ ] Dual-track waybill preview  
- [ ] Hours bar chart  
- [ ] PDA start/report  
- [ ] (Optional) Illegal extension rejected  

## 10.8 Chapter Summary

Mid-term comments became checkable engineering and documentation actions: billing deepened via Strategy and dual-track; architecture clarified via multi-view and honest no-HA statements; quality proven via CI/SAST/tests; management closed via personal hours and concrete risks. The next chapter concludes with limitations, outlook, client feedback placeholder, and submission checklist.

---

# Chapter 11 Conclusion and Outlook

## 11.1 Conclusion

Under solo conditions, this internship delivered two parallel systems: **CloudWarehouse** (Modular Monolith freight-settlement MVP with Phase 2 Strategy billing, receivable/payable dual-track and historical pricing, plus **built-in rule RAG** for assisted FAQ lookup) and a **Honeywell PDA no-order reporting MVP**. Both serve one factory goal but evolve as separate bounded contexts; **this phase did not implement production API integration**.

Mid-term feedback was answered with verifiable artefacts: multi-view architecture and honest no-HA statements; Strategy class diagram and dual-track sequence; CI/CodeQL/test evidence; personal Planned vs Actual (Phase 1: **198→211 hours**). Rule RAG is lexical FAQ retrieval enhancement and **does not replace** `FeeCalculationEngine` as system of record.

## 11.2 Known Limitations

- No JWT/RBAC; CORS/HTTP are demo configurations  
- No production HA / full CD / standing DAST gate  
- Volumetric engine ready; waybill Excel main path still primarily actual weight  
- Irregular / penalty billing variants remain Planned  
- Rule RAG is lexical Retrieve→Augment→Generate (not production vector semantic RAG); excerpt-style generation when ApiKey is unset  

## 11.3 Outlook (Quantified Directions)

| Item | Status | Dependency |
|----|------|------|
| JWT + RBAC | Planned | ADR |
| Demo-environment DAST baseline | Planned | Stable demo deployment |
| Full CD | Planned | Auth and target publish environment |
| Microservice extraction | Planned | Triggers (Chapter 6) |
| CloudWarehouse ↔ PDA integration | Planned | Stable ID / file-exchange conventions |

## 11.4 Client Feedback

### 11.4.1 Enterprise mentor feedback (business value)

From the enterprise side, this internship delivered useful work on two factory problems at once: warehouse freight checking, and shop-floor reporting when there is no formal work order.

On the warehouse track, CloudWarehouse moved settlement away from “guess the Excel sheet” toward a repeatable path: master data, cost and quote import, fee trial, and dual-track receivable versus payable preview by ship date. For supervisors, the valuable part is not a fancy slogan — it is that preview results can be checked, and mismatches can be explained instead of hidden. The Modular Monolith choice also matched our constraint: one developer, limited time, and a need for a working system rather than an early microservice split.

On the line track, the PDA no-order app addressed a pain we see every night shift: work still happens when MES has no order. Workers can log in, pick a machine, start, and report on an industrial handheld. Site feedback has been clearly positive — people prefer scan-and-save over paper or verbal notes because the record can be traced later. Recent shop-floor volume also supports this: in a seven-day window, the no-order path (including PDA dual-write) accounted for the overwhelming majority of mesdb report rows, while the formal “with work-order” path was rarely used. In short, the floor is using the path this project strengthened.

Overall assessment: the intern worked as a solo owner end to end — requirements, design, implementation, test/CI evidence, and honest documentation of gaps (for example auth, full CD, and future join between the two systems). What was promised as MVP is demonstrable. What was deferred was deferred on purpose, not ignored. We accept the delivery for this term and will keep CloudWarehouse and PDA evolving as parallel systems until shared IDs and integration timing are ready.

### 11.4.2 Sponsor formal acceptance status

| Question | Answer |
| --- | --- |
| Written formal sign-off (email/letter)? | **No** on file as of final submission. |
| Practical acceptance | **Demo acceptance + field use:** mentor confirmed demonstrable MVP; PDA path used on shop floor; dual-track preview accepted verbally for reconciliation scenarios. |
| Equal to enterprise production go-live? | **No** — demo/intranet; JWT, HA, CW↔PDA API integration remain Planned. |
| Wording for grading | Sponsor **accepts this term’s internship deliverables**; **not** a full production rollout sign-off. |

## 11.5 Submission Checklist

- [ ] This report (Chinese final + figures)  
- [ ] English version (translated from final facts—do not rewrite facts)  
- [ ] Seven assessment videos  
- [ ] Appendix screenshots complete (see Appendix A)  

---

# Appendix A Evidence and Screenshot Checklist

Paste the following screenshots into the appendix (about half page each):

| ID | Content | Suggested source |
|------|------|----------|
| A-01 | GitHub Actions CI green | Actions web UI |
| A-02 | Coverage Summary | CI artefact |
| A-03 | CodeQL success | Actions |
| A-04 | Price import success | Admin UI |
| A-05 | Price import failure / not persisted | Admin UI |
| A-06 | Dual-track waybill preview match/mismatch | Admin UI |
| A-07 | Hours bar chart | `sprint-hours-chart.html` |
| A-08 | PDA start/report | Device or emulator |
| A-09 | Rule RAG query result (three-step pipeline) | Admin “规则 RAG” tab |
| A-10 | Illegal extension rejected (optional) | UI |
| A-11 | Solution structure / Modules folder (optional) | IDE |
| A-12 | Test pass summary (**114 passed**, incl. E2E) | GitHub Actions → CI → Test with coverage |
| A-13 | Load smoke `[PERF]` / `StressLoadTests` | `perf-load-stress-detailed.txt` or QA page |
| A-14 | NuGet Moderate scan + resolution note | QA page / CI artefact |
| A-15 | Public QA report homepage | https://chenyuxiangAK47.github.io/cloudwarehouse-csharp/ |
| A-16 | Playwright E2E four passes | `artifacts/e2e-playwright-test.txt` |
| A-07b | Cumulative hours burndown (planned vs actual line) | `sprint-burndown-cumulative.csv` |

---

# Chapter 12 Reflection Questions

> **Required by supervisor email.** Answers reflect solo delivery, dual systems, and post–mid-term revisions.

**Q1. Greatest learning?** Moving from “working features” to **verifiable engineering**: Strategy + dual-track design, **114** automated tests (incl. Playwright smoke), CI coverage, CodeQL, and a public QA page.

**Q2. Greatest difficulty?** Sprint 2 Excel overrun (+39%). Mitigated via preview-before-commit, golden samples, and estimation buffers; Phase 2 scope controlled by keeping CW and PDA decoupled.

**Q3. Changes after mid-term feedback?** Added Analysis (§3.0), class-level sequence, Sprint backlog tables, security resolution narrative, Playwright E2E, QA site; clarified sponsor acceptance vs formal sign-off.

**Q4. Solo Sprint/agile — effective?** Yes with simplification: CSV + Markdown backlog + Git instead of Jira; cumulative hours instead of story-point burndown.

**Q5. What would you do differently?** Earlier Playwright smoke; earlier security baseline logging; more frequent Word sync from master Markdown.

**Q6. AI tools?** Used for drafts and scaffolding; billing semantics, hours, acceptance boundaries, and ban-words verified personally. See `ai-assistance-disclosure.md`.

**Q7. Sponsor relationship?** Value is explainable dual-track preview and PDA no-order capture—not buzzwords. Honest “no formal sign-off” supports credible Phase 3 integration planning.

**Q8. Next growth?** JWT/RBAC, DAST baseline, light IaC (Bicep/Compose), SQL performance.

---

# Appendix B Terminology and Ban-Word Quick Reference

| Correct | Forbidden |
|------|------|
| Dual-track = receivable quotes vs payable cost | Domestic / international lanes |
| Modular Monolith; no HA | Microservices already live; production multi-active already built |
| Strategy Tier / Overweight / Volumetric Done | JSON rule engine; AI smart billing |
| Built-in rule RAG (lexical FAQ retrieval) | AI settlement / vector RAG in production |
| PDA not integrated with CloudWarehouse settlement API | Already connected / “Parallel Data Aggregator” misuse |
| Coverage cited from CI artefact | Hard-coding >80% in body text |
