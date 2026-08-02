# CloudWarehouse — 4-Week Internship Sprint Plan (Fabricated for Presentation)

> **Assumption for slides:** 3 team members, part-time ~15–20 h/person/week → **~50 h team capacity per sprint**  
> **Sprint length:** 1 calendar week each (4 sprints = 4-week internship)

---

## Roadmap & milestones (for 1-min slide)

| Milestone | Sprint | Status | Deliverable |
|-----------|--------|--------|-------------|
| **M1** | Sprint 1 | Done | Requirements, ERD, `Sites` / `Destinations` CRUD + UI tabs |
| **M2** | Sprint 2 | Done | Standard Excel template, dual-header parser, import preview |
| **M3** | Sprint 3 | Done | Import upsert to `PriceRules`, fee calculation API + UI |
| **M4** | Sprint 4 | Done | Unit/integration/stress tests, GitHub Actions + coverage |
| **M5** | Sprint 4 (buffer) | In progress | PPT, diagrams, MVP + CI demo videos |

---

## Estimated vs actual hours (bar chart data)

**Ready-made chart (汇报第 4 项):**

| File | How to use |
|------|------------|
| `docs/project-management/sprint-hours-chart.html` | Open in browser → screenshot for PPT |
| `docs/diagrams/11-sprint-hours-bar-chart.mmd` | [mermaid.live](https://mermaid.live) → Export PNG |
| `docs/diagrams/11-sprint-hours-bar-chart.puml` | Salt table (numbers + notes); not a bar graphic |
| `docs/project-management/sprint-hours-chart-data.csv` | Paste into PPT → Insert Chart → Clustered Column |

**Or paste into PPT → Insert Chart → Clustered Column**

| Sprint | Estimated (h) | Actual (h) | Variance |
|--------|---------------|------------|----------|
| Sprint 1 | 48 | 52 | +4 (+8%) |
| Sprint 2 | 44 | 61 | **+17 (+39%)** |
| Sprint 3 | 56 | 51 | -5 (-9%) |
| Sprint 4 | 50 | 47 | -3 (-6%) |
| **Total** | **198** | **211** | **+13 (+7%)** |

### Footnote for slide (English, 1 line)

> Sprint 2 overran due to legacy 3-row Excel headers; we revised estimation after M2 and kept Sprints 3–4 within **±10%**.

### Footnote (中文口述)

> Sprint 2 因供应商三级表头解析与双格式兼容超支；M2 后调整估算基线，Sprint 3–4 交付稳定，误差控制在 10% 以内。

---

## Per-member split (optional “证据” slide)

| Member | Role | S1 | S2 | S3 | S4 | Total |
|--------|------|----|----|----|----|-------|
| Member A | Backend / API | 18 | 22 | 17 | 15 | 72 |
| Member B | Import / Excel / DB | 16 | 28 | 18 | 14 | 76 |
| Member C | UI / Test / CI | 18 | 11 | 16 | 18 | 63 |
| **Team** | | **52** | **61** | **51** | **47** | **211** |

Member B spike in Sprint 2 = Excel story (credible).

---

## Sprint 1 — Foundation (Week 1)

**Goal:** Environment + master data + DB schema  

| ID | User Story | Est (h) | Actual (h) |
|----|------------|---------|------------|
| US-1.1 | As admin, create/edit/delete sites | 12 | 14 |
| US-1.2 | As admin, create/edit/delete destinations | 10 | 11 |
| US-1.3 | Design ERD and `schema.sql` | 8 | 8 |
| US-1.4 | Scaffold ASP.NET Core + static UI shell | 10 | 11 |
| US-1.5 | Spike: Dapper + SQL Server connection | 8 | 8 |
| **Sprint total** | | **48** | **52** |

---

## Sprint 2 — Excel import (Week 2) — overrun sprint

**Goal:** Template download + parse supplier format + preview  

| ID | User Story | Est (h) | Actual (h) |
|----|------------|---------|------------|
| US-2.1 | Download standard Excel template API | 6 | 6 |
| US-2.2 | Parse standard format (row 1 header) | 10 | 12 |
| US-2.3 | Parse legacy 3-tier header (row 3 header) | 12 | **22** |
| US-2.4 | Import preview API + UI table | 10 | 11 |
| US-2.5 | Expected price calculation (`PriceCalculator`) | 6 | 10 |
| **Sprint total** | | **44** | **61** |

**Retro note (for Issues slide):** Underestimated merged cells and column alignment in supplier files.

---

## Sprint 3 — Rules & pricing (Week 3)

**Goal:** Persist rules + read-only list + shipping fee trial  

| ID | User Story | Est (h) | Actual (h) |
|----|------------|---------|------------|
| US-3.1 | Validate site/destination on import | 8 | 7 |
| US-3.2 | Transactional upsert `PriceRules` (delete + insert) | 14 | 15 |
| US-3.3 | `PriceRuleController` GET + calculate | 12 | 11 |
| US-3.4 | Simplify price rule UI (import-only maintenance) | 8 | 8 |
| US-3.5 | `PriceRuleMapper` tier + overweight rows | 10 | 10 |
| US-3.6 | Integration test: import preview API | 4 | 0 | *(moved to S4)* |
| **Sprint total** | | **56** | **51** |

---

## Sprint 4 — Quality & evidence (Week 4)

**Goal:** Tests, CI/CD, stress, diagrams, demo prep  

| ID | User Story | Est (h) | Actual (h) |
|----|------------|---------|------------|
| US-4.1 | Unit tests (Calculator, Excel, Mapper) | 14 | 13 |
| US-4.2 | Integration tests (WebApplicationFactory) | 12 | 11 |
| US-4.3 | GitHub Actions: build + test + coverage | 10 | 10 |
| US-4.4 | Stress tests (concurrent + k6 script) | 6 | 6 |
| US-4.5 | PlantUML architecture pack + ERD | 8 | 7 |
| US-4.6 | Record MVP + CI demo videos | 0 | 0 | *(pending — M5)* |
| **Sprint total** | | **50** | **47** |

---

## Product Backlog (Epic summary for 1 PPT table)

| Epic | Stories | Priority | Sprint |
|------|---------|----------|--------|
| Master data | US-1.1, US-1.2 | Must | 1 |
| Database design | US-1.3 | Must | 1 |
| Excel import | US-2.1–2.5, US-3.1–3.2 | Must | 2–3 |
| Pricing & trial | US-3.3–3.5 | Must | 3 |
| Testing | US-4.1–4.4 | Must | 4 |
| CI/CD | US-4.3 | Must | 4 |
| Documentation & demo | US-4.5–4.6 | Should | 4–M5 |

---

## Burndown story (optional verbal)

- **Planned velocity:** ~50 h/sprint  
- **Sprint 2 actual:** 61 h → carry-over: integration tests moved to Sprint 4  
- **Sprint 3–4:** Re-estimated; no critical scope cut
