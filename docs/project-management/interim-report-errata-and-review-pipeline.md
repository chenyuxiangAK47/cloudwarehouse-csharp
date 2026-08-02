# CloudWarehouse 中期报告 — 事实勘误 + 二审流程（DeepSeek 查 → 千问改）

> **报告正文位置：** `docs/project-management/interim-report-writing-guide.md`（第 1–714 行为英文报告正文；勿与「写作指南」旧版混淆）  
> **本文件用途：** ① 已核实的事实勘误与替换文案；② 给 DeepSeek 的全文审查 Prompt；③ 给千问的修稿 Prompt。  
> **核实日期：** 以仓库当前 `dotnet test` 结果为准（37 个测试：25 单元 + 12 集成）。

---

## 一、推荐工作流

```
步骤 1  你 → DeepSeek：粘贴「三、DeepSeek 全文审查 Prompt」+ 报告全文（或附件）
步骤 2  DeepSeek → 输出《审查报告》（结构化，见 Prompt 要求）
步骤 3  你 → 千问：粘贴「四、千问修稿 Prompt」+ DeepSeek 审查报告 + 本文件「二、勘误清单」
步骤 4  千问 → 输出修订后的 Section 段落或全文 diff
步骤 5  你 → 合并进 Word/PPT，并插入截图与 PlantUML 导出图
```

---

## 二、事实勘误清单（已对照代码库核实）

### 2.1 全局查找替换（优先级 P0 — 必须改）

| 序号 | 原文（错误或不严谨） | 改为（建议英文） | 依据 |
|------|----------------------|------------------|------|
| E1 | `over forty` / `over forty automated tests` / `over forty passing tests` / `totaling over forty tests` | **`37 automated tests`**（或 *a suite of 37 automated tests, comprising 25 unit and 12 integration tests*） | `dotnet test`：CloudWarehouse.Tests=25，IntegrationTests=12 |
| E2 | `Test suite exceeds 40` / `exceeds 40 unit and integration tests` | **`37 unit and integration tests (25 + 12)`** | 同上；M4 完成标准需一致 |
| E3 | `Figure 6-1 (see below)` | **`Section 8 (Database ERD); Figure 8-1 (ERD diagram, see Appendix A)`** — 且须插入图或写 *Figure 8-1 to be inserted* | 正文无 Figure 6-1 |
| E4 | `registration of key services such as Dapper's SqlConnection` | **`registration of application services (PriceRuleImportService, PriceRuleCalculateService)`**；连接在控制器/服务内 **`new SqlConnection(_conn)`**，非 DI 注册 | `Program.cs` 仅 `AddScoped` 两个 Service |
| E5 | `integration tests are configured to use an in-memory or test-specific data source` | 见 **§2.3 Section 10 替换段落** | 无 InMemory DB；`Testing` 环境仅改 URL |
| E6 | `SQLite in-memory` / `run in-memory`（描述 CI 集成测试） | **删除或改为：** *Integration tests use WebApplicationFactory with environment "Testing"; they still invoke the same SQL Server connection string when a database is available. CI runs unit tests and API tests that do not require a live database for every scenario.* | 见 `CloudWarehouseWebApplicationFactory.cs` |
| E7 | `risk management strategies were formalized in a dedicated slide document` | **`documented in Section 17 (Risk Register) and docs/project-management/risk-management-slide.md`** | 无独立 slide 文件名为 "dedicated slide document" |
| E8 | `docs/architecture/decisions/`（Appendix E） | **`Sections 12–13 of this report; source diagrams in docs/diagrams/01a-*.puml and 01b-*.puml`** | 该目录不存在 |
| E9 | `demo video recorded`（若 M5 表写 Done） | 与 sprint plan 一致：**`In progress`**（US-4.6 / M5 演示视频待录） | `4-week-sprint-plan.md` US-4.6 pending |
| E10 | 尾注孤立数字 `1`（无参考文献列表） | 改为 **`(see Section X)`** / **`(Appendix B)`** 或删除 | 格式问题 |

### 2.2 图表引用统一规则（P0）

报告中引用了但未嵌入的图，**二选一**：

**方案 A（推荐终稿）：** 从 `docs/diagrams/*.puml` 导出 PNG 插入，并统一编号：

| 原引用 | 建议编号 | 源文件 |
|--------|----------|--------|
| Figure 6-1 | **删除**，改用 Figure 8-1 | `07-erd.puml` |
| Figure 14-1 | Figure 14-1 | `08-sequence-import.puml` |
| Figure 14-2, 14-3 | 截图 | 导入预览 / 入库成功 UI |
| Figure 17-1 | Figure 17-1 | `12-risk-management.puml` |
| Figure 17-2, 17-3 | 截图 | .txt 拒绝、导入失败回滚 |
| Figure 18-1, 18-2, 18-3 | 截图 | dotnet test 绿勾、Test Explorer、Coverage |

**方案 B（中期草稿）：** 在 Section 2 末或封面加一句：

> *Figures and screenshots referenced in this interim draft are provided in Appendix A–C of the submission package; embedded placement is deferred to the final report.*

然后把表中所有 `Figure X-X` 改为 `*(Appendix A, Figure X-X — placeholder)*`。

### 2.3 Section 10 替换段落（复制进报告替换错误句）

**替换 10.2 表中 CI Runner 备注行（原 in-memory 那句）：**

> Executes the CI pipeline defined in `.github/workflows/ci.yml` (checkout, restore, `dotnet test` with Coverlet, ReportGenerator, artifact upload). The runner does **not** host SQL Server. **Unit tests** run fully in CI. **Integration tests** use `WebApplicationFactory` with environment `Testing` (see `CloudWarehouseWebApplicationFactory.cs`); they call the real HTTP pipeline and, where applicable, the configured `DefaultConnection`—the MVP does **not** use an in-memory database or SQLite substitute. CI evidence is green builds plus uploaded `coverage-report` artifacts.

**替换 10.5 或 15.6 中「Database Availability」对比表 — CI 列：**

| Aspect | Local | CI (GitHub Actions) |
|--------|-------|---------------------|
| Database | SQL Server on localhost:1433 for dev and manual integration testing | No SQL Server service on `ubuntu-latest` runner; pipeline still runs `dotnet test` on projects that predominantly use in-memory workbooks or API tests without mandatory DB |
| Integration tests | May hit SQL Server when connection string points to local instance | Same test assembly; DB-dependent paths depend on connection availability—document as MVP limitation |

**替换 3.5 Evidence 中 Program.cs 一句：**

> `Program.cs` registers `PriceRuleImportService` and `PriceRuleCalculateService` as scoped services, configures CORS, and serves static files from `wwwroot`. Database access uses explicit `SqlConnection` instances inside controllers and services with connection strings from configuration—not centralized `SqlConnection` DI registration.

### 2.4 可选增强（P1 — 非错误，但更准确）

| 位置 | 建议补充一句 |
|------|----------------|
| Section 2 / 4 | Phase 1 also supports **destination-matrix** Excel (default site **C001**), **auto-create** sites/destinations on price import, **Site/Destination/Customer** Excel import |
| Section 14 | Matrix format: no site column → default site code **C001** |
| Section 19 | US-3.1 描述可从 "validate site exists" 改为 **auto-provision master data on import**（与当前代码一致） |

### 2.5 已核实「正确」、DeepSeek 勿再误报

| 陈述 | 状态 |
|------|------|
| ASP.NET Core 9 + .NET 9 | ✅ |
| Dapper、ClosedXML、xUnit、GitHub Actions | ✅ |
| Sprint 2 +39%（44→61） | ✅ |
| 团队总工时 198 est / 211 actual | ✅ |
| 三种 Excel 格式 | ✅ |
| `fix-price-rules-index.sql`、多行 PriceRules | ✅ |
| `docs/diagrams/06-use-case-diagram.puml` 存在 | ✅（需导出图嵌入） |
| Appendix G 指向 `log` | ✅ 仓库根目录有 `log` 文件 |
| Phase 2 Strategy Pattern = Planned | ✅ |

### 2.6 测试数量证据（给报告 / 截图用）

在 Section 18 写入：

> As of the interim submission, the solution executes **37** automated tests: **25** in `CloudWarehouse.Tests` and **12** in `CloudWarehouse.IntegrationTests`, all passing via `dotnet test CloudWarehouse.sln -c Release`.

验证命令：

```powershell
cd D:\tools\cloudwarehouse-csharp
dotnet test -c Release --verbosity minimal
```

---

## 三、DeepSeek 全文审查 Prompt（复制整段）

```markdown
# Role
You are a strict academic reviewer for the CloudWarehouse NUS internship **interim report**.
Perform a **full-document audit** of Sections 2–20. Do NOT rewrite the report; produce a structured defect list for another AI (Qwen) to apply fixes.

# Mandatory references (read first)
1. Factual baseline & known corrections: `docs/project-management/interim-report-errata-and-review-pipeline.md` Section 2
2. Supervisor requirements: repository root file `log`
3. Writing spec (section headings): original guide structure Sections 2–20
4. Verify claims against repo paths listed in Section 2.5 and Section 5 of the errata file

# Document under review
The report text is in: `docs/project-management/interim-report-writing-guide.md` (lines 1–714, English body).
If the user pastes the report below, use the paste; otherwise assume the file content.

---REPORT START---
[PASTE FULL REPORT HERE IF NOT USING FILE]
---REPORT END---

# Audit tasks (complete ALL)

## A. Factual accuracy vs codebase
Check every quantified claim: test count, ports (5001, 1433), tech versions, file paths, ADR statements, CI behaviour, in-memory DB claims, DI registration in Program.cs, M4 "40 tests", demo video status.
Flag each as: CONFIRMED / WRONG (with correction) / UNVERIFIED (need screenshot).

## B. Figure & evidence
List every "Figure X-X", "screenshot", "accompanying", "see below", "Appendix".
Mark: EMBEDDED / REFERENCED BUT MISSING / WRONG NUMBER.
Minimum evidence supervisor expects: import UI, CI green, dotnet test output, coverage artifact.

## C. Section-by-section (2–20)
For each section: PASS / PASS WITH REVISIONS / FAIL + bullet gaps (max 5 per section).

## D. Supervisor feedback (`log`)
Table: Feedback item | Addressed in section | Adequate? | Missing evidence?

## E. Internal consistency
- Test count same everywhere?
- M4 criteria vs Section 18?
- M5 video recorded vs In progress?
- Sprint hours 198/211 vs Section 19 story sums?
- Duplicate Section 20 conclusion?

## F. Copy-paste fix list for Qwen (CRITICAL OUTPUT)
Produce a numbered list **F1, F2, …** each with:
- `Location`: Section + quote first 10 words
- `Problem`: one line
- `Fix`: exact replacement English text OR instruction
- `Priority`: P0 / P1 / P2

Merge all items from errata file Section 2.1 if still present in report (do not duplicate—reference E#).

## G. Final verdict
One of: **READY AFTER P0 FIXES** / **NEEDS MAJOR REVISION** / **FAIL**
Count: P0 issues __, P1 __, P2 __

# Output format (strict markdown)
Use headings: ## A. Factual … ## B. Figures … ## C. Section … ## D. Supervisor … ## E. Consistency … ## F. Fix List … ## G. Verdict

# Rules
- Be stricter than a friendly peer review.
- "over forty tests" when actual is 37 → P0 WRONG.
- "in-memory" integration tests without SQLite/InMemory in repo → P0 WRONG.
- SqlConnection registered in DI → P0 WRONG if Program.cs only has two AddScoped services.
- Do not mark PASS on evidence sections if zero screenshots embedded.
```

---

## 四、千问修稿 Prompt（在 DeepSeek 输出后使用）

```markdown
# Role
You are an academic report editor for the CloudWarehouse interim report (English).

# Inputs (all mandatory)
1. **Original report** — full text from `docs/project-management/interim-report-writing-guide.md` (Sections 2–20)
2. **DeepSeek audit report** — paste below (especially section ## F. Fix List)
3. **Verified errata** — `docs/project-management/interim-report-errata-and-review-pipeline.md` Section 2 (E1–E10 and Section 10 replacement paragraphs)

---DEEPSEEK AUDIT START---
[PASTE DEEPSEEK FULL OUTPUT HERE]
---DEEPSEEK AUDIT END---

# Task
Apply **all P0 fixes** from DeepSeek F-list and errata E1–E10. Apply **P1** where clearly stated. Do not change meaning of correct technical content.

# Editing rules
1. Output **complete revised sections** only for sections that changed (not the whole 714 lines unless many sections change).
2. For each change, prefix a line: `<!-- FIX Fxx or Ex: reason -->` before the revised paragraph (便于用户核对).
3. Test count: use **37** (25 unit + 12 integration) everywhere.
4. Remove or fix all broken Figure 6-1 references; use Section 8 / Figure 8-1 / Appendix A convention from errata §2.2.
5. Replace Section 10 / 15 CI database paragraphs with errata §2.3 text.
6. Fix Program.cs / DI description per errata E4.
7. Fix Appendix E path per errata E8.
8. Fix risk "dedicated slide document" per errata E7.
9. Align M4, M5, US-4.5/4.6 with sprint plan (M5 In progress if video not done).
10. Remove stray footnote-only `.1` or replace with real cross-references.
11. Delete duplicate concluding "20. Any Other Business" block at end if it repeats 20.1–20.8 (merge into one conclusion).
12. Keep academic tone; do not shorten ADR/DDD sections unless fixing errors.

# Output format
## Summary of changes (table)
| ID | Section | Change type |
|----|---------|-------------|

## Revised text
### Section X
[full revised section text]

(repeat for each modified section)

## Remaining manual tasks for human
- [ ] Insert screenshot: ...
- [ ] Export PlantUML: ...
```

---

## 五、给 DeepSeek 的「必查清单」（可附在 Prompt 末尾）

复制下面列表，确保 DeepSeek 逐项打勾：

```
[ ] 测试数量：全文搜索 forty/40 tests → 应为 37
[ ] Program.cs：是否误写 SqlConnection DI
[ ] Section 10/15/16：in-memory / SQLite / CI 无数据库 描述是否准确
[ ] Figure 6-1 是否存在
[ ] Figure 14/17/18 是否嵌入或 placeholder 说明
[ ] Appendix E 路径是否存在
[ ] M4 完成标准与测试数
[ ] M5 / demo video 状态
[ ] Section 3.4 dedicated slide → Section 17
[ ] Section 7 个人工时表是否存在
[ ] Section 20 Strategy + 微服务是否标 Planned
[ ] 文末重复 Section 20
[ ] 导师 log 五条意见是否都有对应章节
```

---

## 六、文件拆分建议（交稿前）

| 文件 | 建议 |
|------|------|
| `interim-report-writing-guide.md` | 拆出：① `interim-report-draft.md`（仅正文）② 恢复 guide 为中文写作指南 |
| 截图 | 放 `docs/report-evidence/` |
| PlantUML PNG | 放 `docs/report-evidence/diagrams/` |

---

*维护：修完一轮后更新 §2.1 测试数（若新增测试）并重新跑 `dotnet test`。*
