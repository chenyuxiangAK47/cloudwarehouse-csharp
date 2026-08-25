# 第四章 项目路线图与迭代执行

第三章用 MoSCoW 明确了已交付用例与优先级。本章说明这些能力如何在 **Solo** 开发条件下，按 **一周一个 Sprint** 落地，并以 **个人 Planned vs Actual（小时）** 证明执行过程可审计。工时数据来自项目跟踪表 `docs/project-management/sprint-hours-chart-data.csv` 及 Phase 2 工作记录；不采用任何“多人团队产能”假设。

---

## 4.1 方法与节奏

本项目采用短迭代敏捷节奏：**每个 Sprint = 1 个自然周**。

| 阶段 | Sprint | 重心 |
|------|--------|------|
| Phase 1 | Sprint 1–4 | CloudWarehouse MVP（主数据、Excel 导入、试算、CI） |
| Phase 2 | Sprint 5 起 | 计费 Strategy、运单双轨与历史价、规则检索；**并列**推进 PDA 无订单报工 |

Solo 场景下的管理方式：

- **计划**：每周按可投入小时选择 Must 级任务，拆成可验证小项（可演示 / 可测）。
- **执行**：以仓库任务清单与 Git 提交为进度证据（非虚构的多人看板故事）。
- **复盘**：对比 Planned/Actual，调整下一周对“外部 Excel / 硬件联调”类任务的缓冲。
- **治理**：满足导师要求——报告中单独呈现 **个人** 预估工时与实际工时。

---

## 4.2 里程碑总览

| ID | 名称 | Sprint | 状态 | 主要证据 |
|----|------|--------|------|----------|
| M1 | Foundation | S1 | Done | `database/schema.sql`、Sites/Destinations CRUD |
| M2 | Import Preview | S2 | Done | 标准表头 + legacy 三级表头解析、预览流程 |
| M3 | Rules & Pricing | S3 | Done | PriceRules 事务入库、试算 API/UI |
| M4 | QA & CI | S4 | Done | 单元/集成/轻量压测、GitHub Actions、覆盖率 Artifact |
| M5 | Documentation & Videos | S4–终期 | In progress | 本报告、7 段评估视频 |
| M6 | Billing Strategy + Dual-track | S5 | **Done** | Strategy 类图/时序、运单双轨、历史价 |
| M6b | Rule knowledge lookup | S5 | **Done** | `/api/Assistant/ask`、检索 UI |
| M6c | PDA No-order reporting MVP | Phase 2 并列 | **Done** | 霍尼韦尔 PDA 开工/报工演示 |
| M7 | Authentication (JWT/RBAC) | 规划 | Planned | ADR 延期认证 |
| M8 | Microservice extraction（按触发条件） | 规划 | Planned | 见架构章触发条件 |

图示建议：`docs/diagrams/10-roadmap-milestones.puml`（若图中 M6 仍标 Planned，以本章文字为准并更新图）。

---

## 4.3 Sprint 1 — Foundation（计划 48h / 实际 52h，+8%）

**目标：** 建立可运行底座——数据库、主数据读写、静态管理端壳子、Dapper 访问路径。

**完成：**

- SQL Server 库表设计与脚本（站点、目的地等主数据及价格规则基础结构）；
- Sites / Destinations 等 CRUD API 与前端 Tab；
- Dapper + 连接字符串配置打通。

**偏差说明：** 实际 52h，相对计划 +4h（约 +8%），主要来自本机 SQL Server / .NET 9 环境与连接调通，属于基础建设性超支，幅度可控。

---

## 4.4 Sprint 2 — Excel Import（计划 44h / 实际 61h，+39%）

**目标：** 支撑供应商/师傅侧价表进入系统——模板、解析、预览，避免不经验证直接写库。

**完成：**

- 标准单行表头模板下载；
- **Legacy 三级表头**等复杂格式自动探测与解析（ClosedXML）；
- 预览 → 确认 的导入工作流；预览中可挂钩试算校验。

**超支原因（主因）：** 真实供应商表头层级、列对齐与双格式兼容复杂度显著高于初估，导致解析与回归测试工作量上升。实际 61h，超支约 **+39%**，是 Phase 1 最大偏差源。

**管理纠正（用于 Sprint 3–4）：**

1. 涉及外部文件的任务拆得更细，并单独估“坏数据样本”回归；
2. 对此类任务预留缓冲（约 15% 量级）；
3. 坚持预览后提交，降低错误入库返工。

---

## 4.5 Sprint 3 — Rules & Pricing（计划 56h / 实际 51h，−9%）

**目标：** 将预览通过的价表 **事务性写入** PriceRules，并提供稳定运费试算。

**完成：**

- 导入提交：校验失败整批回滚；按 lane + 生效日版本更新规则；
- **一行 Excel → 多条 PriceRules**（多档区间 + 续重）映射落地；
- `/api/PriceRule/calculate` 与 UI 试算。

**说明：** 本 Sprint **不是** JSON 动态折扣规则引擎，而是基于数据库规则行的计费数据模型 + 计算服务。实际 51h，低于计划，反映 Sprint 2 之后估算收敛、以及导入管道复用带来的效率。

---

## 4.6 Sprint 4 — QA & CI（计划 50h / 实际 47h，−6%）

**目标：** 把质量门禁工程化，形成可验证证据链。

**完成：**

- xUnit 单元测试 + `WebApplicationFactory` 集成测试 + 轻量并发/压测用例；
- GitHub Actions：`restore` → `dotnet test`（含覆盖率）→ ReportGenerator → Artifact；
- CI 在无 SQL Server 的云端 Runner 上对依赖库的用例采取可解释的跳过策略，避免环境假红。

**说明：** 文档终稿与 7 段视频归入 M5，滚动到终期完成，避免 Sprint 4 范围膨胀。实际 47h，略低于计划。

---

## 4.7 Phase 2（Sprint 5 起）— 设计加深与并列 PDA

中期反馈指出系统“偏简单”、计费变体需要设计模式与详细设计、架构叙述与证据不足。Phase 2 的回应 **不是** 立刻拆微服务，而是：

### 4.7.1 CloudWarehouse 加深（已完成）

- **Strategy Pattern**：`TierBillingStrategy` / `OverweightBillingStrategy` / `VolumetricBillingStrategy`，由 `FeeCalculationEngine` 编排；
- **运单双轨**：应收（客户报价）vs 应付（成本），按 **发货日** 取历史有效规则，预览与表内中转费对比；
- **计价规则检索**：辅助查阅知识库，**不替代**结算引擎；
- 配套类图、双轨时序图与测试（见软件设计 / QA 章）。

### 4.7.2 PDA 无订单报工（并列已完成）

为契合产线夜班/无正式工单场景，并列交付霍尼韦尔 PDA 应用：登录 → 选产线/机群/机床 → 开工/报工/查询；数据经 Spring Boot API 落库。与 CloudWarehouse **未做生产级 API 打通**，属同一工厂目标下的分上下文交付。

### 4.7.3 Phase 2 工时（个人，待作者填实数）

| 工作包 | Planned (h) | Actual (h) | 备注 |
|--------|-------------|------------|------|
| CW：Strategy + 双轨/历史价 + 检索 + 图/报告同步 | （填） | （填） | 建议与提交日志对照 |
| PDA：API + Android + 联调/手册 | （填） | （填） | 与 CW 分栏，勿混成一笔糊涂账 |
| **Phase 2 小计** | （填） | （填） | |

（定稿前必须替换为真实数字；不可留空提交。）

---

## 4.8 个人工时 Planned vs Actual（Phase 1）

| Sprint | 目标摘要 | Planned (h) | Actual (h) | Variance |
|--------|----------|-------------|------------|----------|
| S1 | Foundation | 48 | 52 | +8% |
| S2 | Excel Import | 44 | 61 | **+39%** |
| S3 | Rules & Pricing | 56 | 51 | −9% |
| S4 | QA & CI | 50 | 47 | −6% |
| **Phase 1 合计** | | **198** | **211** | **+7%** |

图示建议：打开 `docs/project-management/sprint-hours-chart.html` 截屏插入本报告。

**分析要点：**

- Phase 1 总体 +7%，说明计划总体可控；
- **唯一大幅超支在 Sprint 2**，与外部 Excel 复杂度直接相关；Sprint 3–4 回到约 ±10% 以内，说明复盘纠正有效；
- 该表为 **Solo 个人工时**，满足中期“个人 Planned vs Actual”要求。

---

## 4.9 Evidence（本章）

| 证据 | 位置 |
|------|------|
| 工时原始数据 | `docs/project-management/sprint-hours-chart-data.csv` |
| 工时柱状图 | `sprint-hours-chart.html` 截图 |
| 路线图 | `docs/diagrams/10-roadmap-milestones.puml` |
| S2 导入能力 | 导入预览成功/失败截图；ExcelHelper 相关测试 |
| S4 CI | GitHub Actions 绿勾与 coverage Artifact |
| S5 Strategy/双轨 | 类图 13、时序 14、运单预览截图 |
| PDA | 开工/报工成功截图或短录屏 |

---

## 4.10 本章小结

Phase 1 按四周 Sprint 交付了 CloudWarehouse MVP，并在个人工时维度保持总体可控偏差。Phase 2 通过 Strategy 与双轨历史价回应“不够深”的反馈，同时并列交付 PDA 无订单报工以覆盖产线数据采集。后续章节将分别展开持久化模型（数据库）、多视角架构与计费详细设计，并继续使用可验证证据而非口号。
