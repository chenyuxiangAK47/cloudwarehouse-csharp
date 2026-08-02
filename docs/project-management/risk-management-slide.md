# Risk Management — PPT Slide (1 page)

> Copy into one slide: **3 columns × 3 rows**. English for NUS presentation; 中文口述见每段下方。

---

## Slide title

**Risk Register — Project, Technical & Security**

Subtitle (optional): *Identified during 4-week solo internship; mitigations implemented or planned.*

---

## Column layout (paste into PPT table)

| **Project risks** | **Technical risks** | **Security risks** |
|-------------------|---------------------|--------------------|
| **R1 — Sprint overrun** (Excel dual-format took +39% in S2) → **Mitigation:** preview-before-commit workflow; re-baselined estimates; standard template for suppliers | **T1 — Legacy 3-row Excel headers** mis-map columns → **Mitigation:** auto-detect row 1 vs row 3; downloadable standard template (ADR-5) | **S1 — No authentication** on API/UI → **Plan:** JWT + roles (admin / read-only) in Phase 2 |
| **R2 — Scope creep** (solo dev, many diagram/CI deliverables) → **Mitigation:** ADR scope lock; Must vs Should backlog; milestones M1–M5 | **T2 — Partial import corrupts prices** → **Mitigation:** SQL transaction; any row error rolls back entire batch (ADR-4; see sequence diagram) | **S2 — CORS AllowAll** in MVP → **Plan:** restrict allowed origins in production deployment |
| **R3 — Environment drift** (local SQL vs CI) → **Mitigation:** `schema.sql` + `fix-price-rules-index.sql`; GitHub Actions runs 37+ automated tests on every push | **T3 — DB schema mismatch** (wrong unique index blocked tier rules) → **Mitigation:** non-unique index on `(SiteId, DestId)` only; documented fix script | **S3 — Secrets & file upload** (connection string in config; Excel upload) → **Now:** `.xlsx/.xlsm` whitelist + size limit · **Plan:** User Secrets / Key Vault; upload scanning |

---

## 30-second oral script (English)

> We maintain a simple risk register in three categories. Project risks include Sprint 2 overrun on Excel parsing—we mitigated with preview and re-estimation. Technical risks focus on data integrity: all-or-nothing import and dual-header parsing. Security is honest MVP gaps: no auth yet, with JWT and CORS lockdown planned. CI and schema scripts reduce environment drift.

---

## 中文口述（20 秒）

> 风险分三类：管理上 Sprint 2 Excel 超支，用预览和重估应对；技术上靠事务整批回滚和双表头解析；安全上 MVP 未做登录，答辩如实写计划 JWT 和 CORS。CI 和 schema 脚本降低环境和索引不一致风险。

---

## Optional: link to evidence on other slides

| Risk | Slide / artifact |
|------|------------------|
| T2, ADR-4 | `08-sequence-import.puml` |
| T1, ADR-5 | Import demo + Sprint 2 hours chart |
| R3, QA | `09-cicd-pipeline.puml` + coverage artifact |
| S1–S3 | ADR slide + deployment diagram |
